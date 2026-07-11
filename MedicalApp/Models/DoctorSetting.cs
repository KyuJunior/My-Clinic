namespace MedicalApp.Models
{
    public class DoctorSetting
    {
        public int DoctorSettingId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string SettingsJson { get; set; } = string.Empty;
    }
}
