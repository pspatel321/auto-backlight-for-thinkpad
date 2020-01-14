/*
 * Copyright 2019 Parth Patel
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Auto_Keyboard_Backlight
{
    /// <summary>
    /// Main control flow of the application
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Static global, access to keyboard backlight hardware
        /// </summary>
        internal static Backlight Backlight = new Backlight();
        /// <summary>
        /// Static global, access to Registry for this application
        /// </summary>
        public static RegistryKey RegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product);

        public App()
        {
            // Check for updates in Release mode
#if !DEBUG
            _OnlineUpdateProcedure();
#endif
            InitializeComponent();
            
            // Hook startup / exit actions
            Startup += _Starting;
            Exit += _Exiting;
            SessionEnding += (sender, e) => _Exiting(null, null);

            // Initialize controller components
            _settings = (Settings)FindResource("Settings");
            var c = (Panel)((Window)FindResource("HiddenWindow")).Content;
            var pm = c.Children.OfType<PowerManagement.PowerManagement>().First();
            var ri = c.Children.OfType<RawInput.RawInput>().First();
            _timeoutMode = new Timeout(pm, ri);
            _timeoutMode.RestoreFromRegistry();

            // Triggers loading of the icon & menu in tray
            FindResource("SysTrayMenu");
            FindResource("SysTrayIcon");
        }

        private Settings _settings;
        private Timeout _timeoutMode;

        // Setup on app startup
        private void _Starting(object sender, StartupEventArgs e)
        {
            // Try to get settings, if fail save a fresh copy
            if (!_settings.RestoreFromRegistry())
                _settings.SaveToRegistry();
            _settings.PropertyChanged += _Settings_PropertyChanged;
            _settings.Update();

            Backlight.Start();
            _timeoutMode.Start();
        }
        // Connect changes in settings to main program operation
        private void _Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Timeout")
            {
                _timeoutMode.Seconds = _settings.Timeout;
            }
            _settings.SaveToRegistry();
        }
        // Stop and cleanup on app exit
        private void _Exiting(object sender, ExitEventArgs e)
        {
            _timeoutMode.Stop();
            Backlight.Stop();

            _timeoutMode.SaveToRegistry();
            _settings.SaveToRegistry();
        }
        // Show the about window
        private void _About(object sender, RoutedEventArgs e)
        {
            if (_about == null)
            {
                _about = new About();
                _about.Closed += (s, ev) => _about = null;
            }
            _about.Show();
        }
        private About _about;

        // Exit from menu
        private void _Exit(object sender, RoutedEventArgs e) => Shutdown();

        // Update procedure, quiet version showing fewer user message boxes (unlike verbose version in About.xaml.cs)
        private async void _OnlineUpdateProcedure()
        {
            using (OnlineUpdater ou = new OnlineUpdater())
            {
                // Check for updates, prompt only if update is found
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                var update = await ou.GetLatestVersion();

                if (update != null)
                    if (update.Version > ver)
                        if (MessageBoxResult.Yes == MessageBox.Show(string.Format("An update is available for {0}.\n\nThe latest version is: {1}.\nYour version is: {2}.\n\nWould you like to download the update?", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, update.Version, ver), "Update is available", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                            if (await ou.DownloadVersion(update))
                                // Installer started, must quit this instance
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            else
                                // Error
                                MessageBox.Show(string.Format("Could not download update for {0}.", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, ver), "Could not download", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    /// <summary>
    /// Format time in seconds as printable friendly string
    /// </summary>
    public class TimeFormatter : IValueConverter
    {
        /// <summary>
        /// Convert seconds to friendly display string
        /// </summary>
        /// <param name="value">time in seconds (double)</param>
        /// <param name="targetType">string</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>friendly display string, "" if error</returns>
        public object Convert(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(double) && targetType == typeof(string))
            {
                if (value == null) return "";
                double val = (double)value;
                if (val == double.PositiveInfinity) return "Never";

                var ts = TimeSpan.FromSeconds(val);
                string[] strs = { "", "", "" };
                strs[0] = ts.Hours > 0 ? ts.Hours.ToString() + " hr" : "";
                strs[1] = ts.Minutes > 0 ? ts.Minutes.ToString() + " min" : "";
                strs[2] = ts.Seconds > 0 ? ts.Seconds.ToString() + " sec" : "";
                return string.Join(" ", strs).Trim();
            }
            throw new ArgumentException();
        }
        /// <summary>
        /// Parse user string into seconds
        /// </summary>
        /// <param name="value">user string</param>
        /// <param name="targetType">double</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>time in seconds</returns>
        public object ConvertBack(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(string) && targetType == typeof(double))
            {
                if (value == null) return double.NaN;
                string str = (string)value;

                // Clean to only lowercase alphanumeric
                str = str.Trim().ToLower();
                str = Regex.Replace(str, @"[^a-z\d]+", " ");
                if (str == "never" || str.StartsWith("inf")) return double.PositiveInfinity;

                // Contains h,m,s labels
                if (str.Any(c => char.IsLetter(c)))
                {
                    var h = Regex.Match(str, @"(\d+\s*h[a-z]*)");
                    if (h.Success) str = str.Remove(h.Index, h.Length);
                    var m = Regex.Match(str, @"(\d+\s*m[a-z]*)");
                    if (m.Success) str = str.Remove(m.Index, m.Length);
                    var s = Regex.Match(str, @"(\d+\s*s[a-z]*)");
                    if (s.Success) str = str.Remove(s.Index, s.Length);
                    if (str.Trim().Length > 0) return double.PositiveInfinity;

                    TimeSpan ts = new TimeSpan();
                    string hs = new string(h.Value.TakeWhile(c => char.IsDigit(c)).ToArray());
                    string ms = new string(m.Value.TakeWhile(c => char.IsDigit(c)).ToArray());
                    string ss = new string(s.Value.TakeWhile(c => char.IsDigit(c)).ToArray());
                    if (hs.Length > 0) ts += TimeSpan.FromHours(int.Parse(hs));
                    if (ms.Length > 0) ts += TimeSpan.FromMinutes(int.Parse(ms));
                    if (ss.Length > 0) ts += TimeSpan.FromSeconds(int.Parse(ss));
                    if (ts.TotalSeconds < 1) return double.PositiveInfinity;
                    return ts.TotalSeconds;
                }
                // Contains no labels, only separators
                else
                {
                    var strs = str.Split(null).ToList();
                    int ins = 3 - strs.Count;
                    if (ins >= 3 || ins < 0) return double.PositiveInfinity;
                    for (int i = 0; i < ins; i++) strs.Insert(0, "");

                    TimeSpan ts = new TimeSpan();
                    string hs = strs[0];
                    string ms = strs[1];
                    string ss = strs[2];
                    if (hs.Length > 0) ts += TimeSpan.FromHours(int.Parse(hs));
                    if (ms.Length > 0) ts += TimeSpan.FromMinutes(int.Parse(ms));
                    if (ss.Length > 0) ts += TimeSpan.FromSeconds(int.Parse(ss));
                    if (ts.TotalSeconds < 1) return double.PositiveInfinity;
                    return ts.TotalSeconds;
                }
            }
            throw new ArgumentException();
        }
    }
}