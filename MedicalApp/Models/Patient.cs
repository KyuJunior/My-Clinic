using System;
using System.Collections.Generic;

namespace MedicalApp.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Job { get; set; } = string.Empty;
        public string Governorate { get; set; } = string.Empty;
        public int AgeMonths { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? SpouseBirthDate { get; set; }
        public string HasChildren { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string PatientFiles { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}
