using System.Data;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using Microsoft.Data.SqlClient;

namespace LegacySnapshot.Services;

public sealed class SqlServerDatabaseRepository : IDatabaseRepository
{
    private const string TablesQuery = """
        WITH PartitionRows AS
        (
            SELECT
                object_id,
                SUM(CASE WHEN index_id IN (0, 1) THEN rows ELSE 0 END) AS RowTotal
            FROM sys.partitions
            GROUP BY object_id
        ),
        TablePages AS
        (
            SELECT
                p.object_id,
                SUM(au.total_pages) AS TotalPages
            FROM sys.partitions AS p
            LEFT JOIN sys.allocation_units AS au
                ON (au.type IN (1, 3) AND au.container_id = p.hobt_id)
                OR (au.type = 2 AND au.container_id = p.partition_id)
            GROUP BY p.object_id
        )
        SELECT
            s.name AS SchemaName,
            t.name AS TableName,
            COALESCE(pr.RowTotal, 0) AS TotalRows,
            COALESCE(tp.TotalPages, 0) * 8.0 / 1024 AS TotalSizeMB
        FROM sys.tables AS t
        INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
        LEFT JOIN PartitionRows AS pr ON pr.object_id = t.object_id
        LEFT JOIN TablePages AS tp ON tp.object_id = t.object_id
        ORDER BY TotalSizeMB DESC;
        """;

    public async Task<bool> TestConnectionAsync(string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = new SqlCommand("SELECT @@VERSION;", connection);
        await command.ExecuteScalarAsync();
        return true;
    }

    public async Task<List<TableInfo>> GetTablesAsync(string connectionString)
    {
        var tables = new List<TableInfo>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(TablesQuery, connection);
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess);

