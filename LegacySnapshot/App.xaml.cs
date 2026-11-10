namespace LegacySnapshot
{
    public partial class App : Application
    {
        private readonly AppShell _appShell;

        public App(AppShell appShell)
        {
            InitializeComponent();
            _appShell = appShell;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var savedTheme = Preferences.Default.Get("app_theme", "Sistema");
            UserAppTheme = savedTheme switch
            {
                "Claro" => AppTheme.Light,
                "Oscuro" => AppTheme.Dark,
                _ => AppTheme.Unspecified
            };
            return new Window(_appShell);
        }
    }
}
