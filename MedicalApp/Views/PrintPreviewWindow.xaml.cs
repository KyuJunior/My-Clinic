using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace MedicalApp.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly FlowDocument _document;
        private readonly string _documentTitle;
        private readonly bool _printBackground;
        private readonly Image? _bgImage;

        public PrintPreviewWindow(FlowDocument document, string documentTitle, bool printBackground = true, Image? bgImage = null)
        {
            InitializeComponent();
            _document = document;
            _documentTitle = documentTitle;
            _printBackground = printBackground;
            _bgImage = bgImage;
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
                // Collapse background image if we should NOT print it on physical paper
                if (!_printBackground && _bgImage != null)
                {
                    _bgImage.Visibility = Visibility.Collapsed;
                }

                try
                {
                    printDialog.PrintDocument(((IDocumentPaginatorSource)_document).DocumentPaginator, _documentTitle);
                }
                finally
                {
                    // Restore background visibility for screen preview
                    if (_bgImage != null)
                    {
                        _bgImage.Visibility = Visibility.Visible;
                    }
                }

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
