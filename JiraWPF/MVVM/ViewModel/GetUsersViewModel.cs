using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JiraWPF.MVVM.ViewModel
{
    class GetUsersViewModel : INotifyPropertyChanged
    {
        public GetUsersViewModel()
        {
            Username = true;
            Email = true;
        }

        private bool _all;
        public bool All
        {
            get { return _all; }
            set
            {
                _all = value;
                OnPropertyChanged("All");
                if (_all)
                {
                    Username = true;
                    DisplayName = true;
                    Email = true;
                    Groups = true;
                }
                else
                {
                    Username = true;
                    DisplayName = false;
                    Email = true;
                    Groups = false;
                }
            }
        }

        private bool _username;
        public bool Username
        {
            get { return _username; }
            set
            {
                _username = value;
                OnPropertyChanged("Username");
            }
        }

        private bool _groups;
        public bool Groups
        {
            get { return _groups; }
            set
            {
                _groups = value;
                OnPropertyChanged("Groups");
            }
        }

        private bool _displayName;
        public bool DisplayName
        {
            get { return _displayName; }
            set
            {
                _displayName = value;
                OnPropertyChanged("DisplayName");
            }
        }

        private bool _email;
        public bool Email
        {
            get { return _email; }
            set
            {
                _email = value;
                OnPropertyChanged("Email");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}