using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MedicalApp.Models;
using MedicalApp.Data;
using MedicalApp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalApp.ViewModels
{
    public partial class PrintSettingsViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IDbContextFactory<AppDbContext> _contextFactory;
        private readonly ISharedStateService _sharedStateService;
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_settings.json");

        [ObservableProperty]
        private string _rxBackgroundPath = string.Empty;

        [ObservableProperty]
        private bool _printBackground;

        [ObservableProperty]
        private double _patientNameX = 40;

        [ObservableProperty]
        private double _patientNameY = 100;

        [ObservableProperty]
        private double _patientAgeGenderX = 40;

        [ObservableProperty]
        private double _patientAgeGenderY = 125;

        [ObservableProperty]
        private double _patientDateX = 230;

        [ObservableProperty]
        private double _patientDateY = 100;

        [ObservableProperty]
        private double _rxSymbolX = 40;

        [ObservableProperty]
        private double _rxSymbolY = 160;

        [ObservableProperty]
        private bool _showRxSymbol = true;

        [ObservableProperty]
        private double _drugsX = 40;

        [ObservableProperty]
        private double _drugsY = 200;

        [ObservableProperty]
        private double _fontSize = 14;

        [ObservableProperty]
        private int _activeTab = 0; // 0=Print, 1=Clinic, 2=Database, 3=Staff, 4=Templates

        // Clinic Profile Settings
        [ObservableProperty]
        private string _clinicNameAr = "عيادتي التخصصية";

        [ObservableProperty]
        private string _clinicNameEn = "My Specialty Clinic";

        [ObservableProperty]
        private string _clinicPhone = "+964 770 123 4567";

        [ObservableProperty]
        private string _clinicAddress = "Baghdad, Iraq";

        [ObservableProperty]
        private string _clinicSpecialty = "Gynecology & Obstetrics | التوليد وأمراض النساء";

        // Database & Backups
        [ObservableProperty]
        private string _dbBackupPath = @"C:\Myapps\Backups";

        [ObservableProperty]
        private string _dbBackupInterval = "Daily | يومي";

        [ObservableProperty]
        private bool _dbAutoBackupEnabled = true;

        // Staff Settings
        [ObservableProperty]
        private string _adminPassword = "••••••••";

        [ObservableProperty]
        private bool _requireLogin = false;

        [RelayCommand]
        public void SwitchTab(string tabIndex)
        {
            if (int.TryParse(tabIndex, out int index))
            {
                ActiveTab = index;
            }
        }

        public PrintSettingsViewModel(
            IServiceProvider serviceProvider,
            IDbContextFactory<AppDbContext> contextFactory,
            ISharedStateService sharedStateService)
        {
            _serviceProvider = serviceProvider;
            _contextFactory = contextFactory;
            _sharedStateService = sharedStateService;
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                PrintSettings? settings = null;
                var activeDocName = _sharedStateService.ActiveDoctorName;
                if (!string.IsNullOrEmpty(activeDocName))
                {
                    using var context = _contextFactory.CreateDbContext();
                    var record = context.DoctorSettings.FirstOrDefault(s => s.DoctorName == activeDocName);
                    if (record != null)
                    {
                        settings = JsonSerializer.Deserialize<PrintSettings>(record.SettingsJson);
                    }
                }

                if (settings == null)
                {
                    if (File.Exists(SettingsFile))
                    {
                        string json = File.ReadAllText(SettingsFile);
                        settings = JsonSerializer.Deserialize<PrintSettings>(json);
                    }
                }

                if (settings != null)
                {
                    RxBackgroundPath = settings.RxBackgroundPath;
                    PrintBackground = settings.PrintBackground;
                    
                    PatientNameX = settings.PatientNameX;
                    PatientNameY = settings.PatientNameY;
                    
                    PatientAgeGenderX = settings.PatientAgeGenderX;
                    PatientAgeGenderY = settings.PatientAgeGenderY;
                    
                    PatientDateX = settings.PatientDateX;
                    PatientDateY = settings.PatientDateY;
                    
                    RxSymbolX = settings.RxSymbolX;
                    RxSymbolY = settings.RxSymbolY;
                    ShowRxSymbol = settings.ShowRxSymbol;
                    
                    DrugsX = settings.DrugsX;
                    DrugsY = settings.DrugsY;
                    FontSize = settings.FontSize;

                    // Clinic Profile
                    ClinicNameAr = settings.ClinicNameAr ?? "عيادتي التخصصية";
                    ClinicNameEn = settings.ClinicNameEn ?? "My Specialty Clinic";
                    ClinicPhone = settings.ClinicPhone ?? "+964 770 123 4567";
                    ClinicAddress = settings.ClinicAddress ?? "Baghdad, Iraq";
                    ClinicSpecialty = settings.ClinicSpecialty ?? "Gynecology & Obstetrics | التوليد وأمراض النساء";

                    // Database & Backups
                    DbBackupPath = settings.DbBackupPath ?? @"C:\Myapps\Backups";
                    DbBackupInterval = settings.DbBackupInterval ?? "Daily | يومي";
                    DbAutoBackupEnabled = settings.DbAutoBackupEnabled;

                    // Staff Settings
                    AdminPassword = settings.AdminPassword ?? "••••••••";
                    RequireLogin = settings.RequireLogin;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load print settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        public void UploadImage()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
                Title = "Select Prescription Background Image Template"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string ext = Path.GetExtension(openFileDialog.FileName);
                    var activeDocName = _sharedStateService.ActiveDoctorName;
                    var suffix = string.IsNullOrEmpty(activeDocName) ? "default" : activeDocName.Replace(" ", "_").Replace(".", "");
                    string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"rx_template_{suffix}{ext}");

                    // If file already exists, delete it first to overwrite cleanly
                    if (File.Exists(destPath))
                    {
                        File.Delete(destPath);
                    }

                    File.Copy(openFileDialog.FileName, destPath, true);
                    RxBackgroundPath = destPath;
                    MessageBox.Show("Background template uploaded successfully!", "Template Uploaded", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to copy template image: {ex.Message}", "Upload Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        public void SaveSettings()
        {
            try
            {
                var settings = new PrintSettings
                {
                    RxBackgroundPath = RxBackgroundPath,
                    PrintBackground = PrintBackground,
                    
                    PatientNameX = PatientNameX,
                    PatientNameY = PatientNameY,
                    
                    PatientAgeGenderX = PatientAgeGenderX,
                    PatientAgeGenderY = PatientAgeGenderY,
                    
                    PatientDateX = PatientDateX,
                    PatientDateY = PatientDateY,
                    
                    RxSymbolX = RxSymbolX,
                    RxSymbolY = RxSymbolY,
                    ShowRxSymbol = ShowRxSymbol,
                    
                    DrugsX = DrugsX,
                    DrugsY = DrugsY,
                    FontSize = FontSize,

                    ClinicNameAr = ClinicNameAr,
                    ClinicNameEn = ClinicNameEn,
                    ClinicPhone = ClinicPhone,
                    ClinicAddress = ClinicAddress,
                    ClinicSpecialty = ClinicSpecialty,

                    DbBackupPath = DbBackupPath,
                    DbBackupInterval = DbBackupInterval,
                    DbAutoBackupEnabled = DbAutoBackupEnabled,

                    AdminPassword = AdminPassword,
                    RequireLogin = RequireLogin
                };

                // Save to database for active doctor
                var activeDocName = _sharedStateService.ActiveDoctorName;
                if (!string.IsNullOrEmpty(activeDocName))
                {
                    using var context = _contextFactory.CreateDbContext();
                    var record = context.DoctorSettings.FirstOrDefault(s => s.DoctorName == activeDocName);
                    string serializedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                    if (record != null)
                    {
                        record.SettingsJson = serializedJson;
                        context.DoctorSettings.Update(record);
                    }
                    else
                    {
                        var newRecord = new DoctorSetting
                        {
                            DoctorName = activeDocName,
                            SettingsJson = serializedJson
                        };
                        context.DoctorSettings.Add(newRecord);
                    }
                    context.SaveChanges();
                }

                // Fallback: save to local file
                string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsFile, json);
                MessageBox.Show("Print calibration settings saved successfully!", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save print settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        public void NavigateBack()
        {
            var mainVm = _serviceProvider.GetRequiredService<MainViewModel>();
            mainVm.NavigateToHome();
        }
    }
}
