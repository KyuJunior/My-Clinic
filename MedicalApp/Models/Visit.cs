using System;

namespace MedicalApp.Models
{
    public class Visit
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public DateTime VisitDate { get; set; } = DateTime.UtcNow;
        public string ChiefComplaint { get; set; } = string.Empty;
        public string HistoryOfPresentIllness { get; set; } = string.Empty;
        public string PhysicalExamination { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public string Prescription { get; set; } = string.Empty;

        // Vitals
        public string VitalsHR { get; set; } = string.Empty;
        public string VitalsSBP { get; set; } = string.Empty;
        public string VitalsDBP { get; set; } = string.Empty;
        public string VitalsRR { get; set; } = string.Empty;
        public string VitalsSPO2 { get; set; } = string.Empty;
        public string VitalsTemp { get; set; } = string.Empty;

        // Investigation & Imaging
        public string Investigation { get; set; } = string.Empty;
        public string Imaging { get; set; } = string.Empty;

        // Return Date
        public DateTime? ReturnDate { get; set; }

        // Navigation Property
        public Patient? Patient { get; set; }
    }
}
