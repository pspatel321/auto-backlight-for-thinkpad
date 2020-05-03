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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Auto_Backlight_for_ThinkPad
{
    public class Settings : INotifyPropertyChanged
    {
        #region Data

        public bool Keyboard_Enabled { get => _raw.Keyboard_Enabled; set => _Set(ref _raw.Keyboard_Enabled, value, Meta.Keyboard_Enabled, 0, nameof(Keyboard_Enabled)); }
        public double Keyboard_InactivityTimeout { get => _raw.Keyboard_InactivityTimeout; set => _Set(ref _raw.Keyboard_InactivityTimeout, value, Meta.Keyboard_InactivityTimeout, 1, nameof(Keyboard_InactivityTimeout)); }
        public int Keyboard_OnLevel { get => _raw.Keyboard_OnLevel; set => _Set(ref _raw.Keyboard_OnLevel, value, Meta.Keyboard_OnLevel, 2, nameof(Keyboard_OnLevel)); }
        public bool Screen_Enabled { get => _raw.Screen_Enabled; set => _Set(ref _raw.Screen_Enabled, value, Meta.Screen_Enabled, 3, nameof(Screen_Enabled)); }
        public double Screen_PollPeriod { get => _raw.Screen_PollPeriod; set => _Set(ref _raw.Screen_PollPeriod, value, Meta.Screen_PollPeriod, 4, nameof(Screen_PollPeriod)); }
        public bool Screen_HotKeyEnabled { get => _raw.Screen_HotKeyEnabled; set => _Set(ref _raw.Screen_HotKeyEnabled, value, Meta.Screen_HotKeyEnabled, 5, nameof(Screen_HotKeyEnabled)); }
        public string Screen_HotKey { get => _raw.Screen_HotKey; set => _Set(ref _raw.Screen_HotKey, value, Meta.Screen_HotKey, 6, nameof(Screen_HotKey)); }
        public List<Tuple<double,double>> Screen_LearnedPoints { get => _raw.Screen_LearnedPoints; set => _Set(ref _raw.Screen_LearnedPoints, value, Meta.Screen_LearnedPoints, 7, nameof(Screen_LearnedPoints)); }
        public double Screen_Curvature { get => _raw.Screen_Curvature; set => _Set(ref _raw.Screen_Curvature, value, Meta.Screen_Curvature, 8, nameof(Screen_Curvature)); }

        #endregion Data

        #region Internal

        private void _Set<T>(ref T item, T value, Metadata<T> meta, int index, string name)
        {
            bool equatable = typeof(T).GetInterface("IEquatable") != null;
            if (equatable)
            {
                bool comparable = typeof(T).GetInterface("IComparable") != null;
                if (comparable)
                {
                    bool requested = !((dynamic)meta.Min).Equals(default(T)) || !((dynamic)meta.Max).Equals(default(T));
                    if (requested)
                    {
                        if (((dynamic)value).CompareTo(meta.Min) < 0) value = meta.Min;
                        if (((dynamic)value).CompareTo(meta.Max) > 0) value = meta.Max;
                    }
                }
            }
            bool r = true;
            if (equatable)
            {
                if (((dynamic)value).Equals(item)) r = false;
            }
            item = value;
            if (r) RaisePropertyChanged(name);
        }
        [Serializable]
        private struct Raw
        {
            public bool Keyboard_Enabled;
            public double Keyboard_InactivityTimeout;
            public int Keyboard_OnLevel;
            public bool Screen_Enabled;
            public double Screen_PollPeriod;
            public bool Screen_HotKeyEnabled;
            public string Screen_HotKey;
            public List<Tuple<double,double>> Screen_LearnedPoints;
            public double Screen_Curvature;
        }
        private Raw _raw;
        private readonly int _cnt = typeof(Raw).GetFields().Length;

        public int ToIndex(string key)
        {
            switch (key)
            {
                case nameof(Keyboard_Enabled): { return 0; }
                case nameof(Keyboard_InactivityTimeout): { return 1; }
                case nameof(Keyboard_OnLevel): { return 2; }
                case nameof(Screen_Enabled): { return 3; }
                case nameof(Screen_PollPeriod): { return 4; }
                case nameof(Screen_HotKeyEnabled): { return 5; }
                case nameof(Screen_HotKey): { return 6; }
                case nameof(Screen_LearnedPoints): { return 7; }
                case nameof(Screen_Curvature): { return 8; }
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }
        public string ToName(int index)
        {
            switch (index)
            {
                case 0: { return nameof(Keyboard_Enabled); }
                case 1: { return nameof(Keyboard_InactivityTimeout); }
                case 2: { return nameof(Keyboard_OnLevel); }
                case 3: { return nameof(Screen_Enabled); }
                case 4: { return nameof(Screen_PollPeriod); }
                case 5: { return nameof(Screen_HotKeyEnabled); }
                case 6: { return nameof(Screen_HotKey); }
                case 7: { return nameof(Screen_LearnedPoints); }
                case 8: { return nameof(Screen_Curvature); }
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }
        public Settings(Settings s) : this()
        {
            _raw = s._raw;
        }
        public Settings()
        {
            Meta = new Metadata();
            _raw.Keyboard_Enabled = Meta.Keyboard_Enabled.Def;
            _raw.Keyboard_InactivityTimeout = Meta.Keyboard_InactivityTimeout.Def;
            _raw.Keyboard_OnLevel = Meta.Keyboard_OnLevel.Def;
            _raw.Screen_Enabled = Meta.Screen_Enabled.Def;
            _raw.Screen_PollPeriod = Meta.Screen_PollPeriod.Def;
            _raw.Screen_HotKeyEnabled = Meta.Screen_HotKeyEnabled.Def;
            _raw.Screen_HotKey = Meta.Screen_HotKey.Def;
            _raw.Screen_LearnedPoints = Meta.Screen_LearnedPoints.Def;
            _raw.Screen_Curvature = Meta.Screen_Curvature.Def;

            List = new ListView(this);
            Dictionary = new DictionaryView(this);
            PropertyChanged += (sender, e) =>
            {
                string name = e.PropertyName;
                Dictionary.RaisePropertyChanged(name.ToString());
                List.RaisePropertyChanged(ToIndex(name).ToString());
            };
            Update();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public void Update()
        {
            RaisePropertyChanged(nameof(Keyboard_Enabled));
            RaisePropertyChanged(nameof(Keyboard_InactivityTimeout));
            RaisePropertyChanged(nameof(Keyboard_OnLevel));
            RaisePropertyChanged(nameof(Screen_Enabled));
            RaisePropertyChanged(nameof(Screen_PollPeriod));
            RaisePropertyChanged(nameof(Screen_HotKeyEnabled));
            RaisePropertyChanged(nameof(Screen_HotKey));
            RaisePropertyChanged(nameof(Screen_LearnedPoints));
            RaisePropertyChanged(nameof(Screen_Curvature));
        }


        #endregion Internal

        #region DataViews

        public class ListView : IReadOnlyList<object>, INotifyPropertyChanged
        {
            public ListView(Settings settings)
            {
                s = settings;
            }
            public int Count => s._cnt;
            public object this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0: { return s.Keyboard_Enabled; }
                        case 1: { return s.Keyboard_InactivityTimeout; }
                        case 2: { return s.Keyboard_OnLevel; }
                        case 3: { return s.Screen_Enabled; }
                        case 4: { return s.Screen_PollPeriod; }
                        case 5: { return s.Screen_HotKeyEnabled; }
                        case 6: { return s.Screen_HotKey; }
                        case 7: { return s.Screen_LearnedPoints; }
                        case 8: { return s.Screen_Curvature; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (index)
                    {
                        case 0: { s.Keyboard_Enabled = (bool)value; return; }
                        case 1: { s.Keyboard_InactivityTimeout = (double)value; return; }
                        case 2: { s.Keyboard_OnLevel = (int)value; return; }
                        case 3: { s.Screen_Enabled = (bool)value; return; }
                        case 4: { s.Screen_PollPeriod = (double)value; return; }
                        case 5: { s.Screen_HotKeyEnabled = (bool)value; return; }
                        case 6: { s.Screen_HotKey = (string)value; return; }
                        case 7: { s.Screen_LearnedPoints = (List<Tuple<double,double>>)value; return; }
                        case 8: { s.Screen_Curvature = (double)value; return; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
            }
            public IEnumerator<object> GetEnumerator()
            {
                yield return s.Keyboard_Enabled;
                yield return s.Keyboard_InactivityTimeout;
                yield return s.Keyboard_OnLevel;
                yield return s.Screen_Enabled;
                yield return s.Screen_PollPeriod;
                yield return s.Screen_HotKeyEnabled;
                yield return s.Screen_HotKey;
                yield return s.Screen_LearnedPoints;
                yield return s.Screen_Curvature;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public event PropertyChangedEventHandler PropertyChanged;
            public void RaisePropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

            private readonly Settings s;
        }
        public ListView List { get; }

        public class DictionaryView : IReadOnlyDictionary<string, object>, INotifyPropertyChanged
        {
            public DictionaryView(Settings settings)
            {
                s = settings;
            }
            public int Count => s._cnt;
            public IEnumerable<string> Keys
            {
                get
                {
                    yield return nameof(s.Keyboard_Enabled);
                    yield return nameof(s.Keyboard_InactivityTimeout);
                    yield return nameof(s.Keyboard_OnLevel);
                    yield return nameof(s.Screen_Enabled);
                    yield return nameof(s.Screen_PollPeriod);
                    yield return nameof(s.Screen_HotKeyEnabled);
                    yield return nameof(s.Screen_HotKey);
                    yield return nameof(s.Screen_LearnedPoints);
                    yield return nameof(s.Screen_Curvature);
                }
            }
            public IEnumerable<object> Values
            {
                get
                {
                    yield return s.Keyboard_Enabled;
                    yield return s.Keyboard_InactivityTimeout;
                    yield return s.Keyboard_OnLevel;
                    yield return s.Screen_Enabled;
                    yield return s.Screen_PollPeriod;
                    yield return s.Screen_HotKeyEnabled;
                    yield return s.Screen_HotKey;
                    yield return s.Screen_LearnedPoints;
                    yield return s.Screen_Curvature;
                }
            }
            public object this[string key]
            {
                get
                {
                    switch (key)
                    {
                        case nameof(s.Keyboard_Enabled): { return s.Keyboard_Enabled; }
                        case nameof(s.Keyboard_InactivityTimeout): { return s.Keyboard_InactivityTimeout; }
                        case nameof(s.Keyboard_OnLevel): { return s.Keyboard_OnLevel; }
                        case nameof(s.Screen_Enabled): { return s.Screen_Enabled; }
                        case nameof(s.Screen_PollPeriod): { return s.Screen_PollPeriod; }
                        case nameof(s.Screen_HotKeyEnabled): { return s.Screen_HotKeyEnabled; }
                        case nameof(s.Screen_HotKey): { return s.Screen_HotKey; }
                        case nameof(s.Screen_LearnedPoints): { return s.Screen_LearnedPoints; }
                        case nameof(s.Screen_Curvature): { return s.Screen_Curvature; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (key)
                    {
                        case nameof(s.Keyboard_Enabled): { s.Keyboard_Enabled = (bool)value; return; }
                        case nameof(s.Keyboard_InactivityTimeout): { s.Keyboard_InactivityTimeout = (double)value; return; }
                        case nameof(s.Keyboard_OnLevel): { s.Keyboard_OnLevel = (int)value; return; }
                        case nameof(s.Screen_Enabled): { s.Screen_Enabled = (bool)value; return; }
                        case nameof(s.Screen_PollPeriod): { s.Screen_PollPeriod = (double)value; return; }
                        case nameof(s.Screen_HotKeyEnabled): { s.Screen_HotKeyEnabled = (bool)value; return; }
                        case nameof(s.Screen_HotKey): { s.Screen_HotKey = (string)value; return; }
                        case nameof(s.Screen_LearnedPoints): { s.Screen_LearnedPoints = (List<Tuple<double,double>>)value; return; }
                        case nameof(s.Screen_Curvature): { s.Screen_Curvature = (double)value; return; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
            }
            public bool ContainsKey(string key)
            {
                foreach (string k in Keys)
                    if (key == k) return true;
                return false;
            }
            public bool TryGetValue(string key, out object value)
            {
                if (ContainsKey(key))
                {
                    value = this[key];
                    return true;
                }
                else
                {
                    value = null;
                    return false;
                }
            }
            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                yield return new KeyValuePair<string, object>(nameof(s.Keyboard_Enabled), s.Keyboard_Enabled);
                yield return new KeyValuePair<string, object>(nameof(s.Keyboard_InactivityTimeout), s.Keyboard_InactivityTimeout);
                yield return new KeyValuePair<string, object>(nameof(s.Keyboard_OnLevel), s.Keyboard_OnLevel);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_Enabled), s.Screen_Enabled);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_PollPeriod), s.Screen_PollPeriod);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_HotKeyEnabled), s.Screen_HotKeyEnabled);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_HotKey), s.Screen_HotKey);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_LearnedPoints), s.Screen_LearnedPoints);
                yield return new KeyValuePair<string, object>(nameof(s.Screen_Curvature), s.Screen_Curvature);
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public event PropertyChangedEventHandler PropertyChanged;
            public void RaisePropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

            private readonly Settings s;
        }
        public DictionaryView Dictionary { get; }

        #endregion DataViews

        #region Metadata

        public struct Metadata<T>
        {
            public Metadata(T def, T min, T max, string unit, string help)
            {
                Def = def;
                Min = min;
                Max = max;
                Unit = unit;
                Help = help;
            }
            public T Def { get; }
            public T Min { get; }
            public T Max { get; }
            public string Unit { get; }
            public string Help { get; }
        }

        public class Metadata
        {
            public Metadata()
            {
                _raw.Keyboard_Enabled = new Metadata<bool>(true, false, true, "", "Enable automatic keyboard backlight control with user activity timeout");
                _raw.Keyboard_InactivityTimeout = new Metadata<double>(60, 1, double.PositiveInfinity, "s", "Keyboard backlight timeout after user inactivity");
                _raw.Keyboard_OnLevel = new Metadata<int>(2, 0, 2, "", "Keyboard backlight brightness level when illuminated");
                _raw.Screen_Enabled = new Metadata<bool>(true, false, true, "", "Enable automatic screen brightness control with ambient light level from webcam image");
                _raw.Screen_PollPeriod = new Metadata<double>(double.PositiveInfinity, 1, double.PositiveInfinity, "s", "Screen brightness refresh period from webcam image");
                _raw.Screen_HotKeyEnabled = new Metadata<bool>(true, false, true, "", "Enable a hotKey to trigger manual screen brightness refresh");
                _raw.Screen_HotKey = new Metadata<string>("Alt + Space", "default", "default", "", "HotKey to trigger manual screen brightness refresh");
                _raw.Screen_LearnedPoints = new Metadata<List<Tuple<double,double>>>(new List<Tuple<double,double>>{ new Tuple<double,double>(0.00,0.10), new Tuple<double,double>(0.10,0.65), new Tuple<double,double>(0.35,0.95), new Tuple<double,double>(1.00,1.00)}, null, null, "", "Screen brightness vs ambient light level calibration curve");
                _raw.Screen_Curvature = new Metadata<double>(0.5, 0, 1, "", "Screen brightness vs ambient light level calibration curve; parameter for smoothed spline");

                List = new ListView(this);
                Dictionary = new DictionaryView(this);
            }
            public Metadata<bool> Keyboard_Enabled => _raw.Keyboard_Enabled;
            public Metadata<double> Keyboard_InactivityTimeout => _raw.Keyboard_InactivityTimeout;
            public Metadata<int> Keyboard_OnLevel => _raw.Keyboard_OnLevel;
            public Metadata<bool> Screen_Enabled => _raw.Screen_Enabled;
            public Metadata<double> Screen_PollPeriod => _raw.Screen_PollPeriod;
            public Metadata<bool> Screen_HotKeyEnabled => _raw.Screen_HotKeyEnabled;
            public Metadata<string> Screen_HotKey => _raw.Screen_HotKey;
            public Metadata<List<Tuple<double,double>>> Screen_LearnedPoints => _raw.Screen_LearnedPoints;
            public Metadata<double> Screen_Curvature => _raw.Screen_Curvature;

            private struct Raw
            {
                public Metadata<bool> Keyboard_Enabled;
                public Metadata<double> Keyboard_InactivityTimeout;
                public Metadata<int> Keyboard_OnLevel;
                public Metadata<bool> Screen_Enabled;
                public Metadata<double> Screen_PollPeriod;
                public Metadata<bool> Screen_HotKeyEnabled;
                public Metadata<string> Screen_HotKey;
                public Metadata<List<Tuple<double,double>>> Screen_LearnedPoints;
                public Metadata<double> Screen_Curvature;
            }
            private readonly Raw _raw;
            private readonly int _cnt = typeof(Raw).GetFields().Length;

            #region MetadataViews

            public class ListView : IReadOnlyList<object>
            {
                public ListView(Metadata metadata)
                {
                    m = metadata;
                }
                public int Count => m._cnt;
                public object this[int index]
                {
                    get
                    {
                        switch (index)
                        {
                            case 0: { return m.Keyboard_Enabled; }
                            case 1: { return m.Keyboard_InactivityTimeout; }
                            case 2: { return m.Keyboard_OnLevel; }
                            case 3: { return m.Screen_Enabled; }
                            case 4: { return m.Screen_PollPeriod; }
                            case 5: { return m.Screen_HotKeyEnabled; }
                            case 6: { return m.Screen_HotKey; }
                            case 7: { return m.Screen_LearnedPoints; }
                            case 8: { return m.Screen_Curvature; }
                            default: { throw new IndexOutOfRangeException(); }
                        }
                    }
                }
                public IEnumerator<object> GetEnumerator()
                {
                    yield return m.Keyboard_Enabled;
                    yield return m.Keyboard_InactivityTimeout;
                    yield return m.Keyboard_OnLevel;
                    yield return m.Screen_Enabled;
                    yield return m.Screen_PollPeriod;
                    yield return m.Screen_HotKeyEnabled;
                    yield return m.Screen_HotKey;
                    yield return m.Screen_LearnedPoints;
                    yield return m.Screen_Curvature;
                }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                private readonly Metadata m;
            }
            public ListView List { get; }

            public class DictionaryView : IReadOnlyDictionary<string, object>
            {
                public DictionaryView(Metadata metadata)
                {
                    m = metadata;
                }
                public int Count => m._cnt;
                public IEnumerable<string> Keys
                {
                    get
                    {
                        yield return nameof(m.Keyboard_Enabled);
                        yield return nameof(m.Keyboard_InactivityTimeout);
                        yield return nameof(m.Keyboard_OnLevel);
                        yield return nameof(m.Screen_Enabled);
                        yield return nameof(m.Screen_PollPeriod);
                        yield return nameof(m.Screen_HotKeyEnabled);
                        yield return nameof(m.Screen_HotKey);
                        yield return nameof(m.Screen_LearnedPoints);
                        yield return nameof(m.Screen_Curvature);
                    }
                }
                public IEnumerable<object> Values
                {
                    get
                    {
                        yield return m.Keyboard_Enabled;
                        yield return m.Keyboard_InactivityTimeout;
                        yield return m.Keyboard_OnLevel;
                        yield return m.Screen_Enabled;
                        yield return m.Screen_PollPeriod;
                        yield return m.Screen_HotKeyEnabled;
                        yield return m.Screen_HotKey;
                        yield return m.Screen_LearnedPoints;
                        yield return m.Screen_Curvature;
                    }
                }
                public object this[string key]
                {
                    get
                    {
                        switch (key)
                        {
                            case nameof(m.Keyboard_Enabled): { return m.Keyboard_Enabled; }
                            case nameof(m.Keyboard_InactivityTimeout): { return m.Keyboard_InactivityTimeout; }
                            case nameof(m.Keyboard_OnLevel): { return m.Keyboard_OnLevel; }
                            case nameof(m.Screen_Enabled): { return m.Screen_Enabled; }
                            case nameof(m.Screen_PollPeriod): { return m.Screen_PollPeriod; }
                            case nameof(m.Screen_HotKeyEnabled): { return m.Screen_HotKeyEnabled; }
                            case nameof(m.Screen_HotKey): { return m.Screen_HotKey; }
                            case nameof(m.Screen_LearnedPoints): { return m.Screen_LearnedPoints; }
                            case nameof(m.Screen_Curvature): { return m.Screen_Curvature; }
                            default: { throw new IndexOutOfRangeException(); }
                        }
                    }
                }
                public bool ContainsKey(string key)
                {
                    foreach (string k in Keys)
                        if (key == k) return true;
                    return false;
                }
                public bool TryGetValue(string key, out object value)
                {
                    if (ContainsKey(key))
                    {
                        value = this[key];
                        return true;
                    }
                    else
                    {
                        value = null;
                        return false;
                    }
                }
                public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
                {
                    yield return new KeyValuePair<string, object>(nameof(m.Keyboard_Enabled), m.Keyboard_Enabled);
                    yield return new KeyValuePair<string, object>(nameof(m.Keyboard_InactivityTimeout), m.Keyboard_InactivityTimeout);
                    yield return new KeyValuePair<string, object>(nameof(m.Keyboard_OnLevel), m.Keyboard_OnLevel);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_Enabled), m.Screen_Enabled);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_PollPeriod), m.Screen_PollPeriod);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_HotKeyEnabled), m.Screen_HotKeyEnabled);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_HotKey), m.Screen_HotKey);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_LearnedPoints), m.Screen_LearnedPoints);
                    yield return new KeyValuePair<string, object>(nameof(m.Screen_Curvature), m.Screen_Curvature);
                }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                private readonly Metadata m;
            }
            public DictionaryView Dictionary { get; }

            #endregion MetadataViews

        }
        public Metadata Meta { get; }

        #endregion Metadata

        #region Registry

        /// <summary>
        /// Save fields to registry as binary
        /// </summary>
        /// <returns>True = successfully saved fields to registry</returns>        
        public bool SaveToRegistry(RegistryKey key)
        {
            try
            {
                key.SetValue("Settings", Encapsulated.ToBytes(_raw), RegistryValueKind.Binary);
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
        public bool RestoreFromRegistry(RegistryKey key)
        {
            try
            {
                _raw = Encapsulated.FromBytes((byte[])key.GetValue("Settings"));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        // Store raw data with checksums to verify it matches source type
        [Serializable]
        private struct Encapsulated
        {
            public Encapsulated(Raw raw)
            {
                this.raw = raw;
                header = raw.GetHashCode();
                footer = hash(raw);
            }
            public static byte[] ToBytes(Raw raw)
            {
                var enc = new Encapsulated(raw);
                var bf = new BinaryFormatter();
                using (var ms = new MemoryStream())
                {
                    bf.Serialize(ms, enc);
                    return ms.ToArray();
                }
            }
            public static Raw FromBytes(byte[] bytes)
            {
                var bf = new BinaryFormatter();
                using (var ms = new MemoryStream(bytes))
                {
                    var enc = (Encapsulated)bf.Deserialize(ms);

                    bool pass = true;
                    pass &= enc.raw.GetHashCode() == enc.header;
                    pass &= hash(enc.raw) == enc.footer;
                    if (!pass) throw new FormatException();
                    return enc.raw;
                }
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
    #endregion Registry
}