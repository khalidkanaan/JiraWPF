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

namespace JiraWPF.MVVM.View
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class Settings : UserControl
    {
        public Settings()
        {
            InitializeComponent();
            JiraURLTextBox.Text = Properties.Settings.Default.JiraURL;
            JiraAccessTokenTextBox.Text = Properties.Settings.Default.JiraAccessToken;
        }

        private void SaveJiraURLButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.JiraURL = JiraURLTextBox.Text;
            Properties.Settings.Default.Save();
        }

        private void SaveJiraAccessTokenButton_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.JiraAccessToken = JiraAccessTokenTextBox.Text;
            Properties.Settings.Default.Save();
        }
    }
}
