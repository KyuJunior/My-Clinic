using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalApp.ViewModels
{
    public partial class HomeLauncherViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;

        [ObservableProperty]
        private string _connectionInfo = "Offline";

        public HomeLauncherViewModel(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;

            var connString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
            if (connString.Contains("Data Source="))
            {
                string dbFile = connString.Split("Data Source=")[1].Split(';')[0];
                ConnectionInfo = $"SQLite ({dbFile})";
            }
            else if (connString.Contains("Server="))
            {
                string server = connString.Split("Server=")[1].Split(';')[0];
                ConnectionInfo = $"Server: {server}";
            }
            else
            {
                ConnectionInfo = "Local DB Mode";
            }
        }

        [RelayCommand]
        public void LaunchRegistry()
        {
            var vm = _serviceProvider.GetRequiredService<PatientRegistrationViewModel>();
            App.LaunchStandaloneWindow("Patient Registration Desk", vm, 500, 720);
        }

        [RelayCommand]
        public void LaunchExam()
        {
            var vm = _serviceProvider.GetRequiredService<ClinicalExamViewModel>();
            App.LaunchStandaloneWindow("Clinical Examination Room", vm, 950, 720);
        }

        [RelayCommand]
        public void LaunchEcho()
        {
            var vm = _serviceProvider.GetRequiredService<EchoUploadViewModel>();
            App.LaunchStandaloneWindow("Echocardiogram Hub", vm, 950, 720);
        }
    }
}
