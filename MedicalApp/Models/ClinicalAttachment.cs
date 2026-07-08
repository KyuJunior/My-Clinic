using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MedicalApp.Models
{
    public partial class ClinicalAttachment : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AttachmentName))]
        private string _name = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AttachmentName))]
        private string _attachmentPath = string.Empty;

        public string AttachmentName => string.IsNullOrWhiteSpace(AttachmentPath) 
            ? string.Empty 
            : Path.GetFileName(AttachmentPath);
    }
}
