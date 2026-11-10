namespace LegacySnapshot.Views;

public partial class SettingsPage : ContentPage
{
    public static string[] ThemeOptions { get; } = ["Sistema", "Claro", "Oscuro"];
    private bool _isInitializing;

    public SettingsPage()
    {
        InitializeComponent();
        ThemePicker.ItemsSource = ThemeOptions;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _isInitializing = true;
        ThemePicker.SelectedItem = Preferences.Default.Get("app_theme", "Sistema");
        _isInitializing = false;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_isInitializing || ThemePicker.SelectedItem is not string selectedTheme)
        {
            return;
        }

        Preferences.Default.Set("app_theme", selectedTheme);
        if (Application.Current is not { } application)
        {
            return;
        }

        application.UserAppTheme = selectedTheme switch
        {
            "Claro" => AppTheme.Light,
            "Oscuro" => AppTheme.Dark,
            _ => AppTheme.Unspecified
        };
    }
}
