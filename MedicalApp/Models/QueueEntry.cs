using System;

namespace MedicalApp.Models
{
    public class QueueEntry
    {
        public int QueueEntryId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, InExam, InEcho, Completed
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        public Patient? Patient { get; set; }
    }
}
