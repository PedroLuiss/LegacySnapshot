using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using Microsoft.Extensions.Logging;

namespace LegacySnapshot.ViewModels;

public partial class QueryRunnerViewModel(
    IActiveConnectionService activeConnectionService,
    IQueryExecutor queryExecutor,
    IQueryHistoryRepository historyRepository,
    IExportStrategy exportStrategy,
    ILogger<QueryRunnerViewModel> logger) : ObservableObject
{
    private QueryResult? _lastResult;

    public ObservableCollection<QueryHistoryItem> History { get; } = [];
    public ObservableCollection<string> ResultColumns { get; } = [];
    public ObservableCollection<QueryResultRow> ResultRows { get; } = [];
    public ICommand CancelQueryCommand => new Command(() => ExecuteQueryCommand.Cancel());
    public double ResultsWidth => Math.Max(150, ResultColumns.Count * 150);

    [ObservableProperty]
    public partial string QueryText { get; set; } = "SELECT name FROM sys.tables ORDER BY name;";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool HasResults { get; set; }

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public string ActiveConnectionName => activeConnectionService.ActiveConnection?.Name ?? "Sin conexión activa";

    [RelayCommand]
    private async Task LoadHistoryAsync()
    {
        try
        {
            var history = await historyRepository.GetRecentAsync();
            History.Clear();
            foreach (var item in history)
            {
                History.Add(item);
            }

            OnPropertyChanged(nameof(ActiveConnectionName));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load query history.");
            StatusMessage = "No se pudo cargar el historial de consultas.";
        }
    }

    [RelayCommand]
    private async Task ExecuteQueryAsync(CancellationToken cancellationToken)
    {
        if (activeConnectionService.ActiveConnection is null)
        {
            StatusMessage = "Ve a Conexiones y selecciona Conectar en un servidor.";
            return;
        }

        if (string.IsNullOrWhiteSpace(QueryText))
        {
            StatusMessage = "Escribe una consulta antes de ejecutarla.";
            return;
        }

        var activeConnection = activeConnectionService.ActiveConnection;
        if (activeConnection.Environment == "Prod")
        {
            if (Shell.Current is not { } shell)
            {
                StatusMessage = "No se pudo mostrar la confirmación requerida para ejecutar en producción.";
                return;
            }

            var confirmed = await shell.DisplayAlertAsync(
                "Conexión de producción",
                "Vas a ejecutar SQL en producción. Revisa la consulta y confirma que es segura. ¿Continuar?",
                "Ejecutar",
                "Cancelar");
            if (!confirmed)
            {
                StatusMessage = "Ejecución cancelada.";
                return;
            }
        }

        if (QueryText.Contains("xp_cmdshell", StringComparison.OrdinalIgnoreCase))
        {
            if (Shell.Current is not { } shell)
            {
                StatusMessage = "No se pudo mostrar la advertencia para xp_cmdshell; la consulta no se ejecutó.";
                return;
            }

            var confirmed = await shell.DisplayAlertAsync(
                "Comando potencialmente peligroso",
                "La consulta contiene xp_cmdshell, que puede ejecutar comandos del sistema operativo con los permisos de SQL Server. Ejecuta solo si comprendes y autorizas sus efectos. ¿Continuar?",
                "Continuar",
                "Cancelar");
            if (!confirmed)
            {
                StatusMessage = "Ejecución cancelada.";
                return;
            }
        }

        IsBusy = true;
        StatusMessage = "Ejecutando consulta...";
        HasResults = false;
        ResultColumns.Clear();
        ResultRows.Clear();
        try
        {
            var connectionString = await activeConnectionService.GetConnectionStringAsync();
            var result = await queryExecutor.ExecuteAsync(connectionString, QueryText, cancellationToken);
            _lastResult = result;
            HasResults = result.IsResultSet;
            foreach (var column in result.Columns)
            {
                ResultColumns.Add(column);
            }

            foreach (var row in result.Rows)
            {
                ResultRows.Add(new QueryResultRow
                {
                    Values = row.Select(value => value ?? "NULL").ToList()
                });
            }

            OnPropertyChanged(nameof(ResultsWidth));
            var item = new QueryHistoryItem
            {
                Query = QueryText,
                ExecutedAt = DateTimeOffset.Now,
                DurationMs = result.DurationMs,
                RowsAffected = result.IsResultSet ? result.Rows.Count : result.RowsAffected
            };
            await historyRepository.AddAsync(item);
            History.Insert(0, item);
            while (History.Count > 20)
            {
                History.RemoveAt(History.Count - 1);
            }

            var rowSummary = result.IsResultSet
                ? $"{result.Rows.Count} filas"
                : $"{result.RowsAffected} filas afectadas";
            var truncated = result.IsTruncated ? " (se alcanzó el límite de 10.000 filas)" : string.Empty;
            StatusMessage = $"{rowSummary}{truncated} · {result.DurationMs} ms";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Consulta cancelada.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "SQL query execution failed.");
            StatusMessage = $"Error al ejecutar la consulta: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void UseHistoryItem(QueryHistoryItem item)
    {
        QueryText = item.Query;
        StatusMessage = "Consulta cargada desde el historial.";
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        if (_lastResult is not { IsResultSet: true })
        {
            StatusMessage = "Ejecuta primero una consulta que devuelva resultados.";
            return;
        }

        try
        {
            var content = exportStrategy.Export(_lastResult);
            var fileName = $"LegacySnapshot-{DateTime.Now:yyyyMMdd-HHmmss}{exportStrategy.FileExtension}";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            await Share.Default.RequestAsync(new ShareFileRequest(
                "Exportar resultados SQL",
                new ShareFile(filePath, exportStrategy.ContentType)));
            StatusMessage = "Archivo CSV preparado para compartir.";
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not export query results to CSV.");
            StatusMessage = $"No se pudieron exportar los resultados: {exception.Message}";
        }
    }
}
