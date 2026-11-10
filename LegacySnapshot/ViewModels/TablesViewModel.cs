using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using Microsoft.Extensions.Logging;

namespace LegacySnapshot.ViewModels;

public partial class TablesViewModel(
    IActiveConnectionService activeConnectionService,
    IDatabaseRepository databaseRepository,
    ILogger<TablesViewModel> logger) : ObservableObject
{
    private readonly List<TableInfo> _allTables = [];

    public ObservableCollection<TableInfo> Tables { get; } = [];
    public ObservableCollection<ColumnInfo> Columns { get; } = [];
    public ObservableCollection<IndexInfo> Indexes { get; } = [];
    public IReadOnlyList<string> SortOptions { get; } = ["Tamaño", "Nombre", "Filas"];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    [NotifyPropertyChangedFor(nameof(EmptyMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedSort { get; set; } = "Tamaño";

    [ObservableProperty]
    public partial TableDetails? SelectedDetails { get; set; }

    [ObservableProperty]
    public partial bool IsDetailsVisible { get; set; }

    public string EmptyMessage => !string.IsNullOrWhiteSpace(StatusMessage)
        ? StatusMessage
        : _allTables.Count == 0
            ? "No se encontraron tablas visibles en esta base de datos."
            : "No hay tablas que coincidan con el filtro.";

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public string ActiveConnectionName => activeConnectionService.ActiveConnection?.Name ?? "Sin conexión activa";

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedSortChanged(string value) => ApplyFilter();

    [RelayCommand]
    private async Task LoadTablesAsync()
    {
        if (activeConnectionService.ActiveConnection is null)
        {
            StatusMessage = "Ve a Conexiones y selecciona Conectar en un servidor.";
            OnPropertyChanged(nameof(ActiveConnectionName));
            return;
        }

        IsBusy = true;
        StatusMessage = string.Empty;
        try
        {
            var connectionString = await activeConnectionService.GetConnectionStringAsync();
            var tables = await databaseRepository.GetTablesAsync(connectionString);
            _allTables.Clear();
            _allTables.AddRange(tables);
            OnPropertyChanged(nameof(ActiveConnectionName));
            ApplyFilter();
            StatusMessage = $"Se encontraron {tables.Count:N0} tablas.";
            OnPropertyChanged(nameof(EmptyMessage));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load tables for the active database.");
            _allTables.Clear();
            Tables.Clear();
            StatusMessage = $"No se pudieron cargar las tablas: {exception.Message}";
            OnPropertyChanged(nameof(EmptyMessage));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowDetailsAsync(TableInfo table)
    {
        IsBusy = true;
        try
        {
            var connectionString = await activeConnectionService.GetConnectionStringAsync();
            SelectedDetails = await databaseRepository.GetTableDetailsAsync(
                connectionString,
                table.SchemaName,
                table.TableName);

            Columns.Clear();
            Indexes.Clear();
            foreach (var column in SelectedDetails.Columns)
            {
                Columns.Add(column);
            }

            foreach (var index in SelectedDetails.Indexes)
            {
                Indexes.Add(index);
            }

            IsDetailsVisible = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load details for table {Schema}.{Table}.", table.SchemaName, table.TableName);
            StatusMessage = $"No se pudo cargar el detalle de la tabla: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void HideDetails()
    {
        IsDetailsVisible = false;
        SelectedDetails = null;
        Columns.Clear();
        Indexes.Clear();
    }

    private void ApplyFilter()
    {
        IEnumerable<TableInfo> filtered = _allTables;
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(table =>
                table.TableName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)
                || table.SchemaName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedSort switch
        {
            "Nombre" => filtered.OrderBy(table => table.SchemaName).ThenBy(table => table.TableName),
            "Filas" => filtered.OrderByDescending(table => table.RowCount),
            _ => filtered.OrderByDescending(table => table.TotalSizeMB)
        };

        Tables.Clear();
        foreach (var table in filtered)
        {
            Tables.Add(table);
        }

        OnPropertyChanged(nameof(EmptyMessage));
    }
}
