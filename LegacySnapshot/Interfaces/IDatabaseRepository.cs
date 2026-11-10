using LegacySnapshot.Models;

namespace LegacySnapshot.Interfaces
{
    public interface IDatabaseRepository
    {
        // Método para probar la conexión
        Task<bool> TestConnectionAsync(string connectionString);

        // Método para obtener las tablas y sus tamaños
        Task<List<TableInfo>> GetTablesAsync(string connectionString);

        Task<TableDetails> GetTableDetailsAsync(
            string connectionString,
            string schemaName,
            string tableName,
            CancellationToken cancellationToken = default);

        Task<List<ProcedureStats>> GetProceduresAsync(
            string connectionString,
            CancellationToken cancellationToken = default);

        Task<string?> GetProcedureDefinitionAsync(
            string connectionString,
            int objectId,
            CancellationToken cancellationToken = default);
    }
}