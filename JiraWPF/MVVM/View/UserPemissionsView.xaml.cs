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
    /// Interaction logic for UserPemissionsView.xaml
    /// </summary>
    public partial class UserPemissionsView : UserControl
    {
        public UserPemissionsView()
        {
            InitializeComponent();
        }

        private void JiraUsernameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckJiraUsernameButton.IsEnabled = !string.IsNullOrWhiteSpace(JiraUsernameTextBox.Text);
            GeneratePermissionsButton.IsEnabled = false;
        }

        private void JiraUsernameTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // If the TextBox is empty, do nothing
                if (string.IsNullOrWhiteSpace(JiraUsernameTextBox.Text))
                {
                    return;
                }
                CheckJiraUsernameButton_Click(sender, e);
            }
        }

        private async void CheckJiraUsernameButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            // Get value of username from textbox
            string targetUsername = JiraUsernameTextBox.Text;

            // Call PowerShell script
            string script = @"
                    param($JiraURL, $JiraToken, $targetUsername)
                    $API = '/rest/api/2'
                    $Header = @{
                        Authorization = 'Bearer ' + $JiraToken
                    }

                    # Define the base API URL
                    $baseApiUrl = $JiraURL + $API

                    # Check if the User exists. Exits the script if the user is invalid
                    try{
                        $CheckUserResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/user?username=' + $targetUsername) -Method Get -Headers $Header
                        $userKey = $CheckUserResponse.key
                        return 1; // User exists
                    }catch{
                        # $_ represents the current exception object in PowerShell
                        if ($_.Exception.Response.StatusCode.Value__ -eq 404){
                            return 2; // User does not exist
                        }else{
                            return 3; // Error sending request for user
                        }
                    }
                    ";

            // Show loading
            UsernameStatusTextBox.Text = "";
            GenerationStatusTextBlock.Text = "";
            UsernameStatusTextBox.Visibility = Visibility.Hidden;
            CheckUsernameTextBox.Visibility = Visibility.Visible;
            LoadingImageGif.Visibility = Visibility.Visible;

            string result = await Task.Run(() =>
            {
                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.AddScript(script);
                    // Passes the values from our settings to the PowerShell script
                    powerShell.AddParameter("JiraURL", jiraURL);
                    powerShell.AddParameter("JiraToken", jiraToken);
                    powerShell.AddParameter("targetUsername", targetUsername);

                    var results = powerShell.Invoke<string>();
                    if (results.Count > 0)
                    {
                        return results[0];
                    }
                    return null;
                }
            });

            // Hide the loading
            CheckUsernameTextBox.Visibility = Visibility.Hidden;
            LoadingImageGif.Visibility = Visibility.Hidden;

            if (result != null)
            {
                UsernameStatusTextBox.Visibility = Visibility.Visible;
                if (result == "1")
                {
                    UsernameStatusTextBox.Text = $"Username '{targetUsername}' is valid ✓";
                    UsernameStatusTextBox.Foreground = (Brush)new BrushConverter().ConvertFromString("#d6e2fb");
                    GeneratePermissionsButton.IsEnabled = true;
                }
                else if (result == "2")
                {
                    UsernameStatusTextBox.Text = $"Username '{targetUsername}' is invalid ✘";
                    UsernameStatusTextBox.Foreground = new SolidColorBrush(Colors.LightCoral);
                    GeneratePermissionsButton.IsEnabled = false;
                }
                else
                {
                    UsernameStatusTextBox.Text = "Error sending request to Jira ✘\n\n" +
                                                 "Check your VPN connection and try again!";
                    UsernameStatusTextBox.Foreground = new SolidColorBrush(Colors.LightCoral);
                    GeneratePermissionsButton.IsEnabled = false;
                }
            }

        }

        private async void GeneratePermissionsButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the values from settings
            string jiraURL = Properties.Settings.Default.JiraURL;
            string jiraToken = Properties.Settings.Default.JiraAccessToken;

            // Get value of username from textbox
            string targetUsername = JiraUsernameTextBox.Text;

            // Call PowerShell script
            string script = @"
                    param($JiraURL, $JiraToken, $targetUsername)
                    $API = '/rest/api/2'
                    $Header = @{
                        Authorization = 'Bearer ' + $JiraToken
                    }

                    # Define the base API URL
                    $baseApiUrl = $JiraURL + $API

                    $CheckUserResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/user?username=' + $targetUsername) -Method Get -Headers $Header
                    $userKey = $CheckUserResponse.key

                    # Get the list of projects
                    $ProjectsResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/project') -Headers $Header
                    # Convert the response to a JSON object
                    $Projects = $ProjectsResponse | Select-Object -ExpandProperty key


                    # Array to store project names where user has a role or group access
                    $userProjects = @{}

                    # Function to add a unique project to the array
                    function AddOrUpdateUserProjects($key, $value) {
                        if ($userProjects.ContainsKey($key)) {
                            $existingValue = $userProjects[$key]
                            if ($existingValue -is [array]) {
                                $userProjects[$key] += $value
                            } else {
                                $userProjects[$key] = @($existingValue, $value)
                            }
                        } else {
                            $userProjects[$key] = @($value)
                        }
                    }

                    # Get the list of groups for the user
                    $groupsResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/user?username=' + $targetUsername +'&expand=groups') -Method Get -Headers $Header
                    # Array to store the group names that the user is associated with
                    $userGroups = @()

                    # Check if the target user is in any group
                    if ($groupsResponse.groups.items.Count -gt 0) {
                        $userGroups += $groupsResponse.groups.items.name
                    }

                    foreach ($projectKey in $Projects) {
                        # Get the list of roles for the project
                        $rolesResponse = Invoke-RestMethod -Uri ($baseApiUrl + '/project/' + $projectKey + '/role') -Method Get -Headers $Header

                        # For each role, check if the target user or any of the user's groups are in that role
                        foreach ($role in $rolesResponse.PSObject.Properties) {
                            # Get the actors in the current role
                            $actorsInRole = Invoke-RestMethod -Uri $role.Value -Method Get -Headers $Header

                            # Check if the target user or any of the user's groups are in the list of actors for this role
                            foreach ($actor in $actorsInRole.actors) {
                                if ($actor.name -eq $targetUsername) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Single Role Actor: ' + $actor.name + '# ' + $role.name)
                                } 
                                if ($actor.type -eq 'atlassian-group-role-actor' -and $userGroups -contains $actor.name) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Group Role Actor: ' + $actor.name + '# ' + $role.name)
                                }
                            }
                        }
                    }


                    foreach ($projectKey in $Projects) {
                        $params = @{
                            'expand' = 'permissions'
                        }

                        # get permission scheme for the project
                        $pScheme = Invoke-RestMethod -Uri ($baseApiUrl + '/project/' + $projectKey + '/permissionscheme') -Body ($params) -Headers $Header

                        foreach ($permission in $pScheme.permissions) {
                            foreach ($holder in $permission.holder) {
                                if ($holder.type -eq 'user' -and $userKey -eq $holder.parameter) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Single Permission: ' + $targetUsername + '# ' + $permission.permission)
                                }
                                if ($holder.type -eq 'group' -and $userGroups -contains $holder.parameter) {
                                    AddOrUpdateUserProjects -key $projectKey -value ('Group Permission: ' + $holder.parameter + '# ' + $permission.permission)
                                }
                            }
                        }
                    }

                    # Save the data to CSV file.
                    $data = @()

                    # targetUsername is deleted in this process.
                    $JiraUser = $targetUsername

                    # Add the group data
                    foreach ($group in $userGroups) {
                        # Create a new row
                        $row = New-Object PSObject
                        $row | Add-Member -MemberType NoteProperty -Name 'username' -Value $JiraUser
                        $row | Add-Member -MemberType NoteProperty -Name 'groups' -Value $group
                        $row | Add-Member -MemberType NoteProperty -Name 'projects' -Value ''
                        $row | Add-Member -MemberType NoteProperty -Name 'Permission/Role' -Value ''
                        $row | Add-Member -MemberType NoteProperty -Name 'granted by' -Value ''

                        # Add the row to the data array
                        $data += $row

                        # Clear the username for subsequent rows
                        $JiraUser = ''
                    }

                    # Add the project and access data
                    foreach ($project in $userProjects.Keys) {
                        foreach ($access in $userProjects[$project]) {
                            # Split the access value
                            $accessValues = $access -split '# '

                            # Create a new row
                            $row = New-Object PSObject
                            $row | Add-Member -MemberType NoteProperty -Name 'username' -Value ''
                            $row | Add-Member -MemberType NoteProperty -Name 'groups' -Value ''
                            $row | Add-Member -MemberType NoteProperty -Name 'projects' -Value $project
                            $row | Add-Member -MemberType NoteProperty -Name 'Permission/Role' -Value $accessValues[1]
                            $row | Add-Member -MemberType NoteProperty -Name 'granted by' -Value $accessValues[0]

                            # Add the row to the data array
                            $data += $row

                            $project = ''
                        }
                    }

                    $shift = $userGroups.count
                    for ($i = $shift; $i -lt $data.Count; $i++) {
                        $data[$i-$shift].projects = $data[$i].projects
                        $data[$i-$shift].'Permission/Role' = $data[$i].'Permission/Role'
                        $data[$i-$shift].'granted by' = $data[$i].'granted by'
                    }

                    # Remove the last $shift number of rows from the $data array
                    $data = $data[0..($data.Count-$shift-1)]

                    $folderPath = 'jira_user_access\' + $targetUsername
                    if (-not (Test-Path $folderPath)) {
                        New-Item -ItemType Directory -Path $folderPath -ErrorAction Stop | Out-Null
                    }

                    $timestamp = Get-Date -Format 'yyyy_MMdd_HHmmss'

                    # Construct the file path with timestamp
                    $OutputFile = $folderPath + '\' + $targetUsername + '_' + $timestamp + '_Jira_Access.csv'

                    # Export the data to a CSV file
                    $data | Export-Csv -Path $OutputFile -NoTypeInformation
                    return $OutputFile
                    ";

            // Show loading
            GenerationStatusTextBlock.Text = "";
            GenerationStatusTextBlock.Visibility = Visibility.Hidden;
            GenerateProgressTextBox.Visibility = Visibility.Visible;
            LoadingImageGif1.Visibility = Visibility.Visible;

            string result = await Task.Run(() =>
            {
                using (PowerShell powerShell = PowerShell.Create())
                {
                    powerShell.AddScript(script);
                    // Passes the values from our settings to the PowerShell script
                    powerShell.AddParameter("JiraURL", jiraURL);
                    powerShell.AddParameter("JiraToken", jiraToken);
                    powerShell.AddParameter("targetUsername", targetUsername);

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
            LoadingImageGif1.Visibility = Visibility.Hidden;

            if (result != null)
            {
                string exeLocation = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string fullPath = System.IO.Path.Combine(exeLocation, result);

                GenerationStatusTextBlock.Visibility = Visibility.Visible;
                GenerationStatusTextBlock.Inlines.Clear();
                GenerationStatusTextBlock.Inlines.Add($"The CSV file containing all the Jira projects user '{targetUsername}' has access to has been generated!\n\n");
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
                GeneratePermissionsButton.IsEnabled = true;
            }
        }
    }
}
