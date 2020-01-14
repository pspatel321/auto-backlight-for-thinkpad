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
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Hold all of the application's current settings for operation
    /// </summary>
    public partial class Settings : INotifyPropertyChanged
    {
        /// <summary>
        /// Save fields to registry as binary
        /// </summary>
        /// <returns>True = successfully saved fields to registry</returns>        
        public bool SaveToRegistry()
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, new Encapsulated(_raw));
                    App.RegistryKey.SetValue("Settings", ms.ToArray(), RegistryValueKind.Binary);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        /// <summary>
        /// Restore fields from registry as binary
        /// </summary>
        /// <returns>True = successfully read and copied fields from registry</returns>        
        public bool RestoreFromRegistry()
        {
            try
            {
                using (var ms = new MemoryStream((byte[])App.RegistryKey.GetValue("Settings")))
                {
                    var bf = new BinaryFormatter();
                    var enc = (Encapsulated)bf.Deserialize(ms);
                    _raw = Encapsulated.Raw(enc);
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // Encapsulated raw data performs hash checking to verify that it matches the source format (ex. fails if you add/remove variables in Settings)
        [Serializable]
        private struct Encapsulated
        {
            public Encapsulated(Raw raw)
            {
                this.raw = raw;
                header = raw.GetHashCode();
                footer = hash(raw);
            }
            public static Raw Raw(Encapsulated enc)
            {
                bool pass = true;
                pass &= enc.raw.GetHashCode() == enc.header;
                pass &= hash(enc.raw) == enc.footer;
                if (!pass) throw new FormatException();
                return enc.raw;
            }
            private static int hash(Raw raw)
            {
                int hash = 0x1234abcd;
                using (var ms = new MemoryStream())
                {
                    var bf = new BinaryFormatter();
                    bf.Serialize(ms, raw);
                    var arr = ms.ToArray();
                    for (int i = 0; i < arr.Length / 4; i++)
                        hash ^= BitConverter.ToInt32(arr, i * 4);
                }
                return hash;
            }

            private int header;
            private Raw raw;
            private int footer;
        }
    }
}