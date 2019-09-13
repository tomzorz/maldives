using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;

namespace Maldives
{
    /// <summary>
    /// Interaction logic for LogWindow.xaml
    /// </summary>
    public partial class LogWindow : MaldivesWindow
    {
        public LogWindow(ObservableCollection<string> logs)
        {
            InitializeComponent();
            logs.CollectionChanged += Logs_CollectionChanged;
            Items.ItemsSource = logs;
        }

        private void Logs_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems.Count <= 0 || !(e.NewItems[0] is string)) return;

            var item = e.NewItems[0].ToString();
            if (item.Contains("requires approval"))
            {
                Approval.Visibility = Visibility.Visible;
            } else if (item.Contains("Successfully installed"))
            {
                Items.Visibility = Visibility.Collapsed;
                Title.Foreground = new SolidColorBrush(Colors.DarkGreen);
                Title.Text = "Installation successful";
            }
            else if (item.Contains("Successfully updated"))
            {
                Items.Visibility = Visibility.Collapsed;
                Title.Foreground = new SolidColorBrush(Colors.DarkGreen);
                Title.Text = "Update successful";
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
