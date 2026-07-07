using System.Collections.Generic;

namespace MedicalApp.Models
{
    public class PatientVisitDraft
    {
        public int PatientId { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string HistoryOfPresentIllness { get; set; } = string.Empty;
        public string PhysicalExamination { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string TreatmentPlan { get; set; } = string.Empty;
        public List<string> PrescribedDrugs { get; set; } = new();
    }
}
