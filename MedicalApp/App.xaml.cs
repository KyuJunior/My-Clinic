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

using Microsoft.Data.SqlClient;

namespace MedicalApp
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; } = null!;
        public IConfiguration Configuration { get; private set; } = null!;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set up Configuration builder to read appsettings.json and optional db_config.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("db_config.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();

            // Test the database connection before continuing
            var connectionString = Configuration.GetConnectionString("DefaultConnection") ?? "";
            try
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                // Connection failed! Open the configuration window
                var configWin = new MedicalApp.Views.DbConfigWindow(connectionString, ex.Message);
                if (configWin.ShowDialog() == true)
                {
                    // Save the new connection string
                    var newConnStr = configWin.SelectedConnectionString;
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "db_config.json");
                    var configContent = $"{{\n  \"ConnectionStrings\": {{\n    \"DefaultConnection\": \"{newConnStr.Replace("\\", "\\\\")}\"\n  }}\n}}";
                    File.WriteAllText(configPath, configContent);

                    // Relaunch the application with same arguments
                    var currentExecutable = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                    if (!string.IsNullOrEmpty(currentExecutable))
                    {
                        var args = string.Join(" ", e.Args);
                        System.Diagnostics.Process.Start(currentExecutable, args);
                    }
                }
                
                Shutdown();
                return;
            }

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

                // Add columns to Visits if they don't exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'Prescription') IS NULL ALTER TABLE dbo.Visits ADD Prescription NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsHR') IS NULL ALTER TABLE dbo.Visits ADD VitalsHR NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsSBP') IS NULL ALTER TABLE dbo.Visits ADD VitalsSBP NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsDBP') IS NULL ALTER TABLE dbo.Visits ADD VitalsDBP NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsRR') IS NULL ALTER TABLE dbo.Visits ADD VitalsRR NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsSPO2') IS NULL ALTER TABLE dbo.Visits ADD VitalsSPO2 NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VitalsTemp') IS NULL ALTER TABLE dbo.Visits ADD VitalsTemp NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'Investigation') IS NULL ALTER TABLE dbo.Visits ADD Investigation NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'Imaging') IS NULL ALTER TABLE dbo.Visits ADD Imaging NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'ReturnDate') IS NULL ALTER TABLE dbo.Visits ADD ReturnDate DATETIME NULL"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'InvestigationAttachmentPath') IS NULL ALTER TABLE dbo.Visits ADD InvestigationAttachmentPath NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'ImagingAttachmentPath') IS NULL ALTER TABLE dbo.Visits ADD ImagingAttachmentPath NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Job') IS NULL ALTER TABLE dbo.Patients ADD Job NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Governorate') IS NULL ALTER TABLE dbo.Patients ADD Governorate NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'AgeMonths') IS NULL ALTER TABLE dbo.Patients ADD AgeMonths INT NOT NULL DEFAULT 0"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'BirthDate') IS NULL ALTER TABLE dbo.Patients ADD BirthDate DATETIME NULL"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'SpouseBirthDate') IS NULL ALTER TABLE dbo.Patients ADD SpouseBirthDate DATETIME NULL"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'HasChildren') IS NULL ALTER TABLE dbo.Patients ADD HasChildren NVARCHAR(50) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Notes') IS NULL ALTER TABLE dbo.Patients ADD Notes NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'PatientFiles') IS NULL ALTER TABLE dbo.Patients ADD PatientFiles NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Weight') IS NULL ALTER TABLE dbo.Patients ADD Weight NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Height') IS NULL ALTER TABLE dbo.Patients ADD Height NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'MaritalStatus') IS NULL ALTER TABLE dbo.Patients ADD MaritalStatus NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'SpouseName') IS NULL ALTER TABLE dbo.Patients ADD SpouseName NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'BloodGroup') IS NULL ALTER TABLE dbo.Patients ADD BloodGroup NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Smoking') IS NULL ALTER TABLE dbo.Patients ADD Smoking NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'LastChildBirthDate') IS NULL ALTER TABLE dbo.Patients ADD LastChildBirthDate DATETIME NULL"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Alcohol') IS NULL ALTER TABLE dbo.Patients ADD Alcohol NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'MarriageDate') IS NULL ALTER TABLE dbo.Patients ADD MarriageDate DATETIME NULL"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'ReferredBy') IS NULL ALTER TABLE dbo.Patients ADD ReferredBy NVARCHAR(200) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'SpouseBloodGroup') IS NULL ALTER TABLE dbo.Patients ADD SpouseBloodGroup NVARCHAR(100) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Patients', 'Allergy') IS NULL ALTER TABLE dbo.Patients ADD Allergy NVARCHAR(MAX) NOT NULL DEFAULT ''"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'IsPaid') IS NULL ALTER TABLE dbo.Visits ADD IsPaid BIT NOT NULL DEFAULT 1"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'VisitPrice') IS NULL ALTER TABLE dbo.Visits ADD VisitPrice DECIMAL(18,2) NOT NULL DEFAULT 0"
                );

                // Add Doctors table if not exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF OBJECT_ID('dbo.Doctors', 'U') IS NULL " +
                    "CREATE TABLE dbo.Doctors (" +
                    "    DoctorId INT IDENTITY(1,1) PRIMARY KEY," +
                    "    Name NVARCHAR(200) NOT NULL UNIQUE," +
                    "    Specialty NVARCHAR(200) NOT NULL DEFAULT ''," +
                    "    Password NVARCHAR(200) NOT NULL DEFAULT 'YaserTheAdmin'" +
                    ")"
                );

                // Add Password column if missing in existing table
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Doctors', 'Password') IS NULL ALTER TABLE dbo.Doctors ADD Password NVARCHAR(200) NOT NULL DEFAULT 'YaserTheAdmin'"
                );

                // Seed default doctor if table empty
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF NOT EXISTS (SELECT 1 FROM dbo.Doctors) " +
                    "INSERT INTO dbo.Doctors (Name, Specialty, Password) VALUES ('Dr. Yaser', 'Obstetrics & Gynecology', 'YaserTheAdmin')"
                );

                // Add DoctorName column to QueueEntries and Visits if not exist
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.QueueEntries', 'DoctorName') IS NULL ALTER TABLE dbo.QueueEntries ADD DoctorName NVARCHAR(200) NOT NULL DEFAULT 'Dr. Yaser'"
                );
                await dbContext.Database.ExecuteSqlRawAsync(
                    "IF COL_LENGTH('dbo.Visits', 'DoctorName') IS NULL ALTER TABLE dbo.Visits ADD DoctorName NVARCHAR(200) NOT NULL DEFAULT 'Dr. Yaser'"
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
                        LaunchStandaloneWindow("Secretary Window", ServiceProvider.GetRequiredService<PatientRegistrationViewModel>(), 1100, 700);
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
            services.AddSingleton<IThemeService, ThemeService>();
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
