using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MedicalApp.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly string _documentTitle;

        public PrintPreviewWindow(FlowDocument document, string documentTitle)
        {
            InitializeComponent();
            _document = document;
            _documentTitle = documentTitle;
            DocViewer.Document = _document;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            var printDialog = new PrintDialog();
            
            // Set default page media size for A5 if document name suggests it is an A5 Prescription
            if (_documentTitle.Contains("Rx") || _document.PageWidth == 560)
            {
                printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(System.Printing.PageMediaSizeName.ISOA5);
            }

            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(((IDocumentPaginatorSource)_document).DocumentPaginator, _documentTitle);
                DialogResult = true;
                Close();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
