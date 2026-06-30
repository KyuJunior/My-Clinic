using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicalApp.Models;
using MedicalApp.Services;

namespace MedicalApp.ViewModels
{
    public partial class PatientRegistrationViewModel : ObservableObject
    {
        private readonly IPatientService _patientService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IQueueService _queueService;

        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Patient> _patients = new();

        [ObservableProperty]
        private Patient? _selectedPatient;

        // Registration form fields
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private int _age;

        [ObservableProperty]
        private string _gender = "Male";

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public PatientRegistrationViewModel(IPatientService patientService, ISharedStateService sharedStateService, IQueueService queueService)
        {
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            _queueService = queueService;
            
            // Sync with current selection
            SelectedPatient = _sharedStateService.CurrentPatient;
            
            // Load initial patients asynchronously
            _ = LoadPatientsAsync();
        }

        // When selection changes, update the shared singleton state
        partial void OnSelectedPatientChanged(Patient? value)
        {
            _sharedStateService.CurrentPatient = value;
        }

        [RelayCommand]
        public async Task LoadPatientsAsync()
        {
            try
            {
                var patients = await _patientService.GetAllPatientsAsync();
                Patients = new ObservableCollection<Patient>(patients);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading patients: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SearchAsync()
        {
            try
            {
                var results = await _patientService.SearchPatientsAsync(SearchTerm);
                Patients = new ObservableCollection<Patient>(results);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task RegisterAsync()
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                StatusMessage = "Patient Name is required.";
                return;
            }

            try
            {
                var patient = new Patient
                {
                    Name = Name,
                    Age = Age,
                    Gender = Gender,
                    Address = Address,
                    Phone = Phone,
                    CreatedAt = DateTime.UtcNow
                };

                await _patientService.AddPatientAsync(patient);
                
                // Add registered patient to daily queue automatically
                await _queueService.AddToQueueAsync(patient.PatientId, patient.Name);
                
                StatusMessage = $"Patient '{Name}' registered & added to waitlist queue!";
                
                // Clear Form
                Name = string.Empty;
                Age = 0;
                Address = string.Empty;
                Phone = string.Empty;

                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error registering patient: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SendToQueueAsync()
        {
            if (SelectedPatient == null)
            {
                StatusMessage = "Please select a patient to queue.";
                return;
            }

            try
            {
                await _queueService.AddToQueueAsync(SelectedPatient.PatientId, SelectedPatient.Name);
                StatusMessage = $"Patient '{SelectedPatient.Name}' added to waitlist queue!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error queueing patient: {ex.Message}";
            }
        }
    }
}
