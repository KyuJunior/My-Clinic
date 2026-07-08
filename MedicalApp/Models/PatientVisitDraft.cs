using System;
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
        public List<PrescribedMedication> PrescribedDrugs { get; set; } = new();

        // Vitals
        public string VitalsHR { get; set; } = string.Empty;
        public string VitalsSBP { get; set; } = string.Empty;
        public string VitalsDBP { get; set; } = string.Empty;
        public string VitalsRR { get; set; } = string.Empty;
        public string VitalsSPO2 { get; set; } = string.Empty;
        public string VitalsTemp { get; set; } = string.Empty;
        public bool IsVitallyStable { get; set; }

        // Investigation & Imaging
        public string Investigation { get; set; } = string.Empty;
        public string Imaging { get; set; } = string.Empty;
        public List<ClinicalAttachment> Investigations { get; set; } = new();
        public List<ClinicalAttachment> Imagings { get; set; } = new();

        // Return Date
        public DateTime? ReturnDate { get; set; }

        // Attachments
        public string InvestigationAttachmentPath { get; set; } = string.Empty;
        public string ImagingAttachmentPath { get; set; } = string.Empty;
    }
}
