using System;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace MedicalApp.Views
{
    public partial class DbConfigWindow : Window
    {
        public string SelectedConnectionString { get; private set; } = string.Empty;

        public DbConfigWindow(string existingConnectionString, string errorMsg)
        {
            InitializeComponent();
            
            // Try to pre-populate from existing connection string
            try
            {
                if (!string.IsNullOrEmpty(existingConnectionString))
                {
                    var builder = new SqlConnectionStringBuilder(existingConnectionString);
                    ServerTextBox.Text = builder.DataSource;
                    DatabaseTextBox.Text = builder.InitialCatalog;
                    if (builder.IntegratedSecurity)
                    {
                        AuthComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        AuthComboBox.SelectedIndex = 1;
                        UserTextBox.Text = builder.UserID;
                        PasswordTextBox.Password = builder.Password;
                    }
                }
            }
            catch
            {
                // Fallback to default values
            }

            if (!string.IsNullOrEmpty(errorMsg))
            {
                StatusTextBlock.Text = $"Connection Error: {errorMsg}";
            }
        }

        private void AuthComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SqlAuthPanel == null) return;
            if (AuthComboBox.SelectedIndex == 1)
            {
                SqlAuthPanel.Visibility = Visibility.Visible;
            }
            else
            {
                SqlAuthPanel.Visibility = Visibility.Collapsed;
            }
        }

        private string BuildConnectionString()
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = ServerTextBox.Text.Trim(),
                InitialCatalog = DatabaseTextBox.Text.Trim(),
                TrustServerCertificate = true,
                MultipleActiveResultSets = true
            };

            if (AuthComboBox.SelectedIndex == 0)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.IntegratedSecurity = false;
                builder.UserID = UserTextBox.Text.Trim();
                builder.Password = PasswordTextBox.Password;
            }

            return builder.ConnectionString;
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Yellow;
            StatusTextBlock.Text = "Connecting... | جاري الاتصال...";

            try
            {
                var connStr = BuildConnectionString();
                using var conn = new SqlConnection(connStr);
                conn.Open();
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.LightGreen;
                StatusTextBlock.Text = "Connection Succeeded! | تم الاتصال بنجاح!";
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Tomato;
                StatusTextBlock.Text = $"Failed: {ex.Message}";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var connStr = BuildConnectionString();
                using var conn = new SqlConnection(connStr);
                conn.Open();
                
                SelectedConnectionString = connStr;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Tomato;
                StatusTextBlock.Text = $"Failed: {ex.Message}\nSave aborted. Connection must succeed to save.";
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
