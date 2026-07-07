namespace MedicalApp.Models
{
    public class PrintSettings
    {
        public string RxBackgroundPath { get; set; } = string.Empty;
        public bool PrintBackground { get; set; } = false;
        public double PatientInfoX { get; set; } = 50;
        public double PatientInfoY { get; set; } = 120;
        public double DrugsX { get; set; } = 50;
        public double DrugsY { get; set; } = 220;
        public double FontSize { get; set; } = 14;
    }
}
