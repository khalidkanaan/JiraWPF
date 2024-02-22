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
    /// Interaction logic for GetUsersView.xaml
    /// </summary>
    public partial class GetUsersView : UserControl
    {
        public GetUsersView()
        {
            InitializeComponent();

            List<string> items = new List<string> { "None", "Checklist Item 2", "Checklist Item 3", "Checklist Item 4", "Checklist Item 5" };

            // CollectionView for the ListBox
            var collectionView = CollectionViewSource.GetDefaultView(items);
            GroupListBox.ItemsSource = collectionView;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            GroupFilterSearchTextBox.TextChanged += (s, _) =>
            {
                // store currently selected items
                var selectedItems = new List<string>();
                foreach (var item in GroupListBox.SelectedItems)
                {
                    selectedItems.Add((string)item);
                }

                // apply search filter
                var collectionView = CollectionViewSource.GetDefaultView(GroupListBox.ItemsSource);
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
}
