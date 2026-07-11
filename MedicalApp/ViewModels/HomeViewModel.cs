using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MedicalApp.Services;
using MedicalApp.Models;

namespace MedicalApp.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IThemeService _themeService;
        private readonly IPatientService _patientService;
        private readonly ISharedStateService _sharedStateService;

        public bool IsDarkMode => _themeService.IsDarkMode;

        [ObservableProperty]
        private ObservableCollection<Doctor> _doctors = new();

        [ObservableProperty]
        private Doctor? _selectedDoctor;

        [ObservableProperty]
        private string _doctorPasswordAttempt = string.Empty;

        [ObservableProperty]
        private bool _showDoctorLoginModal = false;

        [ObservableProperty]
        private string _loginErrorMessage = string.Empty;

        [RelayCommand]
        public void ToggleTheme()
        {
            _themeService.ToggleTheme();
        }

        public HomeViewModel(IServiceProvider serviceProvider, IThemeService themeService, IPatientService patientService, ISharedStateService sharedStateService)
        {
            _serviceProvider = serviceProvider;
            _themeService = themeService;
            _patientService = patientService;
            _sharedStateService = sharedStateService;
            System.Windows.WeakEventManager<IThemeService, EventArgs>.AddHandler(_themeService, nameof(IThemeService.ThemeChanged), (s, ev) => OnPropertyChanged(nameof(IsDarkMode)));
        }

        [RelayCommand]
        public void NavigateToRegistry()
        {
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToPatientRegistration();
        }

        [RelayCommand]
        public void NavigateToExam()
        {
            if (!string.IsNullOrEmpty(_sharedStateService.ActiveDoctorName) &&
                _sharedStateService.AuthenticatedDoctors.Contains(_sharedStateService.ActiveDoctorName))
            {
                var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
                mainVm.NavigateToClinicalExam();
            }
            else
            {
                _ = OpenDoctorLoginModal();
            }
        }

        [RelayCommand]
        public async Task OpenDoctorLoginModal()
        {
            try
            {
                var docList = await _patientService.GetAllDoctorsAsync();
                Doctors.Clear();
                foreach (var doc in docList)
                {
                    Doctors.Add(doc);
                }
                if (Doctors.Count > 0)
                {
                    SelectedDoctor = Doctors[0];
                }
                DoctorPasswordAttempt = string.Empty;
                LoginErrorMessage = string.Empty;
                ShowDoctorLoginModal = true;
            }
            catch (Exception ex)
            {
                LoginErrorMessage = $"Failed to load doctors: {ex.Message}";
            }
        }

        [RelayCommand]
        public void CloseDoctorLoginModal()
        {
            ShowDoctorLoginModal = false;
            DoctorPasswordAttempt = string.Empty;
            LoginErrorMessage = string.Empty;
        }

        [RelayCommand]
        public void ConfirmDoctorLogin()
        {
            if (SelectedDoctor == null)
            {
                LoginErrorMessage = "Please select a doctor | الرجاء اختيار طبيب";
                return;
            }

            bool isAlreadyAuth = _sharedStateService.AuthenticatedDoctors.Contains(SelectedDoctor.Name);

            if (isAlreadyAuth || SelectedDoctor.Password == DoctorPasswordAttempt)
            {
                if (!isAlreadyAuth)
                {
                    _sharedStateService.AuthenticatedDoctors.Add(SelectedDoctor.Name);
                }

                _sharedStateService.ActiveDoctorName = SelectedDoctor.Name;
                ShowDoctorLoginModal = false;
                DoctorPasswordAttempt = string.Empty;
                LoginErrorMessage = string.Empty;

                var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
                mainVm.NavigateToClinicalExam();
            }
            else
            {
                LoginErrorMessage = "Incorrect Password! | كلمة المرور غير صحيحة!";
            }
        }
    }
}
