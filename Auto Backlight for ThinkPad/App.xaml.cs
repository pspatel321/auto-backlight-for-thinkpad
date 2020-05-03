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
using System.Windows.Input;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Main control flow of the application
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Access to Registry key for this application
        /// </summary>
        public static RegistryKey RegistryKey = Registry.CurrentUser.CreateSubKey(@"Software\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product);

        /// <summary>
        /// Main application
        /// </summary>
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
            var gh = c.Children.OfType<GlobalHotKey>().First();
            _autoKeyboard = new AutoKeyboardBacklightController(pm, ri);
            _autoScreen = new AutoScreenBrightnessController(gh);
            _autoScreen.BrightnessChanged += (se, ev) =>
            {
                _lastPoint = new DataPoint(ev.Intensity, ev.Brightness);
            };
            _autoKeyboard.Activity += (se, ev) =>
            {
                // Record changes in OnLevel, used next time application runs
                if (ev.Act == AutoKeyboardBacklightController.ActivityEventArgs.Activity.OnLevelChanged)
                {
                    _settings.Keyboard_OnLevel = _autoKeyboard.OnLevel;
                }
                // Refresh screen brightness on user activity or power resume events from keyboard
                if ((ev.Act == AutoKeyboardBacklightController.ActivityEventArgs.Activity.UserActivity) ||
                    (ev.Act == AutoKeyboardBacklightController.ActivityEventArgs.Activity.PowerResume))
                {
                    if (_autoScreen.Enabled) _ = _autoScreen.Retrigger();
                }
            };

            // Triggers loading of the icon & menu in tray
            FindResource("SysTrayMenu");
            FindResource("SysTrayIcon");
        }

        private Settings _settings;
        private AutoKeyboardBacklightController _autoKeyboard;
        private AutoScreenBrightnessController _autoScreen;
        private object _lastPoint;

        // Setup on app startup
        private void _Starting(object sender, StartupEventArgs e)
        {
            // Try to get settings, if fail save a fresh copy
            if (!_settings.RestoreFromRegistry(RegistryKey))
            {
                _settings.SaveToRegistry(RegistryKey);
                RegistryKey.Flush();
            }
            _settings.PropertyChanged += _Settings_PropertyChanged;
            _settings.Update();
        }
        // Connect changes in settings (from gui bindings in xaml) to controllers
        private void _Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_settings.Keyboard_Enabled))
                _autoKeyboard.Enabled = _settings.Keyboard_Enabled;

            if (e.PropertyName == nameof(_settings.Keyboard_InactivityTimeout))
                _autoKeyboard.InactivityTimeout = _settings.Keyboard_InactivityTimeout;

            if (e.PropertyName == nameof(_settings.Keyboard_OnLevel))
                _autoKeyboard.OnLevel = _settings.Keyboard_OnLevel;

            if (e.PropertyName == nameof(_settings.Screen_Enabled))
                _autoScreen.Enabled = _settings.Screen_Enabled;

            if (e.PropertyName == nameof(_settings.Screen_PollPeriod))
                _autoScreen.PollPeriod = _settings.Screen_PollPeriod;

            if (e.PropertyName == nameof(_settings.Screen_HotKeyEnabled))
                _autoScreen.IsHotKeyEnabled = _settings.Screen_HotKeyEnabled;

            if (e.PropertyName == nameof(_settings.Screen_HotKey))
                _autoScreen.HotKey = HotKey.Parse(_settings.Screen_HotKey);

            if (e.PropertyName == nameof(_settings.Screen_LearnedPoints))
                _autoScreen.LearnedPoints = _settings.Screen_LearnedPoints.Select(p => new DataPoint(p.Item1, p.Item2)).ToList();

            if (e.PropertyName == nameof(_settings.Screen_Curvature))
                _autoScreen.Curvature = _settings.Screen_Curvature;

            _settings.SaveToRegistry(RegistryKey);
            RegistryKey.Flush();
        }
        // Stop and cleanup on app exit
        private void _Exiting(object sender, ExitEventArgs e)
        {
            _autoKeyboard.Stop();
            _autoScreen.Stop();

            _settings.SaveToRegistry(RegistryKey);
            RegistryKey.Flush();
        }
        // Show the screen brightness vs ambient light level calibration window
        private void _ShowCalibration(object sender, RoutedEventArgs e)
        {
            if (_calibration == null)
            {
                // Create new window from saved points if not already open
                _calibration = new Calibration(_settings.Screen_LearnedPoints.Select(p => new DataPoint(p.Item1, p.Item2)).ToList(), _settings.Screen_Curvature);
                if (_lastPoint != null) _calibration.CurrentPoint = (DataPoint)_lastPoint;
                _autoScreen.Stop();
                var tempScreen = new AutoScreenBrightnessController(null);
                tempScreen.BrightnessChanged += (sen, eve) =>
                {
                    _calibration.CurrentPoint = new DataPoint(eve.Intensity, eve.Brightness);
                };
                _calibration.Refresh += (sen, eve) =>
                {
                    tempScreen.LearnedPoints = eve.LearnedPoints;
                    tempScreen.Curvature = eve.Curvature;
                    _ = tempScreen.Retrigger();
                };
                _calibration.ManualScreenBrightness += (sen, eve) =>
                {
                    byte br = (byte)Math.Round(eve.ScreenBrightness * 100.0);
                    tempScreen.SetBrightnessSlider(br);
                    _lastPoint = new DataPoint(_calibration.CurrentPoint.X, eve.ScreenBrightness);
                    _calibration.CurrentPoint = (DataPoint)_lastPoint;
                };
                _calibration.Closed += (sen, eve) =>
                {
                    _calibration = null;
                    if (_settings.Screen_Enabled) 
                        _autoScreen.Start(false);
                };
                _calibration.Apply += (sen, eve) =>
                {
                    // Transfer points on Apply, refresh screen brightness
                    _settings.Screen_Curvature = _calibration.Curvature;
                    _settings.Screen_LearnedPoints.Clear();
                    _settings.Screen_LearnedPoints.AddRange(_calibration.LearnedPoints.Select(p => new Tuple<double, double>(p.X, p.Y)).ToList());
                    _settings.Screen_LearnedPoints = _settings.Screen_LearnedPoints;
                };
            }
            _calibration.Show();
        }
        // Show the application about window
        private void _ShowAbout(object sender, RoutedEventArgs e)
        {
            if (_about == null)
            {
                // Create new window if not already open
                _about = new About();
                _about.Closed += (sen, eve) => _about = null;
            }
            _about.Show();
        }
        private About _about;
        private Calibration _calibration;

        // Exit from menu performs clean shut down of application
        private void _ExitApplication(object sender, RoutedEventArgs e) => Shutdown();

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
                                Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
                            else
                                // Error
                                MessageBox.Show(string.Format("Could not download update for {0}.", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, ver), "Could not download", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #region Boxes

        // Show initial text
        private void _HotKeyBox_Init(object sender, EventArgs e)
        {
            TextBox tb = (TextBox)sender;
            tb.Text = _settings.Screen_HotKey.ToString();
        }
        // HotKey box, capture keyboard shortcut as it is pressed
        private void _HotKeyBox_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            e.Handled = true;
            var mod = Keyboard.Modifiers;
            var key = e.Key;

            // When Alt is pressed, SystemKey is used instead
            if (key == Key.System)
                key = e.SystemKey;

            // Pressing delete, backspace or escape without modifiers clears the current value
            if (mod == ModifierKeys.None &&
                (key == Key.Delete || key == Key.Back || key == Key.Escape))
            {
                tb.Clear();
                return;
            }
            // If no actual key was pressed yet
            if (key == Key.LeftCtrl ||
                key == Key.RightCtrl ||
                key == Key.LeftAlt ||
                key == Key.RightAlt ||
                key == Key.LeftShift ||
                key == Key.RightShift ||
                key == Key.LWin ||
                key == Key.RWin)
            {
                tb.Text = new HotKey(mod, Key.None).ToString();
                return;
            }
            tb.Text = new HotKey(mod, key).ToString();
        }
        // HotKey box, block the shortcut from triggering while user is editing
        private void _HotKeyBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            _autoScreen.IsHotKeyEnabled = false;
        }
        // HotKey box, save final shortcut after user is done editing, unblock shortcut
        private void _HotKeyBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            _settings.Screen_HotKey = tb.Text;
            _autoScreen.IsHotKeyEnabled = _settings.Screen_HotKeyEnabled;
        }

        private MenuItem _scm;
        private MenuItem _kbm;
        private MenuItem _hkm;
        private void ScreenMenu_Init(object sender, EventArgs e) => _scm = (MenuItem)sender;
        private void ScreenCombo_Enter(object sender, MouseEventArgs e) => _scm.IsCheckable = false;
        private void ScreenCombo_Leave(object sender, MouseEventArgs e) => _scm.IsCheckable = true;
        private void KeyboardMenu_Init(object sender, EventArgs e) => _kbm = (MenuItem)sender;
        private void KeyboardCombo_Enter(object sender, MouseEventArgs e) => _kbm.IsCheckable = false;
        private void KeyboardCombo_Leave(object sender, MouseEventArgs e) => _kbm.IsCheckable = true;
        private void HotkeyMenu_Init(object sender, EventArgs e) => _hkm = (MenuItem)sender;
        private void HotkeyBox_Enter(object sender, MouseEventArgs e) => _hkm.IsCheckable = false;
        private void HotkeyBox_Leave(object sender, MouseEventArgs e) => _hkm.IsCheckable = true;

        #endregion Boxes
    }
    /// <summary>
    /// Round data-point coordinate for display, range 0 to 100, no decimals
    /// </summary>
    public class Rounder : IValueConverter
    {
        /// <summary>
        /// From double to string
        /// </summary>
        /// <param name="value">input double</param>
        /// <param name="targetType">double</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns></returns>
        public object Convert(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(double) && targetType == typeof(string))
            {
                if (value == null) return "";
                double val = (double)value;
                int rounded = (int)Math.Round(val);
                return rounded.ToString();
            }
            throw new ArgumentException();
        }
        /// <summary>
        /// From user string to double
        /// </summary>
        /// <param name="value">string</param>
        /// <param name="targetType">double</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>double</returns>
        public object ConvertBack(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(string) && targetType == typeof(double))
            {
                if (value == null) return double.NaN;
                string str = (string)value;
                double dbl;
                if (double.TryParse(str, out dbl))
                    if (dbl >= 0 && dbl <= 100) return dbl;
                return double.NaN;
            }
            throw new ArgumentException();
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
    /// <summary>
    /// Format brightness level (0/1/2) into friendly text
    /// </summary>
    public class BrightnessLevelFormatter : IValueConverter
    {
        /// <summary>
        /// Convert level to friendly display string
        /// </summary>
        /// <param name="value">0/1/2</param>
        /// <param name="targetType">string</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>friendly display string</returns>
        public object Convert(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(int) && targetType == typeof(string))
            {
                if (value == null) return "";
                int val = (int)value;
                if (val == 0) return "Off";
                if (val == 1) return "Low";
                if (val == 2) return "Full";
            }
            throw new ArgumentException();
        }
        /// <summary>
        /// Parse string into level
        /// </summary>
        /// <param name="value">string</param>
        /// <param name="targetType">int</param>
        /// <param name="parameter">unused</param>
        /// <param name="culture">unused</param>
        /// <returns>level 0/1/2</returns>
        public object ConvertBack(object value, Type targetType, object parameter = null, CultureInfo culture = null)
        {
            if (value.GetType() == typeof(string) && targetType == typeof(int))
            {
                if (value == null) return -1;
                string str = (string)value;
                if (str == "Off") return 0;
                if (str == "Low") return 1;
                if (str == "Full") return 2;
            }
            throw new ArgumentException();
        }
    }
}