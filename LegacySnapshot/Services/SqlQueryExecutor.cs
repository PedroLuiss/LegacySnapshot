using System.Data;
using System.Diagnostics;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using Microsoft.Data.SqlClient;

namespace LegacySnapshot.Services;

public sealed class SqlQueryExecutor : IQueryExecutor
{
    private const int MaxRows = 10_000;

    public async Task<QueryResult> ExecuteAsync(
        string connectionString,
        string query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("La consulta no puede estar vacía.", nameof(query));
        }

        var stopwatch = Stopwatch.StartNew();
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(query, connection)
        {
            CommandTimeout = 30
        };

        using var cancelRegistration = cancellationToken.Register(
            static state => ((SqlCommand)state!).Cancel(),
            command);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new QueryResult { IsResultSet = reader.FieldCount > 0 };
        if (result.IsResultSet)
        {
            for (var index = 0; index < reader.FieldCount; index++)
            {
                result.Columns.Add(reader.GetName(index));
            }

            while (result.Rows.Count < MaxRows && await reader.ReadAsync(cancellationToken))
            {
                var row = new List<string?>(reader.FieldCount);
                for (var column = 0; column < reader.FieldCount; column++)
                {
                    row.Add(reader.IsDBNull(column)
                        ? null
                        : Convert.ToString(reader.GetValue(column), System.Globalization.CultureInfo.InvariantCulture));
                }

                result.Rows.Add(row);
            }

            if (result.Rows.Count == MaxRows && await reader.ReadAsync(cancellationToken))
            {
                result.IsTruncated = true;
            }
        }
        else
        {
            result.RowsAffected = reader.RecordsAffected;
            while (await reader.NextResultAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                }
            }

            await reader.CloseAsync();
            result.RowsAffected = reader.RecordsAffected;
        }

        stopwatch.Stop();
        result.DurationMs = stopwatch.ElapsedMilliseconds;
        return result;
    }
}
