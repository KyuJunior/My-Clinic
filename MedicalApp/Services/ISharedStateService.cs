using System;
using MedicalApp.Models;

namespace MedicalApp.Services
{
    public interface ISharedStateService
    {
        Patient? CurrentPatient { get; set; }
        string? ActiveDoctorName { get; set; }
        System.Collections.Generic.HashSet<string> AuthenticatedDoctors { get; }
        event Action<Patient?>? CurrentPatientChanged;
    }
}
