using System;
using MedicalApp.Models;

namespace MedicalApp.Services
{
    public class SharedStateService : ISharedStateService
    {
        private Patient? _currentPatient;

        public string? ActiveDoctorName { get; set; }
        public System.Collections.Generic.HashSet<string> AuthenticatedDoctors { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Patient? CurrentPatient
        {
            get => _currentPatient;
            set
            {
                if (_currentPatient != value)
                {
                    _currentPatient = value;
                    CurrentPatientChanged?.Invoke(_currentPatient);
                }
            }
        }

        public event Action<Patient?>? CurrentPatientChanged;
    }
}
