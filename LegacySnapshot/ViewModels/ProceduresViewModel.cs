using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Models;
using Microsoft.Extensions.Logging;

namespace LegacySnapshot.ViewModels;

public partial class ProceduresViewModel(
    IActiveConnectionService activeConnectionService,
    IDatabaseRepository databaseRepository,
    ILogger<ProceduresViewModel> logger) : ObservableObject
{
    public ObservableCollection<ProcedureStats> Procedures { get; } = [];
    public IReadOnlyList<string> SortOptions { get; } = ["Tiempo total", "Promedio", "CPU", "Ejecuciones"];

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    [NotifyPropertyChangedFor(nameof(EmptyMessage))]
    public partial string StatusMessage { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SelectedSort { get; set; } = "Tiempo total";

    [ObservableProperty]
    public partial ProcedureStats? SelectedProcedure { get; set; }

    [ObservableProperty]
    public partial string ProcedureDefinition { get; set; } = string.Empty;

    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);
    public bool HasDefinition => !string.IsNullOrWhiteSpace(ProcedureDefinition);
    public string ActiveConnectionName => activeConnectionService.ActiveConnection?.Name ?? "Sin conexión activa";
    public string EmptyMessage => !string.IsNullOrWhiteSpace(StatusMessage)
        ? StatusMessage
        : "No se encontraron procedimientos almacenados de usuario en esta base de datos.";

    partial void OnSelectedSortChanged(string value) => SortProcedures();

    [RelayCommand]
    private async Task LoadProceduresAsync()
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
            var result = await databaseRepository.GetProceduresAsync(connectionString);
            Procedures.Clear();
            foreach (var procedure in result)
            {
                Procedures.Add(procedure);
            }

            OnPropertyChanged(nameof(ActiveConnectionName));
            SortProcedures();
            if (Procedures.Count == 0)
            {
                StatusMessage = "Esta base de datos no contiene procedimientos almacenados de usuario.";
            }
            else
            {
                var withStats = Procedures.Count(item => item.HasExecutionStats);
                StatusMessage = withStats == 0
                    ? $"Se encontraron {Procedures.Count} procedimientos; ninguno tiene ejecuciones registradas desde el último reinicio de SQL Server."
                    : $"Se encontraron {Procedures.Count} procedimientos; {withStats} tienen estadísticas de ejecución en caché.";
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load procedure statistics for the active database.");
            StatusMessage = $"No se pudieron cargar las estadísticas. Verifica los permisos VIEW SERVER STATE o VIEW SERVER PERFORMANCE STATE. Detalle: {exception.Message}";
            Procedures.Clear();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ShowDefinitionAsync(ProcedureStats procedure)
    {
        IsBusy = true;
        try
        {
            var connectionString = await activeConnectionService.GetConnectionStringAsync();
            ProcedureDefinition = await databaseRepository.GetProcedureDefinitionAsync(
                connectionString,
                procedure.ObjectId) ?? "El código no está disponible (puede estar cifrado o faltar permiso).";
            SelectedProcedure = procedure;
            OnPropertyChanged(nameof(HasDefinition));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Could not load definition for procedure {ProcedureId}.", procedure.ObjectId);
            StatusMessage = $"No se pudo obtener el código del procedimiento: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SortProcedures()
    {
        var sorted = SelectedSort switch
        {
            "Promedio" => Procedures.OrderByDescending(item => item.AverageElapsedMs),
            "CPU" => Procedures.OrderByDescending(item => item.TotalCpuMs),
            "Ejecuciones" => Procedures.OrderByDescending(item => item.ExecutionCount),
            _ => Procedures.OrderByDescending(item => item.TotalElapsedMs)
        };

        var items = sorted.ToList();
        Procedures.Clear();
        foreach (var item in items)
        {
            Procedures.Add(item);
        }

        OnPropertyChanged(nameof(EmptyMessage));
    }
}
