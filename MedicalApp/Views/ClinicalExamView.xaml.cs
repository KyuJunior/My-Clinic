using System.Windows;
using System.Windows.Controls;

namespace MedicalApp.Views
{
    public partial class ClinicalExamView : UserControl
    {
        public ClinicalExamView()
        {
            InitializeComponent();
        }

        private void GearButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                button.ContextMenu.IsOpen = true;
            }
        }
    }
}
