using System;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MedicalApp.Data;
using MedicalApp.Services;
using MedicalApp.ViewModels;
using MedicalApp.Views;

namespace MedicalApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up Configuration builder to read appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            // Ensure database is created on startup (perfect for SQLite zero-config testing)
            try
            {
                using var scope = ServiceProvider.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using var dbContext = await factory.CreateDbContextAsync();
                await dbContext.Database.EnsureCreatedAsync();

                // Create QueueEntries table if it doesn't exist (since EnsureCreated won't do it if the db already exists)
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF OBJECT_ID('dbo.QueueEntries', 'U') IS NULL " +
                    "CREATE TABLE dbo.QueueEntries (" +
                    "    QueueEntryId INT IDENTITY(1,1) PRIMARY KEY," +
                    "    PatientId INT NOT NULL," +
                    "    PatientName NVARCHAR(200) NOT NULL," +
                    "    Status NVARCHAR(50) NOT NULL," +
                    "    CreatedAt DATETIME NOT NULL DEFAULT GETUTCDATE()," +
                    "    CONSTRAINT FK_QueueEntries_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(PatientId) ON DELETE CASCADE" +
                    ")"
                );

                // Create Drugs table if it doesn't exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF OBJECT_ID('dbo.Drugs', 'U') IS NULL " +
                    "CREATE TABLE dbo.Drugs (" +
                    "    DrugId INT IDENTITY(1,1) PRIMARY KEY," +
                    "    Name NVARCHAR(200) NOT NULL UNIQUE" +
                    ")"
                );

                // Add Prescription column to Visits if it doesn't exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'Prescription') IS NULL " +
                    "ALTER TABLE dbo.Visits ADD Prescription NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to connect or migrate database on the local server:\n{ex.Message}", 
                                "Database Connection Error", 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                Shutdown();
                return;
            }

            // Check command line arguments for individual launching modes
            bool startOnHome = false;
            if (e.Args.Length > 0)
            {
                string mode = e.Args[0].ToLower();
                switch (mode)
                {
                    case "/reg":
                        LaunchStandaloneWindow("Secretary Window", ServiceProvider.GetRequiredService<PatientRegistrationViewModel>(), 500, 720);
                        return;
                    case "/exam":
                        LaunchStandaloneWindow("Doctor Window", ServiceProvider.GetRequiredService<ClinicalExamViewModel>(), 950, 720);
                        return;
                    case "/home":
                    case "--home":
                        startOnHome = true;
                        break;
                }
            }

            // Resolve and configure MainViewModel
            var mainViewModel = ServiceProvider.GetRequiredService<MainViewModel>();
            if (startOnHome)
            {
                mainViewModel.NavigateToHome();
            }
            else
            {
                mainViewModel.NavigateToPatientRegistration();
            }

            // Resolve and show MainWindow
            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        public static void LaunchStandaloneWindow(string title, object viewModel, double width, double height)
        {
            var app = (App)Application.Current;
            var window = new Window
            {
                Title = title,
                Content = viewModel, // WPF automatically uses the DataTemplate declared in App.xaml
                Width = width,
                Height = height,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Background = (System.Windows.Media.SolidColorBrush)app.FindResource("BackgroundColor")
            };

            // Dispose ViewModel when standalone window is closed to prevent memory leaks and endless polling
            window.Closed += (s, ev) =>
            {
                if (viewModel is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

            window.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register Configuration
            services.AddSingleton<IConfiguration>(Configuration);

            // Register AppDbContext with DbContextFactory for WPF concurrency safety using SQL Server
            var connectionString = Configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in appsettings.json.");
            
            services.AddDbContextFactory<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddSingleton<ISharedStateService, SharedStateService>();
            services.AddTransient<IPatientService, PatientService>();
            services.AddTransient<IVisitService, VisitService>();
            services.AddTransient<IQueueService, QueueService>();
            services.AddSingleton<IPrintService, PrintService>();

            // Register ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddTransient<HomeViewModel>();
            services.AddTransient<PatientRegistrationViewModel>();
            services.AddTransient<ClinicalExamViewModel>();
            services.AddTransient<PrintSettingsViewModel>();
            services.AddTransient<Views.PrintSettingsView>();

            // Register Main Window
            services.AddSingleton<MainWindow>(provider => new MainWindow
            {
                DataContext = provider.GetRequiredService<MainViewModel>()
            });
        }
    }
}
