using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicalApp.Models;
using MedicalApp.Services;

using Microsoft.EntityFrameworkCore;
using System.Windows;

namespace MedicalApp.ViewModels
{
    public partial class ClinicalExamViewModel : ObservableObject, IDisposable
    {
        private readonly IVisitService _visitService;
        private readonly IPatientService _patientService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IQueueService _queueService;
        private readonly IPrintService _printService;
        private readonly IDbContextFactory<Data.AppDbContext> _dbContextFactory;
        private readonly System.Windows.Threading.DispatcherTimer _pollingTimer;
        private readonly IThemeService _themeService;

        public bool IsDarkMode => _themeService.IsDarkMode;

        [ObservableProperty]
        private string _activeDoctorName = "Dr. Yaser";

        [ObservableProperty]
        private ObservableCollection<Doctor> _doctors = new();

        [ObservableProperty]
        private bool _showSwitchDoctorPasswordModal = false;

        [ObservableProperty]
        private string _switchDoctorPasswordAttempt = string.Empty;

        [ObservableProperty]
        private Doctor? _selectedSwitchDoctor = null;

        [ObservableProperty]
        private string _switchDoctorErrorMessage = string.Empty;

        [ObservableProperty]
        private bool _showAddPatientModal = false;

        [ObservableProperty]
        private string _newPatientName = string.Empty;

        [ObservableProperty]
        private int _newPatientAge = 0;

        [ObservableProperty]
        private int _newPatientAgeMonths = 0;

        [ObservableProperty]
        private string _newPatientGender = "ذكر";

        [ObservableProperty]
        private string _newPatientPhone = string.Empty;

        [ObservableProperty]
        private DateTime? _newPatientBirthDate = null;

        [ObservableProperty]
        private string _newPatientGovernorate = "بغداد";

        [ObservableProperty]
        private string _newPatientFiles = string.Empty;

        [ObservableProperty]
        private bool _newPatientIsPaidVisit = true;

        [ObservableProperty]
        private bool _newPatientIsFreeVisit = false;

        [ObservableProperty]
        private string _newPatientVisitPrice = "25000";

        [ObservableProperty]
        private string _addPatientValidationMessage = string.Empty;

        public string NewPatientFileName => string.IsNullOrEmpty(NewPatientFiles) ? "لا يوجد ملف" : System.IO.Path.GetFileName(NewPatientFiles);

        partial void OnNewPatientFilesChanged(string value)
        {
            OnPropertyChanged(nameof(NewPatientFileName));
        }

        [RelayCommand]
        public void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        [RelayCommand]
        public void OpenStartNewVisitWindow()
        {
            var window = new MedicalApp.Views.StartNewVisitWindow(_patientService)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            
            if (window.ShowDialog() == true)
            {
                _ = PollQueueAsync();
            }
        }

        private static readonly string DraftsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "session_drafts.json");
        private bool _isSavingOrLoadingDraft;

        [ObservableProperty]
        private Patient? _currentPatient;

        [ObservableProperty]
        private ObservableCollection<Visit> _visitHistory = new();

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _activeQueue = new();

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _waitingPatients = new();

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _notFinishedPatients = new();

        [ObservableProperty]
        private int _completedCountToday;

        [ObservableProperty]
        private bool _isQueueTabActive = true;

        [ObservableProperty]
        private bool _isSoonTabActive;

        // Standalone Patient Lookup fields
        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Patient> _patients = new();

        [ObservableProperty]
        private Patient? _selectedPatientLookup;

        // Visit Form Fields
        [ObservableProperty]
        private string _chiefComplaint = string.Empty;

        [ObservableProperty]
        private string _historyOfPresentIllness = string.Empty;

        [ObservableProperty]
        private string _physicalExamination = string.Empty;

        [ObservableProperty]
        private string _diagnosis = string.Empty;

        [ObservableProperty]
        private string _treatmentPlan = string.Empty;

        [ObservableProperty]
        private string _drugSearchText = string.Empty;

        [ObservableProperty]
        private ObservableCollection<string> _drugSuggestions = new();

        [ObservableProperty]
        private bool _isDrugSuggestionsOpen;

        [ObservableProperty]
        private ObservableCollection<PrescribedMedication> _prescribedDrugs = new();

        public bool HasNoPrescribedDrugs => PrescribedDrugs == null || PrescribedDrugs.Count == 0;
        public bool HasPrescribedDrugs => PrescribedDrugs != null && PrescribedDrugs.Count > 0;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // View Toggling Settings (Show/Hide sections)
        [ObservableProperty]
        private bool _showComplaint = true;

        [ObservableProperty]
        private bool _showPhysicalExam = true;

        [ObservableProperty]
        private bool _showVitals = true;

        [ObservableProperty]
        private bool _showInvestigation = true;

        [ObservableProperty]
        private bool _showDiagnosis = true;

        [ObservableProperty]
        private bool _showDrugs = true;

        // Vital Signs Fields
        [ObservableProperty]
        private string _vitalHR = string.Empty;

        [ObservableProperty]
        private string _vitalSBP = string.Empty;

        [ObservableProperty]
        private string _vitalDBP = string.Empty;

        [ObservableProperty]
        private string _vitalRR = string.Empty;

        [ObservableProperty]
        private string _vitalSPO2 = string.Empty;

        [ObservableProperty]
        private string _vitalTemp = string.Empty;

        [ObservableProperty]
        private bool _isVitallyStable;

        // Investigation & Imaging Fields
        [ObservableProperty]
        private string _selectedInvestigation = string.Empty;

        [ObservableProperty]
        private string _selectedImaging = string.Empty;

        [ObservableProperty]
        private DateTime? _returnDate;

        [ObservableProperty]
        private string _investigationAttachmentPath = string.Empty;

        [ObservableProperty]
        private string _imagingAttachmentPath = string.Empty;

        public string InvestigationAttachmentName => string.IsNullOrWhiteSpace(InvestigationAttachmentPath) 
            ? string.Empty 
            : Path.GetFileName(InvestigationAttachmentPath);

