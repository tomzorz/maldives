using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private Action _killOrder;

        private async void App_OnStartup(object sender, StartupEventArgs e)
        {
            // debug
            var args = e.Args;
            //var args = new[] { @"D:\Projects_development\Temporary\PersistenceTest\PersistenceTest\App\persistenceTest.mpk"};
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
            MessageBox.Show(message, "maldives - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(0);
        }

        private async Task DeployToDevice(string s)
        {
            var pw = new ProgressWindow(_deploy, s, () =>
            {
                _killOrder?.Invoke();
            });
            pw.Show();
            await RunMldb($"install -u \"{_deploy}\"",
                s1 => {
                    if(string.IsNullOrWhiteSpace(s1)) return;
                    Dispatcher.Invoke(() =>
                    {
                        var st = s1.ToUpperInvariant();
                        if (st.Contains("SUCCESSFULLY INSTALLED") || st.Contains("SUCCESSFULLY UPDATED"))
                        {
                            pw.SignalDone();
                        } else if (st.Contains("FAILED") || st.Contains("ERROR"))
                        {
                            pw.SignalError();
                        } else if (st.Contains("%"))
                        {
                            var numberString = Regex.Match(st, @"\d+").Value;
                            pw.UpdatePercent(int.Parse(numberString));
                        }
                    });
                });
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
            return !string.IsNullOrWhiteSpace(path) && File.Exists(path);
        }

        private string GetMldbPath()
        {
            var sdks = Directory.EnumerateDirectories($@"{Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)}\MagicLeap\mlsdk").ToList();
            return !sdks.Any() ? null : $@"{sdks.Last()}\tools\mldb\mldb.exe";
        }

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
            p.Exited += (sender, eventArgs) =>
            {
                tcs.SetResult(new object());
                _killOrder = null;
            };
            p.Start();
            _killOrder = () => p.Kill();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            return tcs.Task;
        }
    }
}
