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

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Documents;

namespace Auto_Keyboard_Backlight
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        /// <summary>
        /// Load informational strings and connect the buttons
        /// </summary>
        public About()
        {
            InitializeComponent();

            // Load strings
            Header.Content = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;
            Subheader.Content = "Version: " + Assembly.GetExecutingAssembly().GetName().Version + " Config: " + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>().Configuration;
            Body.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description;
            Footer.Content = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>().Copyright;

            // Load License.rtf document
            LicenseHeader.Content = "This software is licensed under the Apache License Version 2.0:";
            TextRange content = new TextRange(License.ContentStart, License.ContentEnd);
            string file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "License.rtf");
            if (content.CanLoad(DataFormats.Rtf))
                content.Load(File.OpenRead(file), DataFormats.Rtf);
            StartAutoBox.IsChecked = 0 == Installer.DoCmd(Environment.GetEnvironmentVariable("ComSpec"), string.Format("/c schtasks /Query /TN {0} | findstr /C:Running /C:Ready", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""));

            // Connect actions to buttons
            VisitButton.Click += (sender, e) =>
            {
                // Open url in web browser
                var uri = new Uri(Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().First((i) => i.Key == "WebUrl").Value); ;
                if (uri.AbsoluteUri != "")
                {
                    var proc = new Process();
                    proc.StartInfo.FileName = uri.AbsoluteUri;
                    proc.Start();
                }
            };
            StartAutoBox.Click += (sender, e) =>
            {
                try
                {
                    if (StartAutoBox.IsChecked == true) Installer.DoCmd("schtasks", string.Format("/Change /TN {0} /ENABLE", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""), true);
                    else Installer.DoCmd("schtasks", string.Format("/Change /TN {0} /Disable", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""), true);
                }
                catch (Exception)
                {
                }
                StartAutoBox.IsChecked = 0 == Installer.DoCmd(Environment.GetEnvironmentVariable("ComSpec"), string.Format("/c schtasks /Query /TN {0} | findstr /C:Running /C:Ready", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""));
            };
            CloseButton.Click += (sender, e) => Close();
            UpdateButton.Click += (sender, e) => _OnlineUpdateProcedure();
        }
        // Update procedure, verbose version showing more user message boxes along the way (unlike the quiet version in App.xaml.cs)
        private async void _OnlineUpdateProcedure()
        {
            using (OnlineUpdater ou = new OnlineUpdater())
            {
                var ver = Assembly.GetExecutingAssembly().GetName().Version;
                var update = await ou.GetLatestVersion();

                // Update is available, prompt user
                if (update != null)
                    if (update.Version > ver)
                        // Yes/no download update
                        if (MessageBoxResult.Yes == MessageBox.Show(string.Format("An update is available for {0}.\n\nThe latest version is: {1}.\nYour version is: {2}.\n\nWould you like to download the update?", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, update.Version, ver), "Update is available", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                            if (await ou.DownloadVersion(update))
                                // Installer started, must quit this instance
                                Dispatcher.Invoke(() => Application.Current.Shutdown());
                            else
                                // Error
                                MessageBox.Show(string.Format("Could not download update for {0}.", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, ver), "Could not download", MessageBoxButton.OK, MessageBoxImage.Information);
                        else
                            // Not available
                            MessageBox.Show(string.Format("An update is not available for {0}.\n\nYou have the latest version: {1}.", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, ver), "Update is not available", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        // Error
                        MessageBox.Show(string.Format("Could not check for updates for {0}.", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product, ver), "Could not check", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Clicked");
        }
    }
}