        public string ImagingAttachmentName => string.IsNullOrWhiteSpace(ImagingAttachmentPath) 
            ? string.Empty 
            : Path.GetFileName(ImagingAttachmentPath);

        [ObservableProperty]
        private ObservableCollection<ClinicalAttachment> _addedInvestigations = new();

        [ObservableProperty]
        private ObservableCollection<ClinicalAttachment> _addedImagings = new();

        [ObservableProperty]
        private ObservableCollection<string> _investigationsList = new()
        {
            "CBC (Complete Blood Count)",
            "HbA1c (Glycated Hemoglobin)",
            "Lipid Profile (Cholesterol)",
            "Kidney Function Test (KFT)",
            "Liver Function Test (LFT)",
            "Thyroid Profile (TSH)",
            "Urine Analysis",
            "None"
        };

        [ObservableProperty]
        private ObservableCollection<string> _imagingList = new()
        {
            "Echocardiography",
            "Chest X-Ray",
            "Electrocardiogram (ECG)",
            "Abdominal Ultrasound",
            "Cardiac MRI",
            "CT Angiography",
            "None"
        };

        public ClinicalExamViewModel(
            IVisitService visitService, 
            IPatientService patientService, 
            ISharedStateService sharedStateService, 
            IQueueService queueService,
            IPrintService printService,
            IDbContextFactory<Data.AppDbContext> dbContextFactory,
            IThemeService themeService)
        {
            _visitService = visitService;
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            _queueService = queueService;
            _printService = printService;
            _dbContextFactory = dbContextFactory;
            _themeService = themeService;

            System.Windows.WeakEventManager<IThemeService, EventArgs>.AddHandler(_themeService, nameof(IThemeService.ThemeChanged), (s, ev) => OnPropertyChanged(nameof(IsDarkMode)));

            _prescribedDrugs.CollectionChanged += (s, e) => {
                TriggerAutoSave();
                OnPropertyChanged(nameof(HasNoPrescribedDrugs));
                OnPropertyChanged(nameof(HasPrescribedDrugs));
            };
            _addedInvestigations.CollectionChanged += (s, e) => TriggerAutoSave();
            _addedImagings.CollectionChanged += (s, e) => TriggerAutoSave();

            // Load initial patient context and subscribe to selection updates
            CurrentPatient = _sharedStateService.CurrentPatient;
            SelectedPatientLookup = CurrentPatient;
            _sharedStateService.CurrentPatientChanged += OnSharedPatientChanged;

            if (CurrentPatient != null)
            {
                _ = LoadVisitHistoryAsync();
            }

            // Load initial patient list for the search lookup dropdown
            _ = SearchPatientsAsync();

            // Set up 2-second queue polling timer
            _pollingTimer = new System.Windows.Threading.DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromSeconds(2);
            _pollingTimer.Tick += OnPollingTimerTick;
            _pollingTimer.Start();
            if (!string.IsNullOrWhiteSpace(_sharedStateService.ActiveDoctorName))
            {
                _activeDoctorName = _sharedStateService.ActiveDoctorName;
            }

            _ = PollQueueAsync();

            // Load doctors
            _ = LoadDoctorsAsync();

            LoadViewSettings();
        }

        private async void OnPollingTimerTick(object? sender, EventArgs e)
        {
            _pollingTimer.Stop();
            await PollQueueAsync();
            _pollingTimer.Start();
        }

        private async Task PollQueueAsync()
        {
            try
            {
                var activeTask = _queueService.GetActiveQueueAsync();
                var completedTask = _queueService.GetCompletedCountTodayAsync();
                await Task.WhenAll(activeTask, completedTask);

                var active = activeTask.Result.Where(q => q.DoctorName == ActiveDoctorName).ToList();
                ActiveQueue = new ObservableCollection<QueueEntry>(active);
                WaitingPatients = new ObservableCollection<QueueEntry>(active.Where(q => q.Status == "Pending"));
                NotFinishedPatients = new ObservableCollection<QueueEntry>(active.Where(q => q.Status == "InExam"));
                CompletedCountToday = completedTask.Result;
            }
            catch
            {
                // Ignore background transient query errors
            }
        }

        [RelayCommand]
        public void SwitchActiveDoctor(string doctorName)
        {
            if (string.IsNullOrWhiteSpace(doctorName)) return;

            // Find the doctor object
            foreach (var doc in Doctors)
            {
                if (doc.Name == doctorName)
                {
                    SelectedSwitchDoctor = doc;
                    SwitchDoctorPasswordAttempt = string.Empty;
                    SwitchDoctorErrorMessage = string.Empty;
                    ShowSwitchDoctorPasswordModal = true;
                    return;
                }
            }
        }

        [RelayCommand]
        public void ConfirmSwitchDoctorPassword()
        {
            if (SelectedSwitchDoctor == null) return;

            if (SelectedSwitchDoctor.Password == SwitchDoctorPasswordAttempt)
            {
                ActiveDoctorName = SelectedSwitchDoctor.Name;
                _sharedStateService.ActiveDoctorName = SelectedSwitchDoctor.Name;
                ShowSwitchDoctorPasswordModal = false;
                SwitchDoctorPasswordAttempt = string.Empty;
                SwitchDoctorErrorMessage = string.Empty;
                _ = PollQueueAsync();
            }
            else
            {
                SwitchDoctorErrorMessage = "Incorrect Password! | كلمة المرور غير صحيحة!";
            }
        }

        [RelayCommand]
        public void CancelSwitchDoctorPassword()
        {
            ShowSwitchDoctorPasswordModal = false;
            SelectedSwitchDoctor = null;
            SwitchDoctorPasswordAttempt = string.Empty;
            SwitchDoctorErrorMessage = string.Empty;
        }

