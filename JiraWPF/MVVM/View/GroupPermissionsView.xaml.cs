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
    /// Interaction logic for GroupPermissionsView.xaml
    /// </summary>
    public partial class GroupPermissionsView : UserControl
    {
        public GroupPermissionsView()
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
                // Convert StringCollection to List<string> for filtering
                List<string> jiraGroupsList = jiraGroups.Cast<string>().ToList();

                // Filter out the item with string value "None"
                List<string> filteredList = jiraGroupsList.Where(group => group != "None").ToList();

                // Populate the ListBox
                var collectionView = CollectionViewSource.GetDefaultView(filteredList);
                GroupListBox.ItemsSource = collectionView;

                // Set SelectedIndex to 0 if there are items left after filtering
                if (filteredList.Count > 0)
                {
                    GroupListBox.SelectedIndex = 0;
                }

                GroupFilterSearchTextBox.TextChanged += (s, _) =>
                {
                    // store currently selected item
                    var selectedItem = GroupListBox.SelectedItem as string;

                    // apply search filter
                    collectionView.Filter = item =>
                    {
                        // when TextBox is empty, show all items
                        if (string.IsNullOrEmpty(GroupFilterSearchTextBox.Text))
                            return true;

                        // Show only items that contain the text in the TextBox 
                        return ((string)item).ToLower().Contains(GroupFilterSearchTextBox.Text.ToLower());
                    };

                    // reselect the previously selected item if it still exists after the filter
                    if (collectionView.Contains(selectedItem))
                    {
                        GroupListBox.SelectedItem = selectedItem;
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
            $API = '/rest/api/2/groups/picker'

            $Header = @{
                Authorization = 'Bearer ' + $JiraToken
            }

            $uri = $JiraURL + $API
            try {
                $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $Header -ErrorAction Stop

                if ($response -ne $null) {
                    $groupNames = @()
                    foreach ($group in $response.groups) {
                        $groupNames += $group.name
                    }
                    $groupNamesString = $groupNames -join ','
                    return $groupNamesString
                }
            } catch {
                return 1 # Error occurred while retrieving groups
            }";

            // Show loading
            GroupRefreshStatusTextBlock.Text = "";
            RefreshFailedTextBlock.Visibility = Visibility.Hidden;
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

                    // Get how many Jira groups were added
                    int existingGroupCount = Properties.Settings.Default.JiraGroups.Count;
                    int numGroupsAdded = stringCollection.Count - existingGroupCount;

                    // store the StringCollection in settings.settings
                    Properties.Settings.Default.JiraGroups = stringCollection;
                    Properties.Settings.Default.Save();

                    // Populate the ListBox with the retrieved groups including "None" option
                    var collectionView = CollectionViewSource.GetDefaultView(stringCollection);
                    GroupListBox.ItemsSource = collectionView;

                    GroupRefreshStatusTextBlock.Foreground = new SolidColorBrush(Colors.DodgerBlue);
                    GroupRefreshStatusTextBlock.Inlines.Add($"Jira groups refreshed!\n{numGroupsAdded} new group(s) added.");

                    Window_Loaded(sender, e);
                }
                else
                {
                    RefreshFailedTextBlock.Visibility = Visibility.Visible;
                    GroupRefreshStatusTextBlock.Foreground = new SolidColorBrush(Colors.LightCoral);
                    GroupRefreshStatusTextBlock.Inlines.Add("1. Verify your VPN connection.\n" +
                                                            "2. Update Jira URL and Token in the Settings.");
                }
            }

        }

        private async void GetPermissionsForGroup_Click(object sender, RoutedEventArgs e)
        {
            // get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            string selectedGroup = (string)GroupListBox.SelectedItem;

            string script = @"
                    param($JiraURL, $JiraToken, $targetGroupName)

                    $Header = @{
                        Authorization = 'Bearer ' + $JiraToken
                    }

                    $baseApiUrl = $jiraUrl + '/rest/api/2'

                    try{
                        $CheckGroupResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/group?groupname=' + $targetGroupName) -Method Get -Headers $Header
                    }catch{
                        if ($_.Exception.Response.StatusCode.Value__ -eq 404){
                            return 1
                        }else{
                            return 2
                        }
                    }

                    $ProjectsResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/project') -Headers $Header
                    $Projects = $ProjectsResponse | Select-Object -ExpandProperty key

                    $groupProjects = @{}

                    function AddOrUpdateUserProjects($key, $value) {
                        if ($groupProjects.ContainsKey($key)) {
                            $existingValue = $groupProjects[$key]
                            if ($existingValue -is [array]) {
                                $groupProjects[$key] += $value
                            } else {
                                $groupProjects[$key] = @($existingValue, $value)
                            }
                        } else {
                            $groupProjects[$key] = @($value)
                        }
                    }

                    foreach ($projectKey in $Projects) {
                        $rolesResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/project/' + $projectKey + '/role') -Method Get -Headers $Header

                        foreach ($role in $rolesResponse.PSObject.Properties) {
                            $actorsInRole = Invoke-RestMethod -Uri $role.Value -Method Get -Headers $Header

                            foreach ($actor in $actorsInRole.actors) {
                                if ($actor.type -eq 'atlassian-group-role-actor' -and $actor.name -eq $targetGroupName) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Group Role Actor# ' + $role.name)
                                }
                            }
                        }
                    }


                    foreach ($projectKey in $Projects) {
                        $params = @{
                            'expand' = 'permissions'
                        }

                        $pScheme = Invoke-RestMethod -Uri ($baseApiUrl + '/project/' + $projectKey + '/permissionscheme') -Body ($params) -Headers $Header

                        foreach ($permission in $pScheme.permissions) {
                            foreach ($holder in $permission.holder) {
                                if ($holder.type -eq 'group' -and $holder.parameter -eq $targetGroupName) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Permission Scheme# ' + $permission.permission)
                                }
                            }
                        }
                    }

                    $data = @()

                    $row = New-Object PSObject
                    $row | Add-Member -MemberType NoteProperty -Name 'group' -Value $targetGroupName
                    $row | Add-Member -MemberType NoteProperty -Name 'projects' -Value ''
                    $row | Add-Member -MemberType NoteProperty -Name 'Permission/Role' -Value ''
                    $row | Add-Member -MemberType NoteProperty -Name 'granted by' -Value ''

                    $data += $row

                    if ($groupProjects.Count -eq 0) {
                        $row = New-Object PSObject
                        $row | Add-Member -MemberType NoteProperty -Name 'group' -Value ''
                        $row | Add-Member -MemberType NoteProperty -Name 'projects' -Value 'N/A'
                        $row | Add-Member -MemberType NoteProperty -Name 'Permission/Role' -Value 'N/A'
                        $row | Add-Member -MemberType NoteProperty -Name 'granted by' -Value 'N/A'
                        $data += $row
                    } else {
                        foreach ($project in $groupProjects.Keys) {
                            foreach ($access in $groupProjects[$project]) {
                                $accessValues = $access -split '# '

                                $row = New-Object PSObject
                                $row | Add-Member -MemberType NoteProperty -Name 'group' -Value ''
                                $row | Add-Member -MemberType NoteProperty -Name 'projects' -Value $project
                                $row | Add-Member -MemberType NoteProperty -Name 'Permission/Role' -Value $accessValues[1]
                                $row | Add-Member -MemberType NoteProperty -Name 'granted by' -Value $accessValues[0]

                                $data += $row

                                $project = ''
                            }
                        }
                    }

                    $shift = 1
                    for ($i = $shift; $i -lt $data.Count; $i++) {
                        $data[$i-$shift].projects = $data[$i].projects
                        $data[$i-$shift].'Permission/Role' = $data[$i].'Permission/Role'
                        $data[$i-$shift].'granted by' = $data[$i].'granted by'
                    }

                    $data = $data[0..($data.Count-$shift-1)]

                    $folderPath = 'generated_data\jira_group_access\' + $targetGroupName
                    if (-not (Test-Path $folderPath)) {
                        New-Item -ItemType Directory -Path $folderPath -ErrorAction Stop | Out-Null
                    }

                    $timestamp = Get-Date -Format 'yyyy_MMdd_HHmmss'
                    $OutputFile = $folderPath + '\' + 'Group_' + $targetGroupName + '_' + $timestamp + '_Jira_Access.csv'
                    $data | Export-Csv -Path $OutputFile -NoTypeInformation
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

                    // pass selected group
                    powerShell.AddParameter("targetGroupName", selectedGroup);

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

            if (result != "2" && !string.IsNullOrEmpty(result))
            {
                string exeLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string fullPath = System.IO.Path.Combine(exeLocation, result);

                GenerationStatusTextBlock.Inlines.Add($"The CSV file containing Jira project permissions for group '{selectedGroup}' has been generated!\n\n");
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
            } else {
                GenerationStatusTextBlock.Inlines.Add($"Could not retrieve projects for group '{selectedGroup}'\n\n" +
                                                      "  1. Verify your VPN connection.\n" +
                                                      "  2. Double-check the correctness of your Jira URL or token in the Settings.");
                GenerationStatusTextBlock.Foreground = new SolidColorBrush(Colors.LightCoral);
            }
        }
    }
}
