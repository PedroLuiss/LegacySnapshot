using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LegacySnapshot.Models;

public partial class ConnectionModel : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Server { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Database { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UserName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AuthenticationType { get; set; } = "SQL Server";

    [ObservableProperty]
    public partial bool TrustServerCertificate { get; set; }

    [ObservableProperty]
    public partial string Environment { get; set; } = "Dev";

    [ObservableProperty]
    public partial bool IsFavorite { get; set; }

    [ObservableProperty]
    public partial DateTimeOffset? LastSuccessfulConnection { get; set; }

    [JsonIgnore]
    public Color EnvironmentColor => Environment switch
    {
        "Staging" => Colors.Goldenrod,
        "Prod" => Colors.IndianRed,
        _ => Colors.ForestGreen
    };

    [JsonIgnore]
    public string FavoriteIcon => IsFavorite ? "★" : "☆";

    partial void OnIsFavoriteChanged(bool value)
    {
        OnPropertyChanged(nameof(FavoriteIcon));
    }

    partial void OnEnvironmentChanged(string value)
    {
        OnPropertyChanged(nameof(EnvironmentColor));
    }
}