        public async Task LoadDoctorsAsync()
        {
            try
            {
                var docList = await _patientService.GetAllDoctorsAsync();
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Doctors.Clear();
                    foreach (var doc in docList)
                    {
                        Doctors.Add(doc);
                    }
                });
            }
            catch
            {
                // Ignore transient db errors
            }
        }

        partial void OnSelectedPatientLookupChanged(Patient? value)
        {
            if (CurrentPatient != value)
            {
                CurrentPatient = value;
                _sharedStateService.CurrentPatient = value; // Keep shared state synchronized
                if (value != null)
                {
                    _ = LoadVisitHistoryAsync();
                }
                else
                {
                    VisitHistory.Clear();
                }
            }
        }

        partial void OnCurrentPatientChanged(Patient? value)
        {
            if (value != null)
            {
                _ = LoadDraftForPatientAsync(value.PatientId);
            }
            else
            {
                ClearFormFieldsWithoutAutoSave();
            }
            OnPropertyChanged(nameof(ShowObstetricsCard));
        }

        async partial void OnSearchTermChanged(string value)
        {
            await SearchPatientsAsync();
        }

        [RelayCommand]
        public async Task SearchPatientsAsync()
        {
            try
            {
                var results = await _patientService.SearchPatientsAsync(SearchTerm);
                Patients = new ObservableCollection<Patient>(results);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching patients: {ex.Message}";
            }
        }

        private void OnSharedPatientChanged(Patient? patient)
        {
            if (SelectedPatientLookup != patient)
            {
                SelectedPatientLookup = patient;
            }
        }

        [RelayCommand]
        public async Task LoadVisitHistoryAsync()
        {
            if (CurrentPatient == null) return;

            try
            {
                var visits = await _visitService.GetVisitsByPatientIdAsync(CurrentPatient.PatientId);
                VisitHistory = new ObservableCollection<Visit>(visits);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading visit history: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SaveVisitAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No patient selected. Please select a patient first.";
                return;
            }

            SyncPhysicalExamination();
            SyncTreatmentPlan();

            if (string.IsNullOrWhiteSpace(ChiefComplaint) && string.IsNullOrWhiteSpace(Diagnosis))
            {
                StatusMessage = "Chief Complaint or Diagnosis is required to log a visit.";
                return;
            }

            try
            {
                var rxText = string.Join(Environment.NewLine, PrescribedDrugs.Select(d => d.ToString()));

                using (var db = await _dbContextFactory.CreateDbContextAsync())
                {
                    var today = DateTime.UtcNow.Date;
                    var existingVisit = await db.Visits
                        .FirstOrDefaultAsync(v => v.PatientId == CurrentPatient.PatientId && v.VisitDate >= today);

                    if (existingVisit != null)
                    {
                        existingVisit.ChiefComplaint = ChiefComplaint;
                        existingVisit.HistoryOfPresentIllness = HistoryOfPresentIllness;
                        existingVisit.PhysicalExamination = PhysicalExamination;
                        existingVisit.Diagnosis = Diagnosis;
                        existingVisit.TreatmentPlan = TreatmentPlan;
                        existingVisit.Prescription = rxText;
                        existingVisit.VitalsHR = VitalHR;
                        existingVisit.VitalsSBP = VitalSBP;
                        existingVisit.VitalsDBP = VitalDBP;
                        existingVisit.VitalsRR = VitalRR;
                        existingVisit.VitalsSPO2 = VitalSPO2;
                        existingVisit.VitalsTemp = VitalTemp;
                        existingVisit.Investigation = string.Join(", ", AddedInvestigations.Select(x => x.Name));
                        existingVisit.Imaging = string.Join(", ", AddedImagings.Select(x => x.Name));
                        existingVisit.ReturnDate = ReturnDate;
                        existingVisit.InvestigationAttachmentPath = string.Join(";", AddedInvestigations.Select(x => x.AttachmentPath));
                        existingVisit.ImagingAttachmentPath = string.Join(";", AddedImagings.Select(x => x.AttachmentPath));

                        db.Visits.Update(existingVisit);
                    }
                    else
                    {
                        var visit = new Visit
                        {
                            PatientId = CurrentPatient.PatientId,
                            ChiefComplaint = ChiefComplaint,
                            HistoryOfPresentIllness = HistoryOfPresentIllness,
                            PhysicalExamination = PhysicalExamination,
                            Diagnosis = Diagnosis,
                            TreatmentPlan = TreatmentPlan,
                            Prescription = rxText,
                            VitalsHR = VitalHR,
                            VitalsSBP = VitalSBP,
                            VitalsDBP = VitalDBP,
                            VitalsRR = VitalRR,
                            VitalsSPO2 = VitalSPO2,
                            VitalsTemp = VitalTemp,
                            Investigation = string.Join(", ", AddedInvestigations.Select(x => x.Name)),
                            Imaging = string.Join(", ", AddedImagings.Select(x => x.Name)),
                            ReturnDate = ReturnDate,
                            InvestigationAttachmentPath = string.Join(";", AddedInvestigations.Select(x => x.AttachmentPath)),
                            ImagingAttachmentPath = string.Join(";", AddedImagings.Select(x => x.AttachmentPath)),
                            VisitDate = DateTime.UtcNow
                        };
                        await db.Visits.AddAsync(visit);
                    }
                    await db.SaveChangesAsync();
                }

                // Save new drugs to drug dictionary for autocomplete
                using (var db = await _dbContextFactory.CreateDbContextAsync())
                {
                    foreach (var drug in PrescribedDrugs)
                    {
                        var exists = await db.Drugs.AnyAsync(d => d.Name == drug.Name);
                        if (!exists)
                        {
                            db.Drugs.Add(new Drug { Name = drug.Name });
                        }
                    }
                    await db.SaveChangesAsync();
                }

                // Delete local draft
                await DeleteDraftForPatientAsync(CurrentPatient.PatientId);

                StatusMessage = "Visit log saved successfully!";

                // Clear Form Fields
                ClearFormFieldsWithoutAutoSave();

                await LoadVisitHistoryAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving visit: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task StartSessionAsync(QueueEntry entry)
        {
            if (entry == null) return;
            try
            {
                // Update queue status to InExam
                await _queueService.UpdateQueueStatusAsync(entry.PatientId, "InExam");

                // Get patient from DB
                var patient = await _patientService.GetPatientByIdAsync(entry.PatientId);
                SelectedPatientLookup = patient;
                StatusMessage = $"Exam session started for {entry.PatientName}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting exam session: {ex.Message}";
            }
        }

        public async Task MoveToQueueStatusAsync(QueueEntry entry, string newStatus)
        {
            if (entry == null || string.IsNullOrEmpty(newStatus)) return;
            try
            {
                await _queueService.UpdateQueueStatusAsync(entry.PatientId, newStatus);
                StatusMessage = $"Patient {entry.PatientName} status changed to {newStatus}.";
                await PollQueueAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error moving patient: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task CompleteSessionDoneAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No active patient to complete.";
                return;
            }

            try
            {
                // Save visit first if complaint/diagnosis is populated
                if (!string.IsNullOrWhiteSpace(ChiefComplaint) || !string.IsNullOrWhiteSpace(Diagnosis))
                {
                    await SaveVisitAsync();
                }

                // Set queue entry as Completed
                await _queueService.CompleteQueueEntryAsync(CurrentPatient.PatientId);
                StatusMessage = $"Exam session for '{CurrentPatient.Name}' completed and removed from queue.";

                // Clear current session
                SelectedPatientLookup = null;
                CurrentPatient = null;
                VisitHistory.Clear();

                ChiefComplaint = string.Empty;
                HistoryOfPresentIllness = string.Empty;
                PhysicalExamination = string.Empty;
                Diagnosis = string.Empty;
                TreatmentPlan = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error completing exam session: {ex.Message}";
            }
        }

        [RelayCommand]
        public void ExitToDashboard()
        {
            SelectedPatientLookup = null;
            CurrentPatient = null;
            VisitHistory.Clear();
            ClearFormFieldsWithoutAutoSave();
        }

        [RelayCommand]
        public async Task HoldSessionAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No active patient to hold.";
                return;
            }

            try
            {
                // Save current visit details first if any complaint/diagnosis is entered
                if (!string.IsNullOrWhiteSpace(ChiefComplaint) || !string.IsNullOrWhiteSpace(Diagnosis))
                {
                    await SaveVisitAsync();
                }

                // Explicitly update queue status to InExam (which means Incomplete / Not Finished)
                await _queueService.UpdateQueueStatusAsync(CurrentPatient.PatientId, "InExam");
                StatusMessage = $"Exam session for '{CurrentPatient.Name}' put on hold (Not Finished).";

                // Clear current session
                SelectedPatientLookup = null;
                CurrentPatient = null;
                VisitHistory.Clear();

                ChiefComplaint = string.Empty;
                HistoryOfPresentIllness = string.Empty;
                PhysicalExamination = string.Empty;
                Diagnosis = string.Empty;
                TreatmentPlan = string.Empty;
                VitalHR = string.Empty;
                VitalSBP = string.Empty;
                VitalDBP = string.Empty;
                VitalRR = string.Empty;
                VitalSPO2 = string.Empty;
                VitalTemp = string.Empty;
                SelectedInvestigation = string.Empty;
                SelectedImaging = string.Empty;
                ReturnDate = null;
                PrescribedDrugs.Clear();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error holding exam session: {ex.Message}";
            }
        }

        async partial void OnDrugSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
            {
                DrugSuggestions.Clear();
                IsDrugSuggestionsOpen = false;
                return;
            }

            try
            {
                using var db = await _dbContextFactory.CreateDbContextAsync();
                var matches = await db.Drugs
                    .Where(d => d.Name.StartsWith(value))
                    .Select(d => d.Name)
                    .Take(8)
                    .ToListAsync();

                DrugSuggestions.Clear();
                foreach (var match in matches)
                {
                    DrugSuggestions.Add(match);
                }
                IsDrugSuggestionsOpen = DrugSuggestions.Count > 0;
            }
            catch
            {
                // Ignore search DB errors silently
            }
        }

        [RelayCommand]
        public void AddDrug()
        {
            if (!string.IsNullOrWhiteSpace(DrugSearchText))
            {
                string drugName = DrugSearchText.Trim();
                if (!PrescribedDrugs.Any(d => d.Name.Equals(drugName, StringComparison.OrdinalIgnoreCase)))
                {
                    var med = new PrescribedMedication { Name = drugName };
                    PrescribedDrugs.Add(med);
                }
                DrugSearchText = string.Empty;
                IsDrugSuggestionsOpen = false;
            }
        }

        [RelayCommand]
        public void SelectSuggestedDrug(string drugName)
        {
            if (!string.IsNullOrEmpty(drugName))
            {
                DrugSearchText = drugName;
                AddDrug();
            }
        }

        [RelayCommand]
        public void RemoveDrug(PrescribedMedication drug)
        {
            if (drug != null && PrescribedDrugs.Contains(drug))
            {
                PrescribedDrugs.Remove(drug);
            }
        }

        [RelayCommand]
        public void PrintActiveRx()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No patient selected.";
                return;
            }
            if (PrescribedDrugs.Count == 0 && string.IsNullOrWhiteSpace(DrugSearchText))
            {
                StatusMessage = "No prescription added yet.";
                return;
            }

            string rxText = string.Join(Environment.NewLine, PrescribedDrugs.Select(d => d.ToString()));
            if (string.IsNullOrWhiteSpace(rxText))
            {
                rxText = DrugSearchText.Trim();
            }

            _printService.PrintPrescription(CurrentPatient, rxText);
        }

        [RelayCommand]
        public void PrintVisitRx(Visit visit)
        {
            if (CurrentPatient == null || visit == null) return;
            if (string.IsNullOrWhiteSpace(visit.Prescription))
            {
                MessageBox.Show("No prescription was recorded for this visit.", "No Prescription", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _printService.PrintPrescription(CurrentPatient, visit.Prescription);
        }

        [RelayCommand]
        public void PrintVisitReport(Visit visit)
        {
            if (CurrentPatient == null || visit == null) return;
            _printService.PrintReport(CurrentPatient, visit);
        }

        [RelayCommand]
        public void PrintActiveVisitReport()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No patient selected.";
                return;
            }

            var mockVisit = new Visit
            {
                VisitDate = DateTime.UtcNow,
                ChiefComplaint = ChiefComplaint,
                HistoryOfPresentIllness = HistoryOfPresentIllness,
                PhysicalExamination = PhysicalExamination,
                Diagnosis = Diagnosis,
                TreatmentPlan = TreatmentPlan,
                Prescription = string.Join(Environment.NewLine, PrescribedDrugs)
            };

            _printService.PrintReport(CurrentPatient, mockVisit);
        }

        partial void OnChiefComplaintChanged(string value) => TriggerAutoSave();
        partial void OnHistoryOfPresentIllnessChanged(string value) => TriggerAutoSave();
        partial void OnPhysicalExaminationChanged(string value) => TriggerAutoSave();
        partial void OnDiagnosisChanged(string value) => TriggerAutoSave();
        partial void OnTreatmentPlanChanged(string value) => TriggerAutoSave();

        private void TriggerAutoSave()
        {
            if (_isSavingOrLoadingDraft || CurrentPatient == null) return;
            _ = AutoSaveDraftAsync();
        }

        private async Task AutoSaveDraftAsync()
        {
            if (CurrentPatient == null) return;
            
            _isSavingOrLoadingDraft = true;
            try
            {
                var drafts = new Dictionary<int, PatientVisitDraft>();
                if (File.Exists(DraftsFile))
                {
                    try
                    {
                        string json = await File.ReadAllTextAsync(DraftsFile);
                        drafts = JsonSerializer.Deserialize<Dictionary<int, PatientVisitDraft>>(json) ?? drafts;
                    }
                    catch
                    {
                        // File corrupted or empty, start fresh
                    }
                }

                SyncPhysicalExamination();
                SyncTreatmentPlan();

                drafts[CurrentPatient.PatientId] = new PatientVisitDraft
                {
                    PatientId = CurrentPatient.PatientId,
                    ChiefComplaint = ChiefComplaint ?? string.Empty,
                    HistoryOfPresentIllness = HistoryOfPresentIllness ?? string.Empty,
                    PhysicalExamination = PhysicalExamination ?? string.Empty,
                    Diagnosis = Diagnosis ?? string.Empty,
                    TreatmentPlan = TreatmentPlan ?? string.Empty,
                    PrescribedDrugs = new System.Collections.Generic.List<PrescribedMedication>(PrescribedDrugs),
                    VitalsHR = VitalHR ?? string.Empty,
                    VitalsSBP = VitalSBP ?? string.Empty,
                    VitalsDBP = VitalDBP ?? string.Empty,
                    VitalsRR = VitalRR ?? string.Empty,
                    VitalsSPO2 = VitalSPO2 ?? string.Empty,
                    VitalsTemp = VitalTemp ?? string.Empty,
                    IsVitallyStable = IsVitallyStable,
                    Investigation = SelectedInvestigation ?? string.Empty,
                    Imaging = SelectedImaging ?? string.Empty,
                    Investigations = new List<ClinicalAttachment>(AddedInvestigations),
                    Imagings = new List<ClinicalAttachment>(AddedImagings),
                    ReturnDate = ReturnDate,
                    InvestigationAttachmentPath = InvestigationAttachmentPath ?? string.Empty,
                    ImagingAttachmentPath = ImagingAttachmentPath ?? string.Empty
                };

                string outputJson = JsonSerializer.Serialize(drafts, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(DraftsFile, outputJson);
            }
            catch
            {
                // Silently ignore disk IO errors
            }
            finally
            {
                _isSavingOrLoadingDraft = false;
            }
        }

        private async Task LoadDraftForPatientAsync(int patientId)
        {
            _isSavingOrLoadingDraft = true;
            try
            {
                if (File.Exists(DraftsFile))
                {
                    string json = await File.ReadAllTextAsync(DraftsFile);
                    var drafts = JsonSerializer.Deserialize<Dictionary<int, PatientVisitDraft>>(json);
                    if (drafts != null && drafts.TryGetValue(patientId, out var draft))
                    {
                        ChiefComplaint = draft.ChiefComplaint;
                        HistoryOfPresentIllness = draft.HistoryOfPresentIllness;
                        PhysicalExamination = draft.PhysicalExamination;
                        Diagnosis = draft.Diagnosis;
                        TreatmentPlan = draft.TreatmentPlan;
                        ParsePhysicalExamination(PhysicalExamination ?? string.Empty);
                        ParseTreatmentPlan(TreatmentPlan ?? string.Empty);
                        VitalHR = draft.VitalsHR;
                        VitalSBP = draft.VitalsSBP;
                        VitalDBP = draft.VitalsDBP;
                        VitalRR = draft.VitalsRR;
                        VitalSPO2 = draft.VitalsSPO2;
                        VitalTemp = draft.VitalsTemp;
                        IsVitallyStable = draft.IsVitallyStable;
                        SelectedInvestigation = draft.Investigation;
                        SelectedImaging = draft.Imaging;
                        ReturnDate = draft.ReturnDate;
                        InvestigationAttachmentPath = draft.InvestigationAttachmentPath;
                        ImagingAttachmentPath = draft.ImagingAttachmentPath;

                        // Legacy draft parsing
                        if ((draft.Investigations == null || draft.Investigations.Count == 0) && !string.IsNullOrEmpty(draft.Investigation))
                        {
                            var names = draft.Investigation.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                            var paths = (draft.InvestigationAttachmentPath ?? string.Empty).Split(new[] { ";" }, StringSplitOptions.None);
                            draft.Investigations = new List<ClinicalAttachment>();
                            for (int i = 0; i < names.Length; i++)
                            {
                                draft.Investigations.Add(new ClinicalAttachment 
                                { 
                                    Name = names[i], 
                                    AttachmentPath = i < paths.Length ? paths[i] : string.Empty 
                                });
                            }
                        }

                        if ((draft.Imagings == null || draft.Imagings.Count == 0) && !string.IsNullOrEmpty(draft.Imaging))
                        {
                            var names = draft.Imaging.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                            var paths = (draft.ImagingAttachmentPath ?? string.Empty).Split(new[] { ";" }, StringSplitOptions.None);
                            draft.Imagings = new List<ClinicalAttachment>();
                            for (int i = 0; i < names.Length; i++)
                            {
                                draft.Imagings.Add(new ClinicalAttachment 
                                { 
                                    Name = names[i], 
                                    AttachmentPath = i < paths.Length ? paths[i] : string.Empty 
                                });
                            }
                        }

                        var newInvestigations = new ObservableCollection<ClinicalAttachment>(draft.Investigations ?? new List<ClinicalAttachment>());
                        newInvestigations.CollectionChanged += (s, e) => TriggerAutoSave();
                        AddedInvestigations = newInvestigations;

                        var newImagings = new ObservableCollection<ClinicalAttachment>(draft.Imagings ?? new List<ClinicalAttachment>());
                        newImagings.CollectionChanged += (s, e) => TriggerAutoSave();
                        AddedImagings = newImagings;
                        
                        var newCollection = new ObservableCollection<PrescribedMedication>(draft.PrescribedDrugs);
                        newCollection.CollectionChanged += (s, e) => TriggerAutoSave();
                        PrescribedDrugs = newCollection;
                        return;
                    }
                }
                
                // Clear fields if no draft exists
                ClearFormFieldsWithoutAutoSave();
            }
            catch
            {
                // Ignore load errors and fallback to clean form
                ClearFormFieldsWithoutAutoSave();
            }
            finally
            {
                _isSavingOrLoadingDraft = false;
            }
        }

        private async Task DeleteDraftForPatientAsync(int patientId)
        {
            try
            {
                if (File.Exists(DraftsFile))
                {
                    string json = await File.ReadAllTextAsync(DraftsFile);
                    var drafts = JsonSerializer.Deserialize<Dictionary<int, PatientVisitDraft>>(json);
                    if (drafts != null && drafts.Remove(patientId))
                    {
                        string outputJson = JsonSerializer.Serialize(drafts, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(DraftsFile, outputJson);
                    }
                }
            }
            catch
            {
                // Ignore delete errors silently
            }
        }

        private void ClearFormFieldsWithoutAutoSave()
        {
            ChiefComplaint = string.Empty;
            HistoryOfPresentIllness = string.Empty;
            PhysicalExamination = string.Empty;
            Diagnosis = string.Empty;
            TreatmentPlan = string.Empty;
            VitalHR = string.Empty;
            VitalSBP = string.Empty;
            VitalDBP = string.Empty;
            VitalRR = string.Empty;
            VitalSPO2 = string.Empty;
            VitalTemp = string.Empty;
            IsVitallyStable = false;
            SelectedInvestigation = string.Empty;
            SelectedImaging = string.Empty;
            ReturnDate = null;
            InvestigationAttachmentPath = string.Empty;
            ImagingAttachmentPath = string.Empty;
            
            var newCollection = new ObservableCollection<PrescribedMedication>();
            newCollection.CollectionChanged += (s, e) => TriggerAutoSave();
            PrescribedDrugs = newCollection;

            var newInvestigations = new ObservableCollection<ClinicalAttachment>();
            newInvestigations.CollectionChanged += (s, e) => TriggerAutoSave();
            AddedInvestigations = newInvestigations;

            var newImagings = new ObservableCollection<ClinicalAttachment>();
            newImagings.CollectionChanged += (s, e) => TriggerAutoSave();
            AddedImagings = newImagings;
        }

        [RelayCommand]
        public void SimulateVoiceInput(string fieldName)
        {
            string simulatedText = fieldName switch
            {
                "Complaint" => "Patient complains of chest tightness and shortness of breath.",
                "HPI" => "Symptoms started 3 days ago, worsening with physical activity.",
                "Exam" => "Blood pressure 135/85 mmHg, pulse 80 bpm, lungs clear to auscultation.",
                "Diagnosis" => "Mild essential hypertension.",
                "Plan" => "Instructed patient on low-sodium diet and scheduled follow-up in two weeks.",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(simulatedText)) return;

            switch (fieldName)
            {
                case "Complaint":
                    ChiefComplaint = string.IsNullOrEmpty(ChiefComplaint) ? simulatedText : $"{ChiefComplaint} {simulatedText}";
                    break;
                case "HPI":
                    HistoryOfPresentIllness = string.IsNullOrEmpty(HistoryOfPresentIllness) ? simulatedText : $"{HistoryOfPresentIllness} {simulatedText}";
                    break;
                case "Exam":
                    PhysicalExamination = string.IsNullOrEmpty(PhysicalExamination) ? simulatedText : $"{PhysicalExamination} {simulatedText}";
                    break;
                case "Diagnosis":
                    Diagnosis = string.IsNullOrEmpty(Diagnosis) ? simulatedText : $"{Diagnosis} {simulatedText}";
                    break;
                case "Plan":
                    TreatmentPlan = string.IsNullOrEmpty(TreatmentPlan) ? simulatedText : $"{TreatmentPlan} {simulatedText}";
                    break;
            }

            StatusMessage = $"Captured voice input for {fieldName}.";
        }

        // View settings load and save
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "doctor_view_settings.json");

        private void LoadViewSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
                    if (settings != null)
                    {
                        bool val;
                        if (settings.TryGetValue("ShowComplaint", out val)) ShowComplaint = val;
                        if (settings.TryGetValue("ShowPhysicalExam", out val)) ShowPhysicalExam = val;
                        if (settings.TryGetValue("ShowVitals", out val)) ShowVitals = val;
                        if (settings.TryGetValue("ShowInvestigation", out val)) ShowInvestigation = val;
                        if (settings.TryGetValue("ShowDiagnosis", out val)) ShowDiagnosis = val;
                        if (settings.TryGetValue("ShowDrugs", out val)) ShowDrugs = val;
                    }
                }
            }
            catch
            {
                // Fallback to default true values
            }
        }

        private void SaveViewSettings()
        {
            try
            {
                var settings = new Dictionary<string, bool>
                {
                    { "ShowComplaint", ShowComplaint },
                    { "ShowPhysicalExam", ShowPhysicalExam },
                    { "ShowVitals", ShowVitals },
                    { "ShowInvestigation", ShowInvestigation },
                    { "ShowDiagnosis", ShowDiagnosis },
                    { "ShowDrugs", ShowDrugs }
                };
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
            }
            catch
            {
                // Silently ignore settings save errors
            }
        }

        // Changed handlers for auto-saving view/draft fields
        partial void OnShowComplaintChanged(bool value) => SaveViewSettings();
        partial void OnShowPhysicalExamChanged(bool value) => SaveViewSettings();
        partial void OnShowVitalsChanged(bool value) => SaveViewSettings();
        partial void OnShowInvestigationChanged(bool value) => SaveViewSettings();
        partial void OnShowDiagnosisChanged(bool value) => SaveViewSettings();
        partial void OnShowDrugsChanged(bool value) => SaveViewSettings();

        partial void OnVitalHRChanged(string value) => TriggerAutoSave();
        partial void OnVitalSBPChanged(string value) => TriggerAutoSave();
        partial void OnVitalDBPChanged(string value) => TriggerAutoSave();
        partial void OnVitalRRChanged(string value) => TriggerAutoSave();
        partial void OnVitalSPO2Changed(string value) => TriggerAutoSave();
        partial void OnVitalTempChanged(string value) => TriggerAutoSave();
        partial void OnSelectedInvestigationChanged(string value) => TriggerAutoSave();
        partial void OnSelectedImagingChanged(string value) => TriggerAutoSave();
        partial void OnReturnDateChanged(DateTime? value) => TriggerAutoSave();

        partial void OnIsVitallyStableChanged(bool value)
        {
            if (value)
            {
                VitalHR = "75";
                VitalSBP = "120";
                VitalDBP = "80";
                VitalRR = "16";
                VitalSPO2 = "98";
                VitalTemp = "37.0";
            }
            else
            {
                VitalHR = string.Empty;
                VitalSBP = string.Empty;
                VitalDBP = string.Empty;
                VitalRR = string.Empty;
                VitalSPO2 = string.Empty;
                VitalTemp = string.Empty;
            }
            TriggerAutoSave();
        }

        [RelayCommand]
        public void UploadInvestigationAttachment()
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
                    var extension = Path.GetExtension(sourcePath);
                    var destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VisitAttachments");
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    var uniqueName = $"investigation_{Guid.NewGuid()}{extension}";
                    var destPath = Path.Combine(destDir, uniqueName);
                    
                    File.Copy(sourcePath, destPath, overwrite: true);
                    InvestigationAttachmentPath = destPath;
                    StatusMessage = "Investigation attachment uploaded successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to upload file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void UploadImagingAttachment()
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
                    var extension = Path.GetExtension(sourcePath);
                    var destDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VisitAttachments");
                    if (!Directory.Exists(destDir))
                    {
                        Directory.CreateDirectory(destDir);
                    }

                    var uniqueName = $"imaging_{Guid.NewGuid()}{extension}";
                    var destPath = Path.Combine(destDir, uniqueName);
                    
                    File.Copy(sourcePath, destPath, overwrite: true);
                    ImagingAttachmentPath = destPath;
                    StatusMessage = "Imaging attachment uploaded successfully!";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Failed to upload file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemoveInvestigationAttachment()
        {
            try
            {
                if (File.Exists(InvestigationAttachmentPath))
                {
                    File.Delete(InvestigationAttachmentPath);
                }
            }
            catch {}
            InvestigationAttachmentPath = string.Empty;
            TriggerAutoSave();
        }

        [RelayCommand]
        public void RemoveImagingAttachment()
        {
            try
            {
                if (File.Exists(ImagingAttachmentPath))
                {
                    File.Delete(ImagingAttachmentPath);
                }
            }
            catch {}
            ImagingAttachmentPath = string.Empty;
            TriggerAutoSave();
        }

        [RelayCommand]
        public void OpenAttachment(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                StatusMessage = "Attachment file not found.";
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
                StatusMessage = $"Could not open attachment: {ex.Message}";
            }
        }

        [RelayCommand]
        public void AddInvestigation()
        {
            if (!string.IsNullOrWhiteSpace(SelectedInvestigation) && SelectedInvestigation != "None")
            {
                string name = SelectedInvestigation.Trim();
                if (!AddedInvestigations.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AddedInvestigations.Add(new ClinicalAttachment { Name = name });
                }
                SelectedInvestigation = string.Empty;
            }
        }

        [RelayCommand]
        public void AddImaging()
        {
            if (!string.IsNullOrWhiteSpace(SelectedImaging) && SelectedImaging != "None")
            {
                string name = SelectedImaging.Trim();
                if (!AddedImagings.Any(i => i.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    AddedImagings.Add(new ClinicalAttachment { Name = name });
                }
                SelectedImaging = string.Empty;
            }
        }

        [RelayCommand]
        public void RemoveInvestigation(ClinicalAttachment item)
        {
            if (item != null && AddedInvestigations.Contains(item))
            {
                if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
                {
                    try { File.Delete(item.AttachmentPath); } catch {}
                }
                AddedInvestigations.Remove(item);
            }
        }

        [RelayCommand]
        public void RemoveImaging(ClinicalAttachment item)
        {
            if (item != null && AddedImagings.Contains(item))
            {
                if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
                {
                    try { File.Delete(item.AttachmentPath); } catch {}
                }
                AddedImagings.Remove(item);
            }
        }

        [RelayCommand]
        public void UploadInvestigationFile(ClinicalAttachment item)
        {
            if (item == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|PDF Files (*.pdf)|*.pdf|Excel Files (*.xls;*.xlsx)|*.xls;*.xlsx"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sourcePath = openFileDialog.FileName;
                    var extension = Path.GetExtension(sourcePath);
                    var uploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var uniqueName = $"investigation_{Guid.NewGuid()}{extension}";
                    var destPath = Path.Combine(uploadsDir, uniqueName);
                    File.Copy(sourcePath, destPath, true);

                    // Remove old file if it exists
                    if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
                    {
                        try { File.Delete(item.AttachmentPath); } catch {}
                    }

                    item.AttachmentPath = destPath;
                    StatusMessage = "Investigation attachment uploaded successfully!";
                    TriggerAutoSave();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error uploading file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemoveInvestigationFile(ClinicalAttachment item)
        {
            if (item == null) return;

            if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
            {
                try { File.Delete(item.AttachmentPath); } catch {}
            }
            item.AttachmentPath = string.Empty;
            StatusMessage = "Investigation attachment removed.";
            TriggerAutoSave();
        }

        [RelayCommand]
        public void UploadImagingFile(ClinicalAttachment item)
        {
            if (item == null) return;

            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "All Files (*.*)|*.*|Image Files (*.png;*.jpg;*.jpeg;*.gif)|*.png;*.jpg;*.jpeg;*.gif|PDF Files (*.pdf)|*.pdf"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var sourcePath = openFileDialog.FileName;
                    var extension = Path.GetExtension(sourcePath);
                    var uploadsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Uploads");
                    if (!Directory.Exists(uploadsDir))
                    {
                        Directory.CreateDirectory(uploadsDir);
                    }

                    var uniqueName = $"imaging_{Guid.NewGuid()}{extension}";
                    var destPath = Path.Combine(uploadsDir, uniqueName);
                    File.Copy(sourcePath, destPath, true);

                    // Remove old file if it exists
                    if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
                    {
                        try { File.Delete(item.AttachmentPath); } catch {}
                    }

                    item.AttachmentPath = destPath;
                    StatusMessage = "Imaging attachment uploaded successfully!";
                    TriggerAutoSave();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error uploading file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemoveImagingFile(ClinicalAttachment item)
        {
            if (item == null) return;

            if (!string.IsNullOrEmpty(item.AttachmentPath) && File.Exists(item.AttachmentPath))
            {
                try { File.Delete(item.AttachmentPath); } catch {}
            }
            item.AttachmentPath = string.Empty;
            StatusMessage = "Imaging attachment removed.";
            TriggerAutoSave();
        }

        [RelayCommand]
        public void OpenAddPatientModal()
        {
            NewPatientName = string.Empty;
            NewPatientAge = 0;
            NewPatientAgeMonths = 0;
            NewPatientGender = "ذكر";
            NewPatientPhone = string.Empty;
            NewPatientBirthDate = null;
            NewPatientGovernorate = "بغداد";
            NewPatientFiles = string.Empty;
            
            NewPatientIsPaidVisit = true;
            NewPatientIsFreeVisit = false;
            NewPatientVisitPrice = "25000";
            
            AddPatientValidationMessage = string.Empty;
            ShowAddPatientModal = true;
        }

        [RelayCommand]
        public void CancelAddPatient()
        {
            ShowAddPatientModal = false;
        }

        [RelayCommand]
        public void UploadNewPatientFile()
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
                    NewPatientFiles = destPath;
                }
                catch (Exception ex)
                {
                    AddPatientValidationMessage = $"Failed to upload file: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public void RemoveNewPatientFile()
        {
            NewPatientFiles = string.Empty;
        }

        [RelayCommand]
        public void OpenNewPatientFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(processInfo);
            }
            catch (Exception ex)
            {
                AddPatientValidationMessage = $"Failed to open file: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task SaveAddPatientAsync()
        {
            if (string.IsNullOrWhiteSpace(NewPatientName))
            {
                AddPatientValidationMessage = "Patient Name is required! | اسم المريض مطلوب!";
                return;
            }

            try
            {
                var patient = new Patient
                {
                    Name = NewPatientName.Trim(),
                    Age = NewPatientAge,
                    AgeMonths = NewPatientAgeMonths,
                    Gender = NewPatientGender,
                    Phone = NewPatientPhone?.Trim() ?? string.Empty,
                    BirthDate = NewPatientBirthDate,
                    Governorate = NewPatientGovernorate,
                    PatientFiles = NewPatientFiles ?? string.Empty,
                    CreatedAt = DateTime.UtcNow
                };

                await _patientService.AddPatientAsync(patient);

                decimal price = 0;
                if (NewPatientIsPaidVisit)
                {
                    decimal.TryParse(NewPatientVisitPrice, out price);
                }

                var visit = new Visit
                {
                    PatientId = patient.PatientId,
                    VisitDate = DateTime.UtcNow,
                    IsPaid = NewPatientIsPaidVisit,
                    VisitPrice = price,
                    DoctorName = ActiveDoctorName
                };

                await _patientService.AddVisitForCheckInAsync(visit);

                // Add to daily queue
                await _queueService.AddToQueueAsync(patient.PatientId, patient.Name, ActiveDoctorName);

                ShowAddPatientModal = false;

                // Sync current selection
                CurrentPatient = patient;
                SelectedPatientLookup = patient;

                _ = PollQueueAsync();
            }
            catch (Exception ex)
            {
                AddPatientValidationMessage = $"Error: {ex.Message}";
            }
        }

        public void StartPolling()
        {
            _pollingTimer?.Start();
        }

        public void StopPolling()
        {
            _pollingTimer?.Stop();
        }

        public void Dispose()
        {
            StopPolling();
            _sharedStateService.CurrentPatientChanged -= OnSharedPatientChanged;
            GC.SuppressFinalize(this);
        }
    }
}
