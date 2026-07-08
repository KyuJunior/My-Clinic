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
        private ObservableCollection<QueueEntry> _incompleteVisits = new();

        [ObservableProperty]
        private bool _showRegistrationModal = false;

        // Check-in properties
        [ObservableProperty]
        private bool _showCheckInModal = false;

        [ObservableProperty]
        private bool _isPaidVisit = true;

        [ObservableProperty]
        private bool _isFreeVisit = false;

        partial void OnIsPaidVisitChanged(bool value)
        {
            if (value == IsFreeVisit)
            {
                IsFreeVisit = !value;
            }
        }

        partial void OnIsFreeVisitChanged(bool value)
        {
            if (value == IsPaidVisit)
            {
                IsPaidVisit = !value;
            }
        }

        [ObservableProperty]
        private string _visitPrice = "25000";

        [ObservableProperty]
        private Patient? _pendingCheckInPatient;

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

        // New Detailed Patient Profile fields
        [ObservableProperty]
        private string _weight = string.Empty;

        [ObservableProperty]
        private string _height = string.Empty;

        [ObservableProperty]
        private string _maritalStatus = string.Empty;

        [ObservableProperty]
        private string _spouseName = string.Empty;

        [ObservableProperty]
        private string _bloodGroup = string.Empty;

        [ObservableProperty]
        private string _smoking = string.Empty;

        [ObservableProperty]
        private DateTime? _lastChildBirthDate;

        [ObservableProperty]
        private string _alcohol = string.Empty;

        [ObservableProperty]
        private DateTime? _marriageDate;

        [ObservableProperty]
        private string _referredBy = string.Empty;

        [ObservableProperty]
        private string _spouseBloodGroup = string.Empty;

        [ObservableProperty]
        private string _allergy = string.Empty;

        // The show/hide switches
        [ObservableProperty]
        private bool _isGearMenuOpen = false;

        [ObservableProperty]
        private bool _isGridGearOpen = false;

        [RelayCommand]
        public void ToggleGridGear()
        {
            IsGridGearOpen = !IsGridGearOpen;
        }

        [ObservableProperty]
        private bool _showPatientId = true;

        [ObservableProperty]
        private bool _showName = true;

        [ObservableProperty]
        private bool _showAge = true;

        [ObservableProperty]
        private bool _showGender = true;

        [ObservableProperty]
        private bool _showGovernorate = true;

        [ObservableProperty]
        private bool _showPhone = true;

        [ObservableProperty]
        private bool _showWeight = false;

        [ObservableProperty]
        private bool _showMaritalStatus = false;

        [ObservableProperty]
        private bool _showVisitsCount = true;

        [ObservableProperty]
        private bool _showSpouseName = false;

        [ObservableProperty]
        private bool _showBloodGroup = false;

        [ObservableProperty]
        private bool _showSmoking = false;

        [ObservableProperty]
        private bool _showLastChildBirthDate = false;

        [ObservableProperty]
        private bool _showAlcohol = false;

        [ObservableProperty]
        private bool _showMarriageDate = false;

        [ObservableProperty]
        private bool _showJob = true;

        [ObservableProperty]
        private bool _showAddress = true;

        [ObservableProperty]
        private bool _showReferredBy = false;

        [ObservableProperty]
        private bool _showHeight = false;

        [ObservableProperty]
        private bool _showReturnDate = true;

        [ObservableProperty]
        private bool _showLastVisit = true;

        [ObservableProperty]
        private bool _showSpouseBirthDate = true;

        [ObservableProperty]
        private bool _showSpouseBloodGroup = false;

        [ObservableProperty]
        private bool _showHasChildren = true;

        [ObservableProperty]
        private bool _showNotes = true;

        [ObservableProperty]
        private bool _showAllergy = true;

        public PatientRegistrationViewModel(IPatientService patientService, ISharedStateService sharedStateService, IQueueService queueService)
        {
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            _queueService = queueService;
            
            // Sync with current selection
            SelectedPatient = _sharedStateService.CurrentPatient;
            
            // Load column visibilities preferences
            LoadPreferences();

            // Load initial patients asynchronously
            _ = LoadPatientsAsync();

            // Periodically refresh the waitlist and incomplete queue
            _ = PollQueueAsync();
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

                // Waitlist list for left sidebar (status Pending)
                WaitingVisits = new ObservableCollection<QueueEntry>(System.Linq.Enumerable.Where(activeQueue, q => q.Status == "Pending"));
                
                // Incomplete list for left sidebar (status InExam or InEcho)
                IncompleteVisits = new ObservableCollection<QueueEntry>(System.Linq.Enumerable.Where(activeQueue, q => q.Status == "InExam" || q.Status == "InEcho"));
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading data: {ex.Message}";
            }
        }

        private async Task PollQueueAsync()
        {
            while (true)
            {
                try
                {
                    var activeQueue = await _queueService.GetActiveQueueAsync();
                    
                    // Update stats & waitlists safely on dispatcher thread
                    if (System.Windows.Application.Current != null)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            AttendingPatientsCount = System.Linq.Enumerable.Count(activeQueue);
                            WaitingPatientsCount = System.Linq.Enumerable.Count(activeQueue, q => q.Status == "Pending");
                            
                            WaitingVisits = new ObservableCollection<QueueEntry>(System.Linq.Enumerable.Where(activeQueue, q => q.Status == "Pending"));
                            IncompleteVisits = new ObservableCollection<QueueEntry>(System.Linq.Enumerable.Where(activeQueue, q => q.Status == "InExam" || q.Status == "InEcho"));
                        });
                    }
                }
                catch
                {
                    // Ignore transient network/DB errors during background poll
                }
                await Task.Delay(3000);
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
                
                Weight = patient.Weight;
                Height = patient.Height;
                MaritalStatus = patient.MaritalStatus;
                SpouseName = patient.SpouseName;
                BloodGroup = patient.BloodGroup;
                Smoking = patient.Smoking;
                LastChildBirthDate = patient.LastChildBirthDate;
                Alcohol = patient.Alcohol;
                MarriageDate = patient.MarriageDate;
                ReferredBy = patient.ReferredBy;
                SpouseBloodGroup = patient.SpouseBloodGroup;
                Allergy = patient.Allergy;

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
                
                Weight = string.Empty;
                Height = string.Empty;
                MaritalStatus = string.Empty;
                SpouseName = string.Empty;
                BloodGroup = string.Empty;
                Smoking = string.Empty;
                LastChildBirthDate = null;
                Alcohol = string.Empty;
                MarriageDate = null;
                ReferredBy = string.Empty;
                SpouseBloodGroup = string.Empty;
                Allergy = string.Empty;

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
        public void PrepareCheckIn(Patient patient)
        {
            if (patient == null) return;
            PendingCheckInPatient = patient;
            IsPaidVisit = true;
            VisitPrice = "25000";
            ShowCheckInModal = true;
        }

        [RelayCommand]
        public void CancelCheckIn()
        {
            ShowCheckInModal = false;
            PendingCheckInPatient = null;
        }

        [RelayCommand]
        public async Task ConfirmCheckInAsync()
        {
            if (PendingCheckInPatient == null) return;

            try
            {
                decimal price = 0;
                if (IsPaidVisit)
                {
                    decimal.TryParse(VisitPrice, out price);
                }

                // Create the visit record for today
                var visit = new Visit
                {
                    PatientId = PendingCheckInPatient.PatientId,
                    VisitDate = DateTime.UtcNow, // Date & Time today
                    IsPaid = IsPaidVisit,
                    VisitPrice = price
                };

                await _patientService.AddVisitForCheckInAsync(visit);
                
                // Add patient to daily queue
                await _queueService.AddToQueueAsync(PendingCheckInPatient.PatientId, PendingCheckInPatient.Name);
                
                StatusMessage = $"Checked in '{PendingCheckInPatient.Name}' and added to queue.";
                ShowCheckInModal = false;
                PendingCheckInPatient = null;

                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Check-in error: {ex.Message}";
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
                Patient savedPatient;
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

                    ActiveEditingPatient.Weight = Weight;
                    ActiveEditingPatient.Height = Height;
                    ActiveEditingPatient.MaritalStatus = MaritalStatus;
                    ActiveEditingPatient.SpouseName = SpouseName;
                    ActiveEditingPatient.BloodGroup = BloodGroup;
                    ActiveEditingPatient.Smoking = Smoking;
                    ActiveEditingPatient.LastChildBirthDate = LastChildBirthDate;
                    ActiveEditingPatient.Alcohol = Alcohol;
                    ActiveEditingPatient.MarriageDate = MarriageDate;
                    ActiveEditingPatient.ReferredBy = ReferredBy;
                    ActiveEditingPatient.SpouseBloodGroup = SpouseBloodGroup;
                    ActiveEditingPatient.Allergy = Allergy;

                    await _patientService.UpdatePatientAsync(ActiveEditingPatient);
                    savedPatient = ActiveEditingPatient;
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
                        Weight = Weight,
                        Height = Height,
                        MaritalStatus = MaritalStatus,
                        SpouseName = SpouseName,
                        BloodGroup = BloodGroup,
                        Smoking = Smoking,
                        LastChildBirthDate = LastChildBirthDate,
                        Alcohol = Alcohol,
                        MarriageDate = MarriageDate,
                        ReferredBy = ReferredBy,
                        SpouseBloodGroup = SpouseBloodGroup,
                        Allergy = Allergy,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _patientService.AddPatientAsync(patient);
                    savedPatient = patient;
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
                    
                    Weight = string.Empty;
                    Height = string.Empty;
                    MaritalStatus = string.Empty;
                    SpouseName = string.Empty;
                    BloodGroup = string.Empty;
                    Smoking = string.Empty;
                    LastChildBirthDate = null;
                    Alcohol = string.Empty;
                    MarriageDate = null;
                    ReferredBy = string.Empty;
                    SpouseBloodGroup = string.Empty;
                    Allergy = string.Empty;

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
                
                // Trigger check-in dialog instead of immediate queue
                PrepareCheckIn(savedPatient);
                
                await LoadPatientsAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving patient: {ex.Message}";
            }
        }

        [RelayCommand]
        public void SendToQueue(Patient? patient)
        {
            var target = patient ?? SelectedPatient;
            if (target == null)
            {
                StatusMessage = "Please select a patient to check-in.";
                return;
            }
            PrepareCheckIn(target);
        }

        [RelayCommand]
        public void OpenRegistrationModal()
        {
            CancelEdit();
            ShowRegistrationModal = true;
        }

        [RelayCommand]
        public void CloseRegistrationModal()
        {
            ShowRegistrationModal = false;
        }

        private string GetPreferencesFilePath()
        {
            var folder = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "MyClinic");
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            return System.IO.Path.Combine(folder, "column_preferences.json");
        }

        private void LoadPreferences()
        {
            try
            {
                var filePath = GetPreferencesFilePath();
                if (System.IO.File.Exists(filePath))
                {
                    var json = System.IO.File.ReadAllText(filePath);
                    var prefs = System.Text.Json.JsonSerializer.Deserialize<ColumnPreferences>(json);
                    if (prefs != null)
                    {
                        ShowPatientId = prefs.ShowPatientId;
                        ShowName = prefs.ShowName;
                        ShowAge = prefs.ShowAge;
                        ShowGender = prefs.ShowGender;
                        ShowJob = prefs.ShowJob;
                        ShowAddress = prefs.ShowAddress;
                        ShowGovernorate = prefs.ShowGovernorate;
                        ShowPhone = prefs.ShowPhone;
                        ShowWeight = prefs.ShowWeight;
                        ShowHeight = prefs.ShowHeight;
                        ShowMaritalStatus = prefs.ShowMaritalStatus;
                        ShowSpouseName = prefs.ShowSpouseName;
                        ShowBloodGroup = prefs.ShowBloodGroup;
                        ShowSmoking = prefs.ShowSmoking;
                        ShowLastChildBirthDate = prefs.ShowLastChildBirthDate;
                        ShowAlcohol = prefs.ShowAlcohol;
                        ShowMarriageDate = prefs.ShowMarriageDate;
                        ShowReferredBy = prefs.ShowReferredBy;
                        ShowReturnDate = prefs.ShowReturnDate;
                        ShowLastVisit = prefs.ShowLastVisit;
                        ShowSpouseBirthDate = prefs.ShowSpouseBirthDate;
                        ShowSpouseBloodGroup = prefs.ShowSpouseBloodGroup;
                        ShowHasChildren = prefs.ShowHasChildren;
                        ShowNotes = prefs.ShowNotes;
                        ShowAllergy = prefs.ShowAllergy;
                        ShowVisitsCount = prefs.ShowVisitsCount;
                    }
                }
            }
            catch
            {
                // Fallback to default values
            }
        }

        [RelayCommand]
        public void SavePreferences()
        {
            try
            {
                var filePath = GetPreferencesFilePath();
                var prefs = new ColumnPreferences
                {
                    ShowPatientId = ShowPatientId,
                    ShowName = ShowName,
                    ShowAge = ShowAge,
                    ShowGender = ShowGender,
                    ShowJob = ShowJob,
                    ShowAddress = ShowAddress,
                    ShowGovernorate = ShowGovernorate,
                    ShowPhone = ShowPhone,
                    ShowWeight = ShowWeight,
                    ShowHeight = ShowHeight,
                    ShowMaritalStatus = ShowMaritalStatus,
                    ShowSpouseName = ShowSpouseName,
                    ShowBloodGroup = ShowBloodGroup,
                    ShowSmoking = ShowSmoking,
                    ShowLastChildBirthDate = ShowLastChildBirthDate,
                    ShowAlcohol = ShowAlcohol,
                    ShowMarriageDate = ShowMarriageDate,
                    ShowReferredBy = ShowReferredBy,
                    ShowReturnDate = ShowReturnDate,
                    ShowLastVisit = ShowLastVisit,
                    ShowSpouseBirthDate = ShowSpouseBirthDate,
                    ShowSpouseBloodGroup = ShowSpouseBloodGroup,
                    ShowHasChildren = ShowHasChildren,
                    ShowNotes = ShowNotes,
                    ShowAllergy = ShowAllergy,
                    ShowVisitsCount = ShowVisitsCount
                };
                var json = System.Text.Json.JsonSerializer.Serialize(prefs);
                System.IO.File.WriteAllText(filePath, json);
                StatusMessage = "تم حفظ إعدادات الأعمدة بنجاح!";
                IsGridGearOpen = false;
                IsGearMenuOpen = false;
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ أثناء حفظ الإعدادات: {ex.Message}";
            }
        }
    }

    public class ColumnPreferences
    {
        public bool ShowPatientId { get; set; } = true;
        public bool ShowName { get; set; } = true;
        public bool ShowAge { get; set; } = true;
        public bool ShowGender { get; set; } = true;
        public bool ShowJob { get; set; } = true;
        public bool ShowAddress { get; set; } = true;
        public bool ShowGovernorate { get; set; } = true;
        public bool ShowPhone { get; set; } = true;
        public bool ShowWeight { get; set; } = false;
        public bool ShowHeight { get; set; } = false;
        public bool ShowMaritalStatus { get; set; } = false;
        public bool ShowSpouseName { get; set; } = false;
        public bool ShowBloodGroup { get; set; } = false;
        public bool ShowSmoking { get; set; } = false;
        public bool ShowLastChildBirthDate { get; set; } = false;
        public bool ShowAlcohol { get; set; } = false;
        public bool ShowMarriageDate { get; set; } = false;
        public bool ShowReferredBy { get; set; } = false;
        public bool ShowReturnDate { get; set; } = true;
        public bool ShowLastVisit { get; set; } = true;
        public bool ShowSpouseBirthDate { get; set; } = true;
        public bool ShowSpouseBloodGroup { get; set; } = false;
        public bool ShowHasChildren { get; set; } = true;
        public bool ShowNotes { get; set; } = true;
        public bool ShowAllergy { get; set; } = true;
        public bool ShowVisitsCount { get; set; } = true;
    }
}
