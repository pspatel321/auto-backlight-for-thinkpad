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
using System.Reflection;
using System.Windows;

namespace Auto_Keyboard_Backlight
{
    internal class Program
    {
        /// <summary>
        /// Provide a custom entry point for the program, supersede WPF App built-in entry point
        /// </summary>
        [STAThread]
        public static void Main()
        {
            // Allow only a single instance of this application to run at once
            string proc = Process.GetCurrentProcess().ProcessName;
            if (Process.GetProcessesByName(proc).Length > 1)
            {
                MessageBox.Show(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + " is already running, only 1 instance allowed.", "Already running", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(0);
            }

            // Redirect loading of certain assemblies
            AppDomain.CurrentDomain.AssemblyResolve += (sender, e) =>
            {
                string resourceName = new AssemblyName(e.Name).Name;
                switch (resourceName)
                {
                    case "Hardcodet.Wpf.TaskbarNotification":
                        {
                            // Direct to internal resource
                            var res = Properties.Resources.Hardcodet_Wpf_TaskbarNotification;
                            return Assembly.Load(res);
                        }
                    default:
                        return null;
                }
            };

            // Back to WPF App built-in entry point
            App.Main();
        }
    }
}
