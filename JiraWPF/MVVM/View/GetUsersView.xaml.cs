using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
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

namespace JiraWPF.MVVM.View
{
    /// <summary>
    /// Interaction logic for GetUsersView.xaml
    /// </summary>
    public partial class GetUsersView : UserControl
    {
        public GetUsersView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Load JiraGroups from settings.settings
            var jiraGroups = Properties.Settings.Default.JiraGroups;

            if (jiraGroups == null || jiraGroups.Count == 0)
            {
                GetJiraGroupsButton_Click(sender, e);
            }
            else
            {
                // JiraGroups is not empty, populate the ListBox
                var collectionView = CollectionViewSource.GetDefaultView(jiraGroups);
                GroupListBox.ItemsSource = collectionView;
                GroupListBox.SelectedIndex = 0;

                GroupFilterSearchTextBox.TextChanged += (s, _) =>
                {
                    // store currently selected items
                    var selectedItems = new List<string>();
                    foreach (var item in GroupListBox.SelectedItems)
                    {
                        selectedItems.Add((string)item);
                    }

                    // apply search filter
                    collectionView.Filter = item =>
                    {
                        // when TextBox is empty, show all items
                        if (string.IsNullOrEmpty(GroupFilterSearchTextBox.Text))
                            return true;

                        // Show only items that contain the text in the TextBox 
                        return ((string)item).ToLower().Contains(GroupFilterSearchTextBox.Text.ToLower());
                    };

                    // reselect the previously selected items
                    foreach (var item in selectedItems)
                    {
                        GroupListBox.SelectedItems.Add(item);
                    }
                };
            }
        }

        private async void GetJiraGroupsButton_Click(object sender, RoutedEventArgs e)
        {
            // hide all list items in GroupListBox
            GroupListBox.ItemsSource = null;

            // get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            string script = @"
            param($JiraURL, $JiraToken)
            # API endpoint to get all groups
            $API = '/rest/api/2/groups/picker'

            $Header = @{
                Authorization = 'Bearer ' + $JiraToken
            }

            # Get all the groups
            $uri = $JiraURL + $API
            try {
                $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $Header -ErrorAction Stop

                # Check if the response is not null
                if ($response -ne $null) {
                    $groupNames = @()
                    foreach ($group in $response.groups) {
                        $groupNames += $group.name
                    }
                    # Convert the array to a single line comma-separated string
                    $groupNamesString = $groupNames -join ','
                    return $groupNamesString
                }
            } catch {
                return 1 # Error occurred while retrieving groups
            }";

            // Show loading
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
            LoadingGifImage.Visibility = Visibility.Hidden;

            if (result != null)
            {
                if (result != "1" && !string.IsNullOrEmpty(result))
                {
                    // Convert comma-separated string returned from script  to StringCollection
                    var stringCollection = new System.Collections.Specialized.StringCollection();
                    stringCollection.AddRange(result.Split(','));

                    // add "None" option at the start of the list
                    stringCollection.Insert(0, "None");

                    // store the StringCollection in settings.settings
                    Properties.Settings.Default.JiraGroups = stringCollection;
                    Properties.Settings.Default.Save();

                    // Populate the ListBox with the retrieved groups including "None" option
                    var collectionView = CollectionViewSource.GetDefaultView(stringCollection);
                    GroupListBox.ItemsSource = collectionView;

                    Window_Loaded(sender, e);
                }
                else
                {
                    MessageBox.Show("Could not retrieve Jira Groups\n\n" +
                                    "  1. Verify your VPN connection.\n" +
                                    "  2. Double-check the correctness of your Jira URL or token\n" +
                                    "      in the Settings.", "Connection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void GroupListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If "None" is selected, deselect all other options
            if (GroupListBox.SelectedItems.Contains("None") && GroupListBox.SelectedItems.Count > 1)
            {
                while (GroupListBox.SelectedItems.Count > 1)
                {
                    GroupListBox.SelectedItems.Remove(GroupListBox.SelectedItems[0]);
                }
            }

            // If all options except "None" are selected, deselect all of them and select "None"
            if (GroupListBox.SelectedItems.Count == Properties.Settings.Default.JiraGroups.Count - 1 && !GroupListBox.SelectedItems.Contains("None"))
            {
                GroupListBox.SelectedItems.Clear();
                GroupListBox.SelectedItem = "None";
                GroupListBox.ScrollIntoView(GroupListBox.Items[0]); // Scroll to the top of list box where "None" is
            }

            // If no item is selected, select "None"
            if (GroupListBox.SelectedItems.Count == 0)
            {
                GroupListBox.SelectedItem = "None";
            }
        }

    }
}
