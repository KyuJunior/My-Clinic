using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MedicalApp.Models;

namespace MedicalApp.Services
{
    public interface IPrintService
    {
        void PrintReport(Patient patient, Visit visit);
        void PrintPrescription(Patient patient, string prescriptionText);
    }

    public class PrintService : IPrintService
    {
        private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "print_settings.json");

        private PrintSettings LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var settings = JsonSerializer.Deserialize<PrintSettings>(json);
                    if (settings != null) return settings;
                }
            }
            catch
            {
                // Fallback to defaults
            }
            return new PrintSettings();
        }

        public void PrintReport(Patient patient, Visit visit)
        {
            var doc = new FlowDocument
            {
                PageWidth = 794,  // A4 width at 96 DPI
                PageHeight = 1123, // A4 height at 96 DPI
                PagePadding = new Thickness(50),
                Background = Brushes.White,
                FontFamily = new FontFamily("Segoe UI")
            };

            // Report Title / Clinic Header
            var titlePara = new Paragraph(new Run("CardioCenter Clinic"))
            {
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136)), // Teal 600
                Margin = new Thickness(0, 0, 0, 5)
            };
            doc.Blocks.Add(titlePara);

            var subtitlePara = new Paragraph(new Run($"Clinical Summary Report — {visit.VisitDate.ToLocalTime():dd/MM/yyyy hh:mm tt}"))
            {
                FontSize = 12,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20)
            };
            doc.Blocks.Add(subtitlePara);

            // Divider Line
            doc.Blocks.Add(new BlockUIContainer(new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 0, 0, 20) }));

            // Patient Information Grid
            var patientPara = new Paragraph();
            patientPara.Inlines.Add(new Bold(new Run("Patient Name: ")) { Foreground = Brushes.DarkSlateGray });
            patientPara.Inlines.Add(new Run($"{patient.Name}\n"));
            patientPara.Inlines.Add(new Bold(new Run("Age: ")) { Foreground = Brushes.DarkSlateGray });
            patientPara.Inlines.Add(new Run($"{patient.Age} yrs   "));
            patientPara.Inlines.Add(new Bold(new Run("Gender: ")) { Foreground = Brushes.DarkSlateGray });
            patientPara.Inlines.Add(new Run($"{patient.Gender}   "));
            patientPara.Inlines.Add(new Bold(new Run("Phone: ")) { Foreground = Brushes.DarkSlateGray });
            patientPara.Inlines.Add(new Run($"{patient.Phone}\n"));
            if (!string.IsNullOrEmpty(patient.Address))
            {
                patientPara.Inlines.Add(new Bold(new Run("Address: ")) { Foreground = Brushes.DarkSlateGray });
                patientPara.Inlines.Add(new Run($"{patient.Address}"));
            }
            patientPara.FontSize = 12;
            patientPara.LineHeight = 20;
            doc.Blocks.Add(patientPara);

            // Divider Line
            doc.Blocks.Add(new BlockUIContainer(new Border { Height = 1, Background = Brushes.LightGray, Margin = new Thickness(0, 10, 0, 20) }));

            // Add Clinical Section Helper
            void AddSection(string header, string content)
            {
                if (string.IsNullOrWhiteSpace(content)) return;

                var headerPara = new Paragraph(new Run(header))
                {
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136)),
                    Margin = new Thickness(0, 10, 0, 5)
                };
                doc.Blocks.Add(headerPara);

                var contentPara = new Paragraph(new Run(content))
                {
                    FontSize = 12,
                    LineHeight = 18,
                    Margin = new Thickness(0, 0, 0, 15)
                };
                doc.Blocks.Add(contentPara);
            }

            AddSection("Chief Complaint", visit.ChiefComplaint);
            AddSection("History of Present Illness", visit.HistoryOfPresentIllness);
            AddSection("Physical Examination", visit.PhysicalExamination);
            AddSection("Diagnosis", visit.Diagnosis);
            AddSection("Treatment Plan", visit.TreatmentPlan);
            AddSection("Prescribed Medications (Rx)", visit.Prescription);

            // Open Print Preview Window
            var preview = new Views.PrintPreviewWindow(doc, $"CardioCenter Report - {patient.Name}")
            {
                Owner = Application.Current.MainWindow
            };
            preview.ShowDialog();
        }

        public void PrintPrescription(Patient patient, string prescriptionText)
        {
            var settings = LoadSettings();
            var doc = new FlowDocument
            {
                PageWidth = 560,  // A5 width at 96 DPI
                PageHeight = 794, // A5 height at 96 DPI
                PagePadding = new Thickness(0),
                Background = Brushes.White,
                FontFamily = new FontFamily("Segoe UI")
            };

            var canvas = new Canvas
            {
                Width = 560,
                Height = 794
            };

            // 1. Draw background template image if enabled
            if (settings.PrintBackground && File.Exists(settings.RxBackgroundPath))
            {
                try
                {
                    var img = new Image
                    {
                        Source = new BitmapImage(new Uri(settings.RxBackgroundPath)),
                        Width = 560,
                        Height = 794,
                        Stretch = Stretch.Fill
                    };
                    canvas.Children.Add(img);
                    Canvas.SetLeft(img, 0);
                    Canvas.SetTop(img, 0);
                }
                catch
                {
                    // Fallback silently if image cannot load
                }
            }

            // 2. Draw Patient Info (scaled from preview dimensions 350x495 to paper dimensions 560x794)
            var infoPanel = new StackPanel();
            infoPanel.Children.Add(new TextBlock 
            { 
                Text = $"Patient Name: {patient.Name}", 
                FontSize = 13, 
                FontWeight = FontWeights.Bold, 
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)) 
            });
            infoPanel.Children.Add(new TextBlock 
            { 
                Text = $"Age: {patient.Age} yrs   |   Gender: {patient.Gender}   |   Date: {DateTime.Now:dd/MM/yyyy}", 
                FontSize = 10, 
                Foreground = Brushes.SlateGray, 
                Margin = new Thickness(0, 3, 0, 0) 
            });

            canvas.Children.Add(infoPanel);
            Canvas.SetLeft(infoPanel, settings.PatientInfoX * (560.0 / 350.0));
            Canvas.SetTop(infoPanel, settings.PatientInfoY * (794.0 / 495.0));

            // 3. Draw Medications Block (scaled)
            var drugsPanel = new StackPanel();
            drugsPanel.Children.Add(new TextBlock 
            { 
                Text = "Rx", 
                FontSize = 20, 
                FontWeight = FontWeights.Bold, 
                Foreground = new SolidColorBrush(Color.FromRgb(13, 148, 136)),
                Margin = new Thickness(0, 0, 0, 8) 
            });

            string[] lines = prescriptionText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                drugsPanel.Children.Add(new TextBlock 
                { 
                    Text = $"{i + 1}. {lines[i]}", 
                    FontSize = settings.FontSize, 
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Margin = new Thickness(0, 5, 0, 0), 
                    TextWrapping = TextWrapping.Wrap,
                    Width = 450 // Wrap drugs line to avoid printing off-page
                });
            }

            canvas.Children.Add(drugsPanel);
            Canvas.SetLeft(drugsPanel, settings.DrugsX * (560.0 / 350.0));
            Canvas.SetTop(drugsPanel, settings.DrugsY * (794.0 / 495.0));

            doc.Blocks.Add(new BlockUIContainer(canvas));

            // Open Print Preview Window
            var preview = new Views.PrintPreviewWindow(doc, $"CardioCenter Rx - {patient.Name}")
            {
                Owner = Application.Current.MainWindow
            };
            preview.ShowDialog();
        }
    }
}
