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
                    FirstName = true;
                    LastName = true;
                    Email = true;
                }
                else
                {
                    Username = true;
                    FirstName = false;
                    LastName = false;
                    Email = true;
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

        private bool _firstName;
        public bool FirstName
        {
            get { return _firstName; }
            set
            {
                _firstName = value;
                OnPropertyChanged("FirstName");
            }
        }

        private bool _lastName;
        public bool LastName
        {
            get { return _lastName; }
            set
            {
                _lastName = value;
                OnPropertyChanged("LastName");
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