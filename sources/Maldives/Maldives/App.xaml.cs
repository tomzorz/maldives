using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Maldives
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private string _deploy;

        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            // debug
            var args = e.Args;
            //var args = new[] {@"testbuild.mpk"};
            // end debug

            if (args.Length != 1)
            {
                DisplayError("Invalid argument count!");
                return;
            }

            if (!args[0].EndsWith(".mpk"))
            {
                DisplayError("You can only deploy .mpk files!");
                return;
            }

            _deploy = args[0];

            var tools = EnsureTools();

            if (!tools)
            {
                DisplayError($"Missing Magic Leap SDK mldb tool at path {GetMldbPath()}!");
                return;
            }

            var devices = await EnumerateDevices();

            switch (devices.Count)
            {
                case 0:
                    DisplayError("No Magic Leap devices found.");
                    return;
                case 1:
                    await DeployToDevice(devices[0]);
                    return;
                default:
                    DisplayError("Please only connect one device at a time!");
                    return;
            }
        }

        private void DisplayError(string message)
        {
            var w = new ErrorWindow(message);
            w.Show();
        }

        private async Task DeployToDevice(string s)
        {
            var messages = new ObservableCollection<string>();
            var lw = new LogWindow(messages);
            lw.Show();
            await RunMldb($"install -u \"{_deploy}\"", s1 => { Dispatcher.Invoke(() => { messages.Insert(0, s1); }); });
        }

        private async Task<List<string>> EnumerateDevices()
        {
            var l = new List<string>();
            await RunMldb("devices", s =>
            {
                if(string.IsNullOrWhiteSpace(s) || !s.EndsWith("device")) return;
                l.Add(s.Split('\t').First(x => !string.IsNullOrWhiteSpace(x)));
            });
            return l;
        }

        private bool EnsureTools()
        {
            var path = GetMldbPath();
            return File.Exists(path);
        }

        private string GetMldbPath() => $@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\MagicLeap\mlsdk\v0.22.0\tools\mldb\mldb.exe";

        private Task RunMldb(string args, Action<string> output)
        {
            var psi = new ProcessStartInfo(GetMldbPath(),args)
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = GetMldbPath().Replace("mldb.exe", ""),
                UseShellExecute = false
            };
            var p = new Process()
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };
            p.ErrorDataReceived += (sender, eventArgs) => output(eventArgs.Data);
            p.OutputDataReceived += (sender, eventArgs) => output(eventArgs.Data);
            var tcs = new TaskCompletionSource<object>();
            p.Exited += (sender, eventArgs) => { tcs.SetResult(new object()); };
            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            return tcs.Task;
        }
    }
}
