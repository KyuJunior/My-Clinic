using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicalApp.Models;
using MedicalApp.Services;

using Microsoft.Extensions.Configuration;

namespace MedicalApp.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ISharedStateService _sharedStateService;

        [ObservableProperty]
        private object? _currentView;

        [ObservableProperty]
        private Patient? _selectedPatient;

        [ObservableProperty]
        private string _connectionInfo = "Offline";

        // Use Lazy resolving to optimize startup time and instantiate only on-demand
        private readonly Lazy<PatientRegistrationViewModel> _patientRegistrationVm;
        private readonly Lazy<ClinicalExamViewModel> _clinicalExamVm;
        private readonly Lazy<EchoUploadViewModel> _echoUploadVm;

        public MainViewModel(IServiceProvider serviceProvider, ISharedStateService sharedStateService, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _sharedStateService = sharedStateService;

            // Parse connection string to display actual server/database source dynamically
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

            // Header patient info synchronization
            SelectedPatient = _sharedStateService.CurrentPatient;
            _sharedStateService.CurrentPatientChanged += (patient) => SelectedPatient = patient;

            _patientRegistrationVm = new Lazy<PatientRegistrationViewModel>(() => 
                (PatientRegistrationViewModel)_serviceProvider.GetService(typeof(PatientRegistrationViewModel))!);
            _clinicalExamVm = new Lazy<ClinicalExamViewModel>(() => 
                (ClinicalExamViewModel)_serviceProvider.GetService(typeof(ClinicalExamViewModel))!);
            _echoUploadVm = new Lazy<EchoUploadViewModel>(() => 
                (EchoUploadViewModel)_serviceProvider.GetService(typeof(EchoUploadViewModel))!);

            // Set default view on load
            NavigateToPatientRegistration();
        }

        [RelayCommand]
        public void NavigateToPatientRegistration()
        {
            CurrentView = _patientRegistrationVm.Value;
        }

        [RelayCommand]
        public void NavigateToClinicalExam()
        {
            CurrentView = _clinicalExamVm.Value;
        }

        [RelayCommand]
        public void NavigateToEchoUpload()
        {
            CurrentView = _echoUploadVm.Value;
        }
    }
}
