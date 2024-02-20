using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management.Automation;

namespace JiraWPF.MVVM.View
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        private bool isInitializing = true;

        public Settings()
        {
            InitializeComponent();
            JiraURLTextBox.Text = Properties.Settings.Default.JiraURL;
            JiraAccessTokenTextBox.Text = Properties.Settings.Default.JiraAccessToken;
            isInitializing = false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitializing)
            {
                if (sender == JiraURLTextBox)
                {
                    SaveJiraURLButton.IsEnabled = true;
                }
                else if (sender == JiraAccessTokenTextBox)
                {
                    SaveJiraAccessTokenButton.IsEnabled = true;
                }
            }
        }

        private void SaveJiraURLButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.JiraURL = JiraURLTextBox.Text;
            Properties.Settings.Default.Save();
            SaveJiraURLButton.IsEnabled = false;
        }

        private void SaveJiraAccessTokenButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.JiraAccessToken = JiraAccessTokenTextBox.Text;
            Properties.Settings.Default.Save();
            SaveJiraAccessTokenButton.IsEnabled = false;
        }

        private async void VerifyTokenButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            // Call PowerShell script
            string script = @"
            param($JiraURL, $JiraToken)
            $API = '/rest/api/2/myself?expand=groups'
            $Header = @{
                Authorization = 'Bearer ' + $JiraToken
            }
            $uri = $JiraURL + $API
            try {
                $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $Header -ErrorAction Stop
                if ($response -ne $null) {
                    if ($response.groups.items.name -contains 'jira-administrators') {
                        return '1'  # Valid and admin token
                    }
                    return '2'  # Valid but not admin token
                }
            } catch {
                return '3'  # Invalid token
            }";

            // Show loading
            TokenStatusTextBlock.Visibility = Visibility.Hidden;
            VerifyProgresTextBox.Visibility = Visibility.Visible;
            LoadingGifImage.Visibility = Visibility.Visible;

            string result = await Task.Run(() =>
            {
                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.AddScript(script);
                    // Passes the values from our settings to the PowerShell script
                    powerShell.AddParameter("JiraURL", jiraURL);
                    powerShell.AddParameter("JiraToken", jiraToken);

                    var results = powerShell.Invoke<string>();
                    if (results.Count > 0)
                    {
                        return results[0];
                    }
                    return null;
                }
            });

            // Hide the loading
            VerifyProgresTextBox.Visibility = Visibility.Hidden;
            LoadingGifImage.Visibility = Visibility.Hidden;

            if (result != null)
            {
                TokenStatusTextBlock.Visibility = Visibility.Visible;
                if (result == "1")
                {
                    TokenStatusTextBlock.Text = "Jira Access Token valid with Admin Privalges ✓";
                }
                else if (result == "2")
                {
                    TokenStatusTextBlock.Text = "Jira Access Token valid but without Admin Privalges ✓";
                }
                else
                {
                    TokenStatusTextBlock.Text = "Jira Access Token is invalid ✘";
                }
            }

        }



    }
}
