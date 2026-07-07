using System.Windows;
using MedicalApp.ViewModels;

namespace MedicalApp.Views
{
    public partial class HomeLauncherWindow : Window
    {
        public HomeLauncherWindow(HomeLauncherViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
