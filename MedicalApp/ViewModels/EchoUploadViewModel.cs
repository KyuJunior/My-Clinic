using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Extensions.Configuration;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MedicalApp.Models;
using MedicalApp.Services;

namespace MedicalApp.ViewModels
{
    public partial class EchoUploadViewModel : ObservableObject, IDisposable
    {
        private readonly IEchoService _echoService;
        private readonly IPatientService _patientService;
        private readonly ISharedStateService _sharedStateService;
        private readonly IConfiguration _configuration;
        private readonly IQueueService _queueService;
        private readonly System.Windows.Threading.DispatcherTimer _pollingTimer;

        [ObservableProperty]
        private Patient? _currentPatient;

        [ObservableProperty]
        private ObservableCollection<EchoRecord> _echoRecords = new();

        [ObservableProperty]
        private ObservableCollection<QueueEntry> _activeQueue = new();

        // Standalone Patient Lookup fields
        [ObservableProperty]
        private string _searchTerm = string.Empty;

        [ObservableProperty]
        private ObservableCollection<Patient> _patients = new();

        [ObservableProperty]
        private Patient? _selectedPatientLookup;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public EchoUploadViewModel(IEchoService echoService, IPatientService patientService, ISharedStateService sharedStateService, IConfiguration configuration, IQueueService queueService)
        {
            _echoService = echoService;
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            _configuration = configuration;
            _queueService = queueService;

            // Load initial patient context and subscribe to selection updates
            CurrentPatient = _sharedStateService.CurrentPatient;
            SelectedPatientLookup = CurrentPatient;
            _sharedStateService.CurrentPatientChanged += OnSharedPatientChanged;

            if (CurrentPatient != null)
            {
                _ = LoadEchoRecordsAsync();
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

        private void OnPollingTimerTick(object? sender, EventArgs e)
        {
            _ = PollQueueAsync();
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
                    _ = LoadEchoRecordsAsync();
                }
                else
                {
                    EchoRecords.Clear();
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
        public async Task LoadEchoRecordsAsync()
        {
            if (CurrentPatient == null) return;

            try
            {
                var records = await _echoService.GetEchoRecordsByPatientIdAsync(CurrentPatient.PatientId);
                EchoRecords = new ObservableCollection<EchoRecord>(records);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading Echo records: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task UploadEchoFileAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No patient selected. Please select a patient first.";
                return;
            }

            if (string.IsNullOrWhiteSpace(Title))
            {
                StatusMessage = "Please enter a Title/Description for the Echo record.";
                return;
            }

            // Read the central shared folder network storage path from configuration
            string? networkSharePath = _configuration["FileStorageSettings:NetworkSharePath"];
            if (string.IsNullOrEmpty(networkSharePath))
            {
                StatusMessage = "Storage configuration error: Central network folder path not defined.";
                return;
            }

            // Open standard file explorer selection dialog
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Media Files (*.mp4;*.avi;*.jpg;*.png;*.dcm)|*.mp4;*.avi;*.jpg;*.png;*.dcm|All files (*.*)|*.*",
                Title = "Select Echocardiogram File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                string localFilePath = openFileDialog.FileName;
                string extension = Path.GetExtension(localFilePath);
                
                // Formulate unique file name: PatientId_Timestamp_GUID.extension
                string uniqueFileName = $"{CurrentPatient.PatientId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid()}{extension}";
                string destinationPath = Path.Combine(networkSharePath, uniqueFileName);

                try
                {
                    StatusMessage = "Uploading file to server share...";

                    // Copy local file asynchronously to prevent locking the WPF UI thread
                    await Task.Run(() => 
                    {
                        if (!Directory.Exists(networkSharePath))
                        {
                            Directory.CreateDirectory(networkSharePath);
                        }
                        
                        File.Copy(localFilePath, destinationPath, true);
                    });

                    // Log file reference database entry
                    var record = new EchoRecord
                    {
                        PatientId = CurrentPatient.PatientId,
                        Title = Title,
                        FilePath = destinationPath,
                        Notes = Notes,
                        UploadDate = DateTime.UtcNow
                    };

                    await _echoService.AddEchoRecordAsync(record);
                    StatusMessage = "File uploaded and recorded successfully!";

                    // Clear fields
                    Title = string.Empty;
                    Notes = string.Empty;

                    await LoadEchoRecordsAsync();
                }
                catch (Exception ex)
                {
                    StatusMessage = $"File upload failed: {ex.Message}";
                }
            }
        }

        [RelayCommand]
        public async Task StartSessionAsync(QueueEntry entry)
        {
            if (entry == null) return;
            try
            {
                // Update queue status to InEcho
                await _queueService.UpdateQueueStatusAsync(entry.PatientId, "InEcho");

                // Get patient from DB
                var patient = await _patientService.GetPatientByIdAsync(entry.PatientId);
                SelectedPatientLookup = patient;
                StatusMessage = $"Echo session started for {entry.PatientName}.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error starting Echo session: {ex.Message}";
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
                // Set queue entry status as Completed
                await _queueService.CompleteQueueEntryAsync(CurrentPatient.PatientId);
                StatusMessage = $"Echo session for '{CurrentPatient.Name}' completed and removed from queue.";

                // Clear session
                SelectedPatientLookup = null;
                CurrentPatient = null;
                EchoRecords.Clear();

                Title = string.Empty;
                Notes = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error completing Echo session: {ex.Message}";
            }
        }

        [RelayCommand]
        public async Task CompleteSessionToExamAsync()
        {
            if (CurrentPatient == null)
            {
                StatusMessage = "No active patient to complete.";
                return;
            }

            try
            {
                // Return queue status to Pending (sends them back to doctor exam waitlist)
                await _queueService.UpdateQueueStatusAsync(CurrentPatient.PatientId, "Pending");
                StatusMessage = $"Echo session saved. '{CurrentPatient.Name}' sent to Exam waitlist.";

                // Clear session
                SelectedPatientLookup = null;
                CurrentPatient = null;
                EchoRecords.Clear();

                Title = string.Empty;
                Notes = string.Empty;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error sending patient back to Exam: {ex.Message}";
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
