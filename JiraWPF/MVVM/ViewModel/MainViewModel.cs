using JiraWPF.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace JiraWPF.MVVM.ViewModel
{
    class MainViewModel : ObservableObject
    {
        public RelayCommand HomeViewCommand { get; set; }
        public RelayCommand GetUsersViewCommand { get; set; }
        public RelayCommand UserPemissionsViewCommand { get; set; }
        public RelayCommand GroupPemissionsViewCommand { get; set; }
        public RelayCommand SettingsViewCommand { get; set; }

        public HomeViewModel HomeVM { get; set; }
        public GetUsersViewModel GetUsersVM { get; set; }
        public UserPermissionsViewModel UserPermissionsVM { get; set; }
        public GroupPermissionsViewModel GroupPermissionsVM { get; set; }
        public SettingsViewModel SettingsVM { get; set; }
        private object _currentView;

        public object CurrentView
        {
            get { return _currentView; }
            set 
            { 
                _currentView = value;
                OnPropertyChanged();
            }
        }

        private void ExecuteCommandBasedOnSelection(object selectedItem)
        {
            var listBoxItem = selectedItem as ListBoxItem;
            if (listBoxItem == null) return;
            var content = listBoxItem.Content.ToString();

            switch (content)
            {
                case "Home 🛖":
                    HomeViewCommand.Execute(null);
                    break;
                case "Get All Users":
                    GetUsersViewCommand.Execute(null);
                    break;
                case "Jira User Permissions":
                    UserPemissionsViewCommand.Execute(null);
                    break;
                case "Jira Group Permissions":
                    GroupPemissionsViewCommand.Execute(null);
                    break;
                default:
                    break;
            }
        }

        private object _selectedItem;
        public object SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
                ExecuteCommandBasedOnSelection(value);
            }
        }

        public MainViewModel()
        { 
            HomeVM = new HomeViewModel();
            GetUsersVM = new GetUsersViewModel();
            UserPermissionsVM = new UserPermissionsViewModel();
            GroupPermissionsVM = new GroupPermissionsViewModel();
            SettingsVM = new SettingsViewModel();

            CurrentView = HomeVM;

            HomeViewCommand = new RelayCommand(o =>
            {
                CurrentView = HomeVM;
            });

            UserPemissionsViewCommand = new RelayCommand(o =>
            {
                CurrentView = UserPermissionsVM;
            });

            GroupPemissionsViewCommand = new RelayCommand(o =>
            {
                CurrentView = GroupPermissionsVM;
            });

            GetUsersViewCommand = new RelayCommand(o =>
            {
                CurrentView = GetUsersVM;
            });

            SettingsViewCommand = new RelayCommand(o =>
            {
                CurrentView = SettingsVM;
            });
        }
    }
}
