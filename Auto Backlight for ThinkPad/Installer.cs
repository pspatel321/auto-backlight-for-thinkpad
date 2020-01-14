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

using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Additional operations to run during Install/Uninstall when invoked by the .msi installer
    /// </summary>
    [RunInstaller(true)]
    public class Installer : System.Configuration.Install.Installer
    {
        protected override void OnCommitted(IDictionary savedState)
        {
            base.OnCommitted(savedState);

            // Prepare temporary file describing a task (xml) in Task Scheduler from resource template
            string xml = Properties.Resources.task;
            xml = xml.Replace("{Author}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>().Company);
            xml = xml.Replace("{Description}", Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description);
            xml = xml.Replace("{URI}", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\"");
            xml = xml.Replace("{Command}", "\"" + Assembly.GetExecutingAssembly().Location + "\"");
            xml = xml.Replace("{Arguments}", "");
            var file = Path.GetTempFileName();
            File.WriteAllText(file, xml, Encoding.Unicode);

            // Add startup task
            DoCmd("schtasks", string.Format("/Create /TN {0} /F /XML {1}", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\"", file), true);
            File.Delete(file);

            // Start now
            DoCmd("schtasks", string.Format("/Run /TN {0}", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""), true);
        }
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            base.OnBeforeUninstall(savedState);

            // Kill running instances
            var procs = Process.GetProcessesByName(Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product);
            foreach (var p in procs)
            {
                p.Kill();
                p.WaitForExit();
            }

            // Delete task
            DoCmd("schtasks", string.Format("/Delete /TN {0} /F", "\"" + "\\" + Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product + "\""), true);
        }
        public static int DoCmd(string cmd, string args, bool asAdmin = false)
        {
            var proc = new Process();
            proc.StartInfo.FileName = cmd;
            proc.StartInfo.Arguments = args;
            proc.StartInfo.Verb = asAdmin ? "runas" : "";
            proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            proc.Start();
            proc.WaitForExit();
            return proc.ExitCode;
        }
    }
}
