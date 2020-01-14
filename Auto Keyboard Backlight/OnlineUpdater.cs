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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Auto_Keyboard_Backlight
{
    /// <summary>
    /// Ability to check online and download installer for new versions through the Github API
    /// </summary>
    public class OnlineUpdater : IDisposable
    {
        /// <summary>
        /// Describe a downloadable installer version discovered on Github
        /// </summary>
        public class Downloadable
        {
            /// <summary>
            /// Get the version of the file
            /// </summary>
            public Version Version { get; }
            /// <summary>
            /// Get the download link to the file
            /// </summary>
            public Uri Url { get; }

            public Downloadable(Version version, Uri url)
            {
                Version = version;
                Url = url;
            }
        }

        /// <summary>
        /// Get info about the latest program version available online via Github API
        /// </summary>
        /// <returns>Tuple with version# and download url</returns>
        public async Task<Downloadable> GetLatestVersion()
        {
            var uri = new Uri(Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyMetadataAttribute>().First((i) => i.Key == "WebUrl").Value); ;
            uri = new Uri("https://api.github.com/repos" + uri.LocalPath + "/releases/latest");
            var content = await _httpClient.GetAsync(uri);
            if (content.IsSuccessStatusCode)
            {
                var json = await content.Content.ReadAsStringAsync();
                Func<string, string> extract = (key) =>
                {
                    if (json.Contains(key))
                    {
                        int begin = json.IndexOf(key) + key.Length;
                        int start = json.IndexOf("\"", begin);
                        int end = json.IndexOf("\"", start + 1);
                        return json.Substring(start + 1, end - start - 1);
                    }
                    return null;
                };
                var tag = extract("\"tag_name\":");
                var lnk = extract("\"browser_download_url\":");
                if (tag != null && lnk != null)
                    return new Downloadable(new Version(tag), new Uri(lnk));
            }
            return null;
        }
        /// <summary>
        /// Download the installer from Github API, run the installer in separate process
        /// </summary>
        /// <param name="update">Which version to retrieve</param>
        /// <returns>True on success</returns>
        public async Task<bool> DownloadVersion(Downloadable update)
        {
            var content = await _httpClient.GetAsync(update.Url);
            if (content.IsSuccessStatusCode)
            {
                var file = Path.GetTempPath() + Path.GetFileName(update.Url.Segments.Last());
                File.WriteAllBytes(file, await content.Content.ReadAsByteArrayAsync());

                // Get uninstall product code from registry
                string code = "";
                var uninstallKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                var progs = uninstallKey.GetSubKeyNames();
                foreach (string s in progs)
                {
                    string dname = (string)uninstallKey.OpenSubKey(s).GetValue("DisplayName", "");
                    if (dname == Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product)
                        code = s;
                }

                // Run uninstaller, installer in new process
                Process proc = new Process();
                string cmd = "start /wait msiexec /x \"" + code + "\" /passive && start /wait msiexec /i \"" + file + "\"";
                proc.StartInfo.FileName = Environment.GetEnvironmentVariable("ComSpec");
                proc.StartInfo.Arguments = "/c " + cmd;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.Start();
                return true;
            }
            return false;
        }

        public OnlineUpdater()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            _httpClient.Timeout = TimeSpan.FromSeconds(5);
            string name = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyProductAttribute>().Product;
            string ver = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(name.Replace(" ", "") + "/" + ver);
        }

        public void Dispose()
        {
            ((IDisposable)_httpClient).Dispose();
        }

        private HttpClient _httpClient = new HttpClient();
    }
}
