using System;

namespace MedicalApp.Models
{
    public class PrescribedMedication
    {
        public string Name { get; set; } = string.Empty;
        public string Dose { get; set; } = string.Empty;
        public string Type { get; set; } = "Select...";
        public string Time { get; set; } = "Select...";
        public string Note { get; set; } = string.Empty;

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string> { Name };
            if (!string.IsNullOrWhiteSpace(Dose)) parts.Add($"Dose: {Dose}");
            if (!string.IsNullOrWhiteSpace(Type) && Type != "Select...") parts.Add($"Type: {Type}");
            if (!string.IsNullOrWhiteSpace(Time) && Time != "Select...") parts.Add($"Time: {Time}");
            if (!string.IsNullOrWhiteSpace(Note)) parts.Add($"({Note})");
            return string.Join(" | ", parts);
        }
    }
}
