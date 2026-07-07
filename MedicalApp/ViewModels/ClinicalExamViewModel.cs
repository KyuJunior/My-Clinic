using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicalApp.Models;
using MedicalApp.Services;

namespace MedicalApp.ViewModels
{
    public partial class ClinicalExamViewModel : ObservableObject, IDisposable
    {
        private readonly IVisitService _visitService;
        private readonly IPatientService _patientService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IQueueService _queueService;
        private readonly System.Windows.Threading.DispatcherTimer _pollingTimer;

        [ObservableProperty]
        private Patient? _currentPatient;

        [ObservableProperty]
        private ObservableCollection<Visit> _visitHistory = new();

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _activeQueue = new();

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
        private string _statusMessage = string.Empty;

        public ClinicalExamViewModel(IVisitService visitService, IPatientService patientService, ISharedStateService sharedStateService, IQueueService queueService)
        {
            _visitService = visitService;
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            _queueService = queueService;

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
            _ = PollQueueAsync();
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
                var active = await _queueService.GetActiveQueueAsync();
                ActiveQueue = new ObservableCollection<QueueEntry>(active);
            }
            catch
            {
                // Ignore background transient query errors
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

            if (string.IsNullOrWhiteSpace(ChiefComplaint) && string.IsNullOrWhiteSpace(Diagnosis))
            {
                StatusMessage = "Chief Complaint or Diagnosis is required to log a visit.";
                return;
            }

            try
            {
                var visit = new Visit
                {
                    PatientId = CurrentPatient.PatientId,
                    ChiefComplaint = ChiefComplaint,
                    HistoryOfPresentIllness = HistoryOfPresentIllness,
                    PhysicalExamination = PhysicalExamination,
                    Diagnosis = Diagnosis,
                    TreatmentPlan = TreatmentPlan,
                    VisitDate = DateTime.UtcNow
                };

                await _visitService.AddVisitAsync(visit);
                StatusMessage = "Visit log saved successfully!";

                // Clear Form Fields
                ChiefComplaint = string.Empty;
                HistoryOfPresentIllness = string.Empty;
                PhysicalExamination = string.Empty;
                Diagnosis = string.Empty;
                TreatmentPlan = string.Empty;

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
        public async Task CompleteSessionToEchoAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No active patient to complete.";
                return;
            }

            try
            {
                // Save visit first
                if (!string.IsNullOrWhiteSpace(ChiefComplaint) || !string.IsNullOrWhiteSpace(Diagnosis))
                {
                    await SaveVisitAsync();
                }

                // Return queue status to Pending (re-queues them for the Echo room)
                await _queueService.UpdateQueueStatusAsync(CurrentPatient.PatientId, "Pending");
                StatusMessage = $"Exam session saved. '{CurrentPatient.Name}' sent to Echo waitlist.";

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
                StatusMessage = $"Error sending patient to Echo: {ex.Message}";
            }
        }

        public void Dispose()
        {
            _pollingTimer.Stop();
            _sharedStateService.CurrentPatientChanged -= OnSharedPatientChanged;
            GC.SuppressFinalize(this);
        }
    }
}