        while (await reader.ReadAsync())
        {
            tables.Add(new TableInfo
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1),
                RowCount = reader.GetInt64(2),
                TotalSizeMB = Convert.ToDouble(reader.GetValue(3))
            });
        }

        return tables;
    }

    public async Task<TableDetails> GetTableDetailsAsync(
        string connectionString,
        string schemaName,
        string tableName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        const string tableQuery = """
            WITH PartitionRows AS
            (
                SELECT
                    object_id,
                    SUM(CASE WHEN index_id IN (0, 1) THEN rows ELSE 0 END) AS RowTotal
                FROM sys.partitions
                GROUP BY object_id
            ),
            TablePages AS
            (
                SELECT
                    p.object_id,
                    SUM(au.total_pages) AS TotalPages,
                    SUM(CASE WHEN p.index_id IN (0, 1) THEN au.total_pages ELSE 0 END) AS DataPages,
                    SUM(CASE WHEN p.index_id > 1 THEN au.total_pages ELSE 0 END) AS IndexPages
                FROM sys.partitions AS p
                LEFT JOIN sys.allocation_units AS au
                    ON (au.type IN (1, 3) AND au.container_id = p.hobt_id)
                    OR (au.type = 2 AND au.container_id = p.partition_id)
                GROUP BY p.object_id
            )
            SELECT
                s.name,
                t.name,
                COALESCE(pr.RowTotal, 0),
                COALESCE(tp.TotalPages, 0) * 8.0 / 1024,
                COALESCE(tp.DataPages, 0) * 8.0 / 1024,
                COALESCE(tp.IndexPages, 0) * 8.0 / 1024
            FROM sys.tables AS t
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            LEFT JOIN PartitionRows AS pr ON pr.object_id = t.object_id
            LEFT JOIN TablePages AS tp ON tp.object_id = t.object_id
            WHERE s.name = @schemaName AND t.name = @tableName
            """;

        await using var tableCommand = new SqlCommand(tableQuery, connection);
        tableCommand.Parameters.Add("@schemaName", SqlDbType.NVarChar, 128).Value = schemaName;
        tableCommand.Parameters.Add("@tableName", SqlDbType.NVarChar, 128).Value = tableName;
        await using var tableReader = await tableCommand.ExecuteReaderAsync(cancellationToken);
        if (!await tableReader.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException($"No se encontró la tabla [{schemaName}].[{tableName}].");
        }

        var details = new TableDetails
        {
            SchemaName = tableReader.GetString(0),
            TableName = tableReader.GetString(1),
            RowCount = tableReader.GetInt64(2),
            TotalSizeMB = Convert.ToDouble(tableReader.GetValue(3)),
            DataSizeMB = Convert.ToDouble(tableReader.GetValue(4)),
            IndexSizeMB = Convert.ToDouble(tableReader.GetValue(5))
        };
        await tableReader.CloseAsync();

        const string columnsQuery = """
            SELECT
                c.name,
                ty.name,
                c.is_nullable,
                CONVERT(bit, CASE WHEN pk.column_id IS NULL THEN 0 ELSE 1 END)
            FROM sys.columns AS c
            INNER JOIN sys.tables AS t ON t.object_id = c.object_id
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            INNER JOIN sys.types AS ty ON ty.user_type_id = c.user_type_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.indexes AS i
                INNER JOIN sys.index_columns AS ic
                    ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1
            ) AS pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
            WHERE s.name = @schemaName AND t.name = @tableName
            ORDER BY c.column_id;
            """;
        await using (var columnsCommand = new SqlCommand(columnsQuery, connection))
        {
            columnsCommand.Parameters.Add("@schemaName", SqlDbType.NVarChar, 128).Value = schemaName;
            columnsCommand.Parameters.Add("@tableName", SqlDbType.NVarChar, 128).Value = tableName;
            await using var reader = await columnsCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                details.Columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    DataType = reader.GetString(1),
                    IsNullable = reader.GetBoolean(2),
                    IsPrimaryKey = reader.GetBoolean(3)
                });
            }
        }

        const string indexesQuery = """
            SELECT
                i.index_id,
                i.name,
                i.type_desc,
                c.name,
                i.is_unique
            FROM sys.indexes AS i
            INNER JOIN sys.index_columns AS ic
                ON ic.object_id = i.object_id AND ic.index_id = i.index_id
            INNER JOIN sys.columns AS c
                ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            INNER JOIN sys.tables AS t ON t.object_id = i.object_id
            INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
            WHERE s.name = @schemaName AND t.name = @tableName
                AND i.index_id > 0 AND i.is_hypothetical = 0
            ORDER BY i.index_id,
                CASE WHEN ic.key_ordinal = 0 THEN 1 ELSE 0 END,
                ic.key_ordinal,
                ic.index_column_id;
            """;
        await using (var indexesCommand = new SqlCommand(indexesQuery, connection))
        {
            indexesCommand.Parameters.Add("@schemaName", SqlDbType.NVarChar, 128).Value = schemaName;
            indexesCommand.Parameters.Add("@tableName", SqlDbType.NVarChar, 128).Value = tableName;
            await using var reader = await indexesCommand.ExecuteReaderAsync(cancellationToken);
            var indexes = new Dictionary<int, IndexInfo>();
            while (await reader.ReadAsync(cancellationToken))
            {
                var indexId = reader.GetInt32(0);
                if (!indexes.TryGetValue(indexId, out var index))
                {
                    index = new IndexInfo
                    {
                        Name = reader.GetString(1),
                        Type = reader.GetString(2),
                        IsUnique = reader.GetBoolean(4)
                    };
                    indexes.Add(indexId, index);
                    details.Indexes.Add(index);
                }

                if (index.Columns.Length > 0)
                {
                    index.Columns += ", ";
                }

                index.Columns += $"[{reader.GetString(3)}]";
            }
        }

        return details;
    }

    public async Task<List<ProcedureStats>> GetProceduresAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string query = """
            WITH ProcedureStats AS
            (
                SELECT
                    object_id,
                    SUM(execution_count) AS ExecutionCount,
                    SUM(total_elapsed_time) AS TotalElapsedTime,
                    MIN(min_elapsed_time) AS MinElapsedTime,
                    MAX(max_elapsed_time) AS MaxElapsedTime,
                    SUM(total_worker_time) AS TotalWorkerTime,
                    SUM(total_logical_reads) AS TotalLogicalReads
                FROM sys.dm_exec_procedure_stats
                WHERE database_id = DB_ID()
                GROUP BY object_id
            )
            SELECT TOP (20)
                p.object_id,
                s.name,
                p.name,
                COALESCE(ps.ExecutionCount, 0),
                COALESCE(ps.TotalElapsedTime, 0) / 1000.0,
                CASE
                    WHEN COALESCE(ps.ExecutionCount, 0) = 0 THEN 0
                    ELSE ps.TotalElapsedTime / ps.ExecutionCount / 1000.0
                END,
                COALESCE(ps.MinElapsedTime, 0) / 1000.0,
                COALESCE(ps.MaxElapsedTime, 0) / 1000.0,
                COALESCE(ps.TotalWorkerTime, 0) / 1000,
                COALESCE(ps.TotalLogicalReads, 0),
                CONVERT(bit, CASE WHEN ps.object_id IS NULL THEN 0 ELSE 1 END)
            FROM sys.procedures AS p
            INNER JOIN sys.schemas AS s ON s.schema_id = p.schema_id
            LEFT JOIN ProcedureStats AS ps ON ps.object_id = p.object_id
            WHERE p.is_ms_shipped = 0
            ORDER BY COALESCE(ps.TotalElapsedTime, 0) DESC, s.name, p.name;
            """;

        var procedures = new List<ProcedureStats>();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            procedures.Add(new ProcedureStats
            {
                ObjectId = reader.GetInt32(0),
                SchemaName = reader.GetString(1),
                ProcedureName = reader.GetString(2),
                ExecutionCount = reader.GetInt64(3),
                TotalElapsedMs = Convert.ToDouble(reader.GetValue(4)),
                AverageElapsedMs = Convert.ToDouble(reader.GetValue(5)),
                MinElapsedMs = Convert.ToDouble(reader.GetValue(6)),
                MaxElapsedMs = Convert.ToDouble(reader.GetValue(7)),
                TotalCpuMs = reader.GetInt64(8),
                TotalLogicalReads = reader.GetInt64(9),
                HasExecutionStats = reader.GetBoolean(10)
            });
        }

        return procedures;
    }

    public async Task<string?> GetProcedureDefinitionAsync(
        string connectionString,
        int objectId,
        CancellationToken cancellationToken = default)
    {
        const string query = "SELECT OBJECT_DEFINITION(@objectId);";
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@objectId", SqlDbType.Int).Value = objectId;
        var definition = await command.ExecuteScalarAsync(cancellationToken);
        return definition is DBNull or null ? null : Convert.ToString(definition);
    }
}
