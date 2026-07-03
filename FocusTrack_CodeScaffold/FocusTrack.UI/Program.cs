using System;
using System.IO;
using System.Windows.Forms;
using FocusTrack.Business.Interfaces;
using FocusTrack.Business.Services;
using FocusTrack.Data;
using FocusTrack.Data.Repositories;
using FocusTrack.UI.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FocusTrack.UI
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // Manual WinForms bootstrap. (ApplicationConfiguration.Initialize() only exists
            // when a project is scaffolded via `dotnet new winforms`, which auto-generates a
            // hidden ApplicationConfiguration.cs partial class — we don't have that file here.)
            System.Windows.Forms.Application.SetHighDpiMode(HighDpiMode.SystemAware);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            // Because this is a WinExe (no console window), an unhandled exception during
            // startup would otherwise just terminate the process with zero visible output.
            System.Windows.Forms.Application.ThreadException += (s, e) => ShowFatalError(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => ShowFatalError(e.ExceptionObject as Exception ?? new Exception("Unknown fatal error."));

            try
            {
                var services = new ServiceCollection();
                ConfigureServices(services);
                using var provider = services.BuildServiceProvider();

                // Ensure the SQLite schema exists / is migrated on startup.
                var dbFactory = provider.GetRequiredService<IDbContextFactory<FocusTrackDbContext>>();
                using (var db = dbFactory.CreateDbContext())
                {
                    db.Database.Migrate();
                }

                var mainForm = provider.GetRequiredService<MainForm>();
                System.Windows.Forms.Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                ShowFatalError(ex);
            }
        }

        private static void ShowFatalError(Exception ex)
        {
            MessageBox.Show(
                $"FocusTrack failed to start:\n\n{ex}",
                "FocusTrack — Fatal Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private static void ConfigureServices(ServiceCollection services)
        {
            string dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FocusTrack", "focustrack.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

            services.AddSingleton<IDbContextFactory<FocusTrackDbContext>>(
                _ => new SimpleDbContextFactory($"Data Source={dbPath}"));

            // Data layer
            services.AddSingleton<ISessionRepository, SessionRepository>();
            services.AddSingleton<IApplicationRepository, ApplicationRepository>();
            services.AddSingleton<ICategoryRepository, CategoryRepository>();

            // Business layer
            services.AddSingleton<IWindowTrackerService, WindowTrackerService>();
            services.AddSingleton<ICategoryService, CategoryService>();
            services.AddSingleton<IReportService, ReportService>();

            // UI layer — TrayIconManager is independent of MainForm so there is no
            // circular dependency between it and INotificationService.
            services.AddSingleton<TrayIconManager>();
            services.AddSingleton<INotificationService>(sp =>
                new NotificationService(sp.GetRequiredService<TrayIconManager>().Icon));
            services.AddSingleton<MainForm>();

            // Child forms hosted inline inside MainForm's TabControl (see
            // MainForm.CreateHosted<TForm>()). These MUST be registered here — without
            // this, _serviceProvider.GetService(typeof(TForm)) returns null and MainForm's
            // constructor throws a NullReferenceException before any window ever appears.
            services.AddTransient<DashboardForm>();
            services.AddTransient<HistoryForm>();
            services.AddTransient<ReportsForm>();
            services.AddTransient<SettingsForm>();
        }
    }
}

