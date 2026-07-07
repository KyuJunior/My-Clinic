using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MedicalApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MedicalApp.ViewModels
{
    public partial class PrintSettingsViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_settings.json");

        [ObservableProperty]
        private string _rxBackgroundPath = string.Empty;

        [ObservableProperty]
        private bool _printBackground;

        [ObservableProperty]
        private double _patientInfoX = 50;

        [ObservableProperty]
        private double _patientInfoY = 120;

        [ObservableProperty]
        private double _drugsX = 50;

        [ObservableProperty]
        private double _drugsY = 220;

        [ObservableProperty]
        private double _fontSize = 14;

        public PrintSettingsViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            LoadSettings();
        }

        public void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<PrintSettings>(json);
                    if (settings != null)
                    {
                        RxBackgroundPath = settings.RxBackgroundPath;
                        PrintBackground = settings.PrintBackground;
                        PatientInfoX = settings.PatientInfoX;
                        PatientInfoY = settings.PatientInfoY;
                        DrugsX = settings.DrugsX;
                        DrugsY = settings.DrugsY;
                        FontSize = settings.FontSize;
                    }
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
                    string destPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"rx_template{ext}");

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
                    PatientInfoX = PatientInfoX,
                    PatientInfoY = PatientInfoY,
                    DrugsX = DrugsX,
                    DrugsY = DrugsY,
                    FontSize = FontSize
                };

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
