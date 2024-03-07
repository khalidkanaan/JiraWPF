using JiraWPF.MVVM.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                if (!string.IsNullOrEmpty(GroupFilterSearchTextBox.Text))
                {
                    GroupListBox.SelectedItems.Clear();
                }
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

        private string CheckRadioButtonStatus()
        {
            if (All.IsChecked == true)
            {
                return "all";
            }
            else if (Active.IsChecked == true)
            {
                return "active";
            }
            else
            {
                return "inactive";
            }
        }

        private async void GetJiraUsers_Click(object sender, RoutedEventArgs e)
        {
            GetUsersViewModel viewModel = (GetUsersViewModel)DataContext;
            // get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            // retrieve selected options from checkboxes
            string allSelected = viewModel.All ? "1" : "0";
            string usernameSelected = viewModel.Username ? "1" : "0";
            string displayNameSelected = viewModel.DisplayName ? "1" : "0";
            string emailSelected = viewModel.Email ? "1" : "0";
            string groupsSelected = viewModel.Groups ? "1" : "0";

            // retrieve selected radio button option
            string userStatus = CheckRadioButtonStatus();

            // string to store the selected group options
            string selectedGroupsString;
            var selectedGroups = GroupListBox.SelectedItems;

            if (selectedGroups.Count == 1 && selectedGroups.Contains("None"))
            {
                // 'When 'None' is selected retrieve all values from Properties.Settings.Default.JiraGroups except for 'None'
                var allGroupsExceptNone = Properties.Settings.Default.JiraGroups.Cast<string>().Where(group => group != "None");
                selectedGroupsString = string.Join(", ", allGroupsExceptNone);
            }
            else
            {
                // Concatenate the selected items into a single string separated by commas
                selectedGroupsString = string.Join(", ", selectedGroups.Cast<object>());
            }

            string script = @"
                    param($JiraURL, $JiraToken, $UserStatus, 
                          $all, $usernameCheckBox, $displayNameCheckBox, 
                          $emailAddressCheckBox, $groupsCheckBox, $JiraGroupsString)
                    $Header = @{
                        Authorization = 'Bearer ' + $JiraToken
                    }

                    $API = '/rest/api/2/group/member?groupname='
                    $JiraGroups = @()
                    $JiraGroups = $JiraGroupsString -split ',\s*' | ForEach-Object { $_.Trim() }
                    $UserDetails = @{}

                    foreach ($GroupName in $JiraGroups) {
                        $startAt = 0
                        $maxResults = 500
                        do {
                            $uri = $JiraURL + $API + $GroupName + '&includeInactiveUsers=true' + '&startAt=' + $startAt + '&maxResults=' + $maxResults

                            try {
                                $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $Header -ErrorAction Stop

                                if ($response -ne $null) {
                                    foreach ($user in $response.values) {
                                        if ($UserStatus -eq 'all' -or ($UserStatus -eq 'active' -and $user.active) -or ($UserStatus -eq 'inactive' -and !$user.active)) {
                                            $username = $user.name
                                            if ($UserDetails.ContainsKey($username)) {
                                                $UserDetails[$username].groups += ', ' + $GroupName
                                            } else {
                                                $UserDetails[$username] = @{
                                                    'username' = $user.name
                                                    'displayName' = $user.displayName
                                                    'emailAddress' = $user.emailAddress
                                                    'groups' = $GroupName
                                                }
                                            }
                                        }
                                    }
                                }
                            } catch {
                                return 1
                            }
                            $startAt += $maxResults
                        } while ($response.values.Count -eq $maxResults)
                    }

                    $UserList = $UserDetails.Values | ForEach-Object { [PSCustomObject]$_ }

                    if ($UserList.Count -eq 0) {
                        $UserList = @([PSCustomObject]@{
                            'username' = 'N/A'
                            'displayName' = 'N/A'
                            'emailAddress' = 'N/A'
                            'groups' = 'N/A'
                        })
                    }

                    $all = $all -eq '1'
                    $usernameCheckBox = $usernameCheckBox -eq '1'
                    $displayNameCheckBox = $displayNameCheckBox -eq '1'
                    $emailAddressCheckBox = $emailAddressCheckBox -eq '1'
                    $groupsCheckBox = $groupsCheckBox -eq '1'

                    $columns = @()
                    if ($all -or $usernameCheckBox) { $columns += 'username' }
                    if ($all -or $displayNameCheckBox) { $columns += 'displayName' }
                    if ($all -or $emailAddressCheckBox) { $columns += 'emailAddress' }
                    if ($all -or $groupsCheckBox) { $columns += 'groups' }

                    $folderPath = 'jira_users'
                    if (-not (Test-Path $folderPath)) {
                        New-Item -ItemType Directory -Path $folderPath -ErrorAction Stop | Out-Null
                    }

                    $timestamp = Get-Date -Format 'yyyy_MMdd_HHmmss'

                    $OutputFile = $folderPath + '\' + $UserStatus + '_Jira_User_List_' + $timestamp + '.csv'
                    $UserList | Select-Object $columns | Export-Csv -Path $OutputFile -NoTypeInformation -Encoding UTF8

                    return $OutputFile
                    ";

            // Show loading
            GenerationStatusTextBlock.Text = "";
            GenerationStatusTextBlock.Visibility = Visibility.Hidden;
            GenerateProgressTextBox.Visibility = Visibility.Visible;
            LoadingImageGif.Visibility = Visibility.Visible;

            string result = await Task.Run(() =>
            {
                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.AddScript(script);
                    // pass values from our settings to the PowerShell script
                    powerShell.AddParameter("JiraURL", jiraURL);
                    powerShell.AddParameter("JiraToken", jiraToken);

                    // status radiobuttons
                    powerShell.AddParameter("UserStatus", userStatus);

                    // checklist options
                    powerShell.AddParameter("all", allSelected);
                    powerShell.AddParameter("usernameCheckBox", usernameSelected);
                    powerShell.AddParameter("displayNameCheckBox", displayNameSelected);
                    powerShell.AddParameter("emailAddressCheckBox", emailSelected);
                    powerShell.AddParameter("groupsCheckBox", groupsSelected);

                    powerShell.AddParameter("JiraGroupsString", selectedGroupsString);

                    var results = powerShell.Invoke<string>();
                    if (results.Count > 0)
                    {
                        return results[0];
                    }
                    return null;
                }
            });

            // Hide the loading
            GenerateProgressTextBox.Visibility = Visibility.Hidden;
            LoadingImageGif.Visibility = Visibility.Hidden;

            // Show GenerationStatusTextBlock
            GenerationStatusTextBlock.Visibility = Visibility.Visible;
            GenerationStatusTextBlock.Inlines.Clear();

            if (result != "1" && !string.IsNullOrEmpty(result))
            {
                string exeLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string fullPath = System.IO.Path.Combine(exeLocation, result);

                GenerationStatusTextBlock.Inlines.Add($"The CSV file containing {userStatus} Jira users has been generated!\n\n");
                GenerationStatusTextBlock.Inlines.Add("File is located at: ");
                Hyperlink link = new Hyperlink();
                link.Inlines.Add(fullPath);
                link.NavigateUri = new Uri(fullPath);
                link.RequestNavigate += (senderLink, eLink) => { Process.Start("explorer.exe", $"/select,\"{eLink.Uri.LocalPath}\""); eLink.Handled = true; };

                // Set the hover color of the hyperlink to its original color
                Style linkStyle = new Style(typeof(Hyperlink));
                Trigger mouseOverTrigger = new Trigger { Property = Hyperlink.IsMouseOverProperty, Value = true };
                mouseOverTrigger.Setters.Add(new Setter(Hyperlink.ForegroundProperty, link.Foreground));
                linkStyle.Triggers.Add(mouseOverTrigger);
                link.Style = linkStyle;

                GenerationStatusTextBlock.Inlines.Add(link);
                GenerationStatusTextBlock.Foreground = (Brush)new BrushConverter().ConvertFromString("#d6e2fb");
            }
            else
            {
                GenerationStatusTextBlock.Inlines.Add("Could not retrieve Jira users\n\n" +
                                                      "  1. Verify your VPN connection.\n" +
                                                      "  2. Double-check the correctness of your Jira URL or token in the Settings.");
                GenerationStatusTextBlock.Foreground = new SolidColorBrush(Colors.LightCoral);
            }
        }
    }
}
