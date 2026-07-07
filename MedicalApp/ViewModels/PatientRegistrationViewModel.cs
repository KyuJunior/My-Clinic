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
        private bool _isAutofilling = false;

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
        private string _job = string.Empty;

        [ObservableProperty]
        private string _governorate = string.Empty;

        [ObservableProperty]
        private string _address = string.Empty;

        [ObservableProperty]
        private string _phone = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Patient> _nameSuggestions = new();

        [ObservableProperty]
        private bool _isSuggestionsOpen = false;

        [ObservableProperty]
        private string _registrationButtonText = "Register & Send to Queue";

        [ObservableProperty]
        private Patient? _activeEditingPatient;

        // Statistics
        [ObservableProperty]
        private int _totalPatientsCount;

        [ObservableProperty]
        private int _newPatientsTodayCount;

        [ObservableProperty]
        private int _attendingPatientsCount;

        [ObservableProperty]
        private int _waitingPatientsCount;

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _waitingVisits = new();

        [ObservableProperty]
        private bool _showRegistrationModal = false;

        // Advanced registration fields
        [ObservableProperty]
        private int _ageMonths;

        [ObservableProperty]
        private DateTime? _birthDate;

        [ObservableProperty]
        private DateTime? _spouseBirthDate;

        [ObservableProperty]
        private string _hasChildren = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PatientFileName))]
        private string _patientFiles = string.Empty;

        public string PatientFileName => string.IsNullOrEmpty(PatientFiles) ? string.Empty : System.IO.Path.GetFileName(PatientFiles);

        // Section Toggles
        [ObservableProperty]
        private bool _isGearMenuOpen = false;

        [ObservableProperty]
        private bool _showSpouseAndKids = true;

        [ObservableProperty]
        private bool _showSecondaryAge = true;

        [ObservableProperty]
        private bool _showExtraContact = true;

        [ObservableProperty]
        private bool _showNotesAndFiles = true;

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

                // Load active queue entries
                var activeQueue = await _queueService.GetActiveQueueAsync();
                
                // Attending count is total daily entries
                AttendingPatientsCount = System.Linq.Enumerable.Count(activeQueue);
                
                // Waiting count is those pending
                WaitingPatientsCount = System.Linq.Enumerable.Count(activeQueue, q => q.Status == "Pending");
                
                // Total patients
                TotalPatientsCount = Patients.Count;
                
                // New patients today
                NewPatientsTodayCount = System.Linq.Enumerable.Count(Patients, p => p.CreatedAt.Date == DateTime.Today);

                // Waitlist list for left sidebar
                WaitingVisits = new ObservableCollection<QueueEntry>(activeQueue);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
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

        partial void OnNameChanged(string value)
        {
            if (_isAutofilling) return;

            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            {
                NameSuggestions.Clear();
                IsSuggestionsOpen = false;
            }
            else
            {
                _ = QueryNameSuggestionsAsync(value);
            }
        }

        private async Task QueryNameSuggestionsAsync(string query)
        {
            try
            {
                var results = await _patientService.SearchPatientsAsync(query);
                var list = System.Linq.Enumerable.ToList(results);
                
                // Show suggestions only if they don't match our active editing patient name
                if (list.Count > 0 && (ActiveEditingPatient == null || ActiveEditingPatient.Name != query))
                {
                    NameSuggestions = new ObservableCollection<Patient>(list);
                    IsSuggestionsOpen = true;
                }
                else
                {
                    NameSuggestions.Clear();
                    IsSuggestionsOpen = false;
                }
            }
            catch
            {
                // Suppress background query errors
            }
        }

        [RelayCommand]
        public void ToggleGearMenu()
        {
            IsGearMenuOpen = !IsGearMenuOpen;
        }

        [RelayCommand]
        public void UploadPatientFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|PDF Files (*.pdf)|*.pdf|Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sourcePath = openFileDialog.FileName;
                    var extension = System.IO.Path.GetExtension(sourcePath);
                    var destDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PatientFiles");
                    if (!System.IO.Directory.Exists(destDir))
                    {
                        System.IO.Directory.CreateDirectory(destDir);
                    }

                    var uniqueName = $"patient_{Guid.NewGuid()}{extension}";
                    var destPath = System.IO.Path.Combine(destDir, uniqueName);
                    
                    System.IO.File.Copy(sourcePath, destPath, overwrite: true);
                    PatientFiles = destPath;
                    StatusMessage = "Patient document uploaded successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to upload file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemovePatientFile()
        {
            try
            {
                if (System.IO.File.Exists(PatientFiles))
                {
                    System.IO.File.Delete(PatientFiles);
                }
            }
            catch {}
            PatientFiles = string.Empty;
        }

        [RelayCommand]
        public void OpenPatientFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !System.IO.File.Exists(filePath))
            {
                StatusMessage = "File not found.";
                return;
            }

            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Could not open file: {ex.Message}";
            }
        }

        [RelayCommand]
        public void LoadExistingPatient(Patient patient)
        {
            if (patient == null) return;

            _isAutofilling = true;
            try
            {
                Name = patient.Name;
                Age = patient.Age;
                Gender = patient.Gender;
                Address = patient.Address;
                Phone = patient.Phone;
                Job = patient.Job;
                Governorate = patient.Governorate;
                AgeMonths = patient.AgeMonths;
                BirthDate = patient.BirthDate;
                SpouseBirthDate = patient.SpouseBirthDate;
                HasChildren = patient.HasChildren;
                Notes = patient.Notes;
                PatientFiles = patient.PatientFiles;
                
                ActiveEditingPatient = patient;
                RegistrationButtonText = "Update & Send to Queue";
                IsSuggestionsOpen = false;
                NameSuggestions.Clear();
                ShowRegistrationModal = true;
                StatusMessage = $"Loaded existing patient details: '{patient.Name}'";
            }
            finally
            {
                _isAutofilling = false;
            }
        }

        [RelayCommand]
        public void CancelEdit()
        {
            _isAutofilling = true;
            try
            {
                Name = string.Empty;
                Age = 0;
                Gender = "Male";
                Address = string.Empty;
                Phone = string.Empty;
                Job = string.Empty;
                Governorate = string.Empty;
                AgeMonths = 0;
                BirthDate = null;
                SpouseBirthDate = null;
                HasChildren = string.Empty;
                Notes = string.Empty;
                PatientFiles = string.Empty;
                
                ActiveEditingPatient = null;
                RegistrationButtonText = "Register & Send to Queue";
                IsSuggestionsOpen = false;
                NameSuggestions.Clear();
                StatusMessage = "Cleared fields. Switched to new registration.";
            }
            finally
            {
                _isAutofilling = false;
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
                if (ActiveEditingPatient != null)
                {
                    // Update existing patient
                    ActiveEditingPatient.Name = Name;
                    ActiveEditingPatient.Age = Age;
                    ActiveEditingPatient.Gender = Gender;
                    ActiveEditingPatient.Address = Address;
                    ActiveEditingPatient.Phone = Phone;
                    ActiveEditingPatient.Job = Job;
                    ActiveEditingPatient.Governorate = Governorate;
                    ActiveEditingPatient.AgeMonths = AgeMonths;
                    ActiveEditingPatient.BirthDate = BirthDate;
                    ActiveEditingPatient.SpouseBirthDate = SpouseBirthDate;
                    ActiveEditingPatient.HasChildren = HasChildren;
                    ActiveEditingPatient.Notes = Notes;
                    ActiveEditingPatient.PatientFiles = PatientFiles;

                    await _patientService.UpdatePatientAsync(ActiveEditingPatient);
                    
                    // Add/refresh in daily queue
                    await _queueService.AddToQueueAsync(ActiveEditingPatient.PatientId, ActiveEditingPatient.Name);
                    
                    StatusMessage = $"Patient '{ActiveEditingPatient.Name}' details updated & added to queue!";
                }
                else
                {
                    // Create new patient
                    var patient = new Patient
                    {
                        Name = Name,
                        Age = Age,
                        Gender = Gender,
                        Address = Address,
                        Phone = Phone,
                        Job = Job,
                        Governorate = Governorate,
                        AgeMonths = AgeMonths,
                        BirthDate = BirthDate,
                        SpouseBirthDate = SpouseBirthDate,
                        HasChildren = HasChildren,
                        Notes = Notes,
                        PatientFiles = PatientFiles,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _patientService.AddPatientAsync(patient);
                    
                    // Add registered patient to daily queue automatically
                    await _queueService.AddToQueueAsync(patient.PatientId, patient.Name);
                    
                    StatusMessage = $"Patient '{Name}' registered & added to waitlist queue!";
                }
                
                // Clear Form
                _isAutofilling = true;
                try
                {
                    Name = string.Empty;
                    Age = 0;
                    Address = string.Empty;
                    Phone = string.Empty;
                    Job = string.Empty;
                    Governorate = string.Empty;
                    AgeMonths = 0;
                    BirthDate = null;
                    SpouseBirthDate = null;
                    HasChildren = string.Empty;
                    Notes = string.Empty;
                    PatientFiles = string.Empty;
                    ActiveEditingPatient = null;
                    RegistrationButtonText = "Register & Send to Queue";
                    IsSuggestionsOpen = false;
                    NameSuggestions.Clear();
                }
                finally
                {
                    _isAutofilling = false;
                }

                ShowRegistrationModal = false;
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving patient: {ex.Message}";
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
