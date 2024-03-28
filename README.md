# JiraCSV Genie

JiraCSV Genie is a WPF (Windows Presentation Foundation) application designed to facilitate the generation of CSV files using PowerShell scripts for various Jira-related tasks. It provides a user-friendly interface to execute PowerShell scripts, with inputs passed from the UI to the scripts. The generated data is organized in subfolders within the executable's running location for easy access.

## Features

### 1) Get Jira Users tab

This feature allows you to generate a CSV file containing the list of Jira users and their information based on specified criteria. Key functionalities include:

- **Data Selection**: Choose which user data to retrieve (Username, Display Name, Email, Groups, or All).
- **User Status**: Select whether to retrieve data for all users, active users only, or inactive users only.
- **Group Filtering**: Filter users by specific Jira groups or retrieve users from all groups.
- **Search & Multiselection Functionality**: Search for desired groups to retrieve users from.
- **Refresh Option**: Update the list of Jira groups.

### 2) Jira User Permissions

This feature enables the generation of a CSV file containing detailed information about a user's permissions in Jira. It includes:

- **User Verification**: Verify the validity of a Jira username before generating permissions.
- **Group Membership**: Lists the Jira groups the specified user is a member of.
- **Project Access**: Provides access details to Jira projects via group membership or individual role assignment, checks each project permission scheme for indivdual or group access & specifies type of access.

### 3) Jira Group Permissions

This feature allows you to generate a CSV file containing project access information for a selected Jira group. It includes:

- **Group Selection**: Choose the Jira group to generate access for.
- **Search Functionality**: Search for the desired group.
- **Refresh Option**: Update the list of Jira groups.
- **Permission Details**: Specifies the permissions or roles the group has in each project.

## Getting Started

To start using JiraCSV Genie, follow these steps:

1. **Download JiraCSV Genie**: Download the 'JiraCSV Genie.exe.p7m' file from the GitHub repository: 

    > Note: The .p7m file extension indicates that the file is encrypted and digitally signed using the PKCS#7 format. It is commonly used for secure email transmission and digital signatures. In the context of JiraCSV Genie, this file format is used to ensure the authenticity and integrity of the application which allows you to download the app without it getting blocked by Windows.

2. **Run the Executable**: Double-click the downloaded 'JiraCSV Genie.exe.p7m' file. The Entrust Entelligence Security Provider will prompt you with a security warning asking if you want to run the file. Click "Yes" to launch the application.

3. **Update Jira URL and Access Token**: Navigate to the settings menu within the application and update the Jira URL and Jira Access Token. Ensure that the access token has admin privileges to execute scripts effectively.
   
4. **Save Settings**: Save the updated values in the settings menu. These values will be saved and stored locally in the app's configuration file (app.config) for future use. You can safely share the application without worrying about exposing your Personal Access Token to others.

5. **Run Scripts**: Choose the desired feature from the application's UI and provide necessary inputs. Click on the appropriate buttons to execute the scripts and generate CSV files.

6. **Access Generated Data**: when a CSV file is generated the app displays a hyperlink that opens the directory of the newly generated CSV file and points to it when clicked.

## Additional Resources

- For a guide on how to generate a Jira Personal Access Token (PAT), visit [this official Confluence documentation link](https://confluence.atlassian.com/enterprise/using-personal-access-tokens-1026032365.html).

## HC Jira Instances

- **Jira Production**: [https://jill.hc-sc.gc.ca/jira](https://jill.hc-sc.gc.ca/jira)
- **Jira Staging**: [https://jill-stg.jack.hc-sc.gc.ca/jira](https://jill-stg.jack.hc-sc.gc.ca/jira)


## Note
> Different Personal Access Tokens (PATs) are required for different Jira instances. Ensure to use a separate PAT for each instance and update the settings accordingly.

> Ensure that the Jira Personal Access Token (PAT) used has admin privileges to execute the scripts effectively. PATs without admin privileges may result in missing data in the generated CSV files, depending on the user's access rights.

> The application provides feedback on the validity of the Jira PAT used, indicating whether it has admin privileges, lacks admin privileges, or is invalid.

## Support

For any assistance or inquiries, please contact [khalid.kanaan.ca@gmail.com](mailto:khalid.kanaan.ca@gmail.com).
