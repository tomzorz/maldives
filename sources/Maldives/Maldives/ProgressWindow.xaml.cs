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
using System.Windows.Shapes;
using System.Windows.Shell;
using System.Windows.Threading;
using Humanizer;
using Humanizer.Localisation;

namespace Maldives
{
    /// <summary>
    /// Interaction logic for ProgressWindow.xaml
    /// </summary>
    public partial class ProgressWindow : Window
    {
        private readonly Action _cancel;

        private bool _signaled;
        private readonly long _started;

        private int _closeCountDown = 5;

        public ProgressWindow(string filePath, string deviceId, Action cancel)
        {
            InitializeComponent();

            _cancel = cancel;

            TaskbarItemInfo = new TaskbarItemInfo
            {
                ProgressState = TaskbarItemProgressState.Normal,
                ProgressValue = 0.0
            };

            var pathParts = filePath.Split('\\');
            FileName.Text = pathParts[pathParts.Length - 1];
            FromName.Text = pathParts[pathParts.Length - 2];
            DeviceName.Text = deviceId;

            _started = DateTime.Now.Ticks;
        }

        public void SignalDone()
        {
            Signal();
            CompletePercent.Text = "Deployment done";
            ProgressBar.Value = 100;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
            TaskbarItemInfo.ProgressValue = 1.0;

            CloseCountdown.Text = $"Auto-closing window in {_closeCountDown} seconds";
            var dt = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1.0)
            };
            dt.Tick += (sender, args) =>
            {
                _closeCountDown -= 1;
                CloseCountdown.Text = $"Auto-closing window in {_closeCountDown} second{(_closeCountDown > 1 ? "s" : "")}";
                if (_closeCountDown != 0) return;
                Close();
            };
            dt.Start();
        }

        public void SignalError()
        {
            Signal();
            ProgressBar.Foreground = new SolidColorBrush(Colors.Red);
            CompletePercent.Text = "Deployment failed";
            ProgressBar.Value = 100;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
        }

        private void Signal()
        {
            _signaled = true;
            Button.Content = "Close";
            Remaining.Text = "None";
        }

        public void UpdatePercent(int percent)
        {
            if(percent < 1 || percent > 99) return;

            CompletePercent.Text = $"{percent}% complete";

            ProgressBar.Value = percent;
            TaskbarItemInfo.ProgressValue = percent / 100.0;

            var now = DateTime.Now.Ticks;
            var diff = now - _started;
            var onePercent = diff / (double)percent;
            var remaining = TimeSpan.FromTicks(Convert.ToInt64((100 - percent) * onePercent));
            Remaining.Text = $"About {remaining.Humanize(minUnit:TimeUnit.Second)}";
        }

        private void ButtonHandler(object sender, RoutedEventArgs e)
        {
            if (!_signaled) _cancel();
            Close();
        }
    }
}
