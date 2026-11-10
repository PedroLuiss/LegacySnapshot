using CommunityToolkit.Maui;
using CommunityToolkit.Mvvm.Messaging;
using LegacySnapshot.Interfaces;
using LegacySnapshot.Services;
using LegacySnapshot.ViewModels;
using LegacySnapshot.Views;
using Microsoft.Extensions.Logging;

namespace LegacySnapshot
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            builder.Services.AddSingleton<AppShell>();
            builder.Services.AddSingleton<IConnectionRepository, LocalConnectionRepository>();
            builder.Services.AddSingleton<IDatabaseRepository, SqlServerDatabaseRepository>();
            builder.Services.AddSingleton<IActiveConnectionService, ActiveConnectionService>();
            builder.Services.AddSingleton<IQueryExecutor, SqlQueryExecutor>();
            builder.Services.AddSingleton<IQueryHistoryRepository, LocalQueryHistoryRepository>();
            builder.Services.AddSingleton<IExportStrategy, CsvExportStrategy>();
            builder.Services.AddTransient<ConnectionsViewModel>();
            builder.Services.AddTransient<TablesViewModel>();
            builder.Services.AddTransient<QueryRunnerViewModel>();
            builder.Services.AddTransient<ProceduresViewModel>();
            builder.Services.AddTransient<ConnectionsPage>();
            builder.Services.AddTransient<TablesPage>();
            builder.Services.AddTransient<QueryRunnerPage>();
            builder.Services.AddTransient<ProceduresPage>();
            builder.Services.AddTransient<SettingsPage>();
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
