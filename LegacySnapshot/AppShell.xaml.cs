using Microsoft.Extensions.DependencyInjection;

namespace LegacySnapshot
{
    public partial class AppShell : Shell
    {
        public AppShell(IServiceProvider services)
        {
            InitializeComponent();

            var tabBar = new TabBar();
            tabBar.Items.Add(CreateTab<Views.ConnectionsPage>(services, "Conexiones", "Connections"));
            tabBar.Items.Add(CreateTab<Views.TablesPage>(services, "Explorador", "Tables"));
            tabBar.Items.Add(CreateTab<Views.QueryRunnerPage>(services, "Query", "QueryRunner"));
            tabBar.Items.Add(CreateTab<Views.ProceduresPage>(services, "Procedimientos", "Procedures"));
            tabBar.Items.Add(CreateTab<Views.SettingsPage>(services, "Ajustes", "Settings"));
            Items.Add(tabBar);
        }

        private static ShellContent CreateTab<TPage>(
            IServiceProvider services,
            string title,
            string route)
            where TPage : Page
        {
            return new ShellContent
            {
                Title = title,
                Route = route,
                ContentTemplate = new DataTemplate(() => services.GetRequiredService<TPage>())
            };
        }
    }
}
