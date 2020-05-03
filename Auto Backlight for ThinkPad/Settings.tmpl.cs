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

// <tmpl Keys="Type_; Name_; def_; min_; max_; unit_; help_">
// bool; Keyboard_Enabled; true; false; true; ""; "Enable automatic keyboard backlight control with user activity timeout"
// double; Keyboard_InactivityTimeout; 60; 1; double.PositiveInfinity; "s"; "Keyboard backlight timeout after user inactivity"
// int; Keyboard_OnLevel; 2; 0; 2; ""; "Keyboard backlight brightness level when illuminated"
// bool; Screen_Enabled; true; false; true; ""; "Enable automatic screen brightness control with ambient light level from webcam image"
// double; Screen_PollPeriod; double.PositiveInfinity; 1; double.PositiveInfinity; "s"; "Screen brightness refresh period from webcam image"
// bool; Screen_HotKeyEnabled; true; false; true; ""; "Enable a hotKey to trigger manual screen brightness refresh"
// string; Screen_HotKey; "Alt + Space"; "default"; "default"; ""; "HotKey to trigger manual screen brightness refresh"
// List<Tuple<double,double>>; Screen_LearnedPoints; new List<Tuple<double,double>>{ new Tuple<double,double>(0.00,0.10), new Tuple<double,double>(0.10,0.65), new Tuple<double,double>(0.35,0.95), new Tuple<double,double>(1.00,1.00)}; null; null; ""; "Screen brightness vs ambient light level calibration curve"
// double; Screen_Curvature; 0.5; 0; 1; ""; "Screen brightness vs ambient light level calibration curve; parameter for smoothed spline"
// </tmpl>

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

        // <tmpl>
        public Type_ Name_ { get => _raw.Name_; set => _Set(ref _raw.Name_, value, Meta.Name_, i_, nameof(Name_)); }
        // </tmpl>

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
            // <tmpl>
            public Type_ Name_;
            // </tmpl>
        }
        private Raw _raw;
        private readonly int _cnt = typeof(Raw).GetFields().Length;

        public int ToIndex(string key)
        {
            switch (key)
            {
                // <tmpl>
                case nameof(Name_): { return i_; }
                // </tmpl>
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }
        public string ToName(int index)
        {
            switch (index)
            {
                // <tmpl>
                case i_: { return nameof(Name_); }
                // </tmpl>
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
            // <tmpl>
            _raw.Name_ = Meta.Name_.Def;
            // </tmpl>

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
            // <tmpl>
            RaisePropertyChanged(nameof(Name_));
            // </tmpl>
        }

        // <tmpl Ignore="true">
        public class Type_ { }
        public const int i_ = default;
        public const Type_ def_ = default;
        public const Type_ min_ = default;
        public const Type_ max_ = default;
        public const string unit_ = default;
        public const string help_ = default;
        // </tmpl>

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
                        // <tmpl>
                        case i_: { return s.Name_; }
                        // </tmpl>
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (index)
                    {
                        // <tmpl>
                        case i_: { s.Name_ = (Type_)value; return; }
                        // </tmpl>
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
            }
            public IEnumerator<object> GetEnumerator()
            {
                // <tmpl>
                yield return s.Name_;
                // </tmpl>
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
                    // <tmpl>
                    yield return nameof(s.Name_);
                    // </tmpl>
                }
            }
            public IEnumerable<object> Values
            {
                get
                {
                    // <tmpl>
                    yield return s.Name_;
                    // </tmpl>
                }
            }
            public object this[string key]
            {
                get
                {
                    switch (key)
                    {
                        // <tmpl>
                        case nameof(s.Name_): { return s.Name_; }
                        // </tmpl>
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (key)
                    {
                        // <tmpl>
                        case nameof(s.Name_): { s.Name_ = (Type_)value; return; }
                        // </tmpl>
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
                // <tmpl>
                yield return new KeyValuePair<string, object>(nameof(s.Name_), s.Name_);
                // </tmpl>
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
                // <tmpl>
                _raw.Name_ = new Metadata<Type_>(def_, min_, max_, unit_, help_);
                // </tmpl>

                List = new ListView(this);
                Dictionary = new DictionaryView(this);
            }
            // <tmpl>
            public Metadata<Type_> Name_ => _raw.Name_;
            // </tmpl>

            private struct Raw
            {
                // <tmpl>
                public Metadata<Type_> Name_;
                // </tmpl>
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
                            // <tmpl>
                            case i_: { return m.Name_; }
                            // </tmpl>
                            default: { throw new IndexOutOfRangeException(); }
                        }
                    }
                }
                public IEnumerator<object> GetEnumerator()
                {
                    // <tmpl>
                    yield return m.Name_;
                    // </tmpl>
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
                        // <tmpl>
                        yield return nameof(m.Name_);
                        // </tmpl>
                    }
                }
                public IEnumerable<object> Values
                {
                    get
                    {
                        // <tmpl>
                        yield return m.Name_;
                        // </tmpl>
                    }
                }
                public object this[string key]
                {
                    get
                    {
                        switch (key)
                        {
                            // <tmpl>
                            case nameof(m.Name_): { return m.Name_; }
                            // </tmpl>
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
                    // <tmpl>
                    yield return new KeyValuePair<string, object>(nameof(m.Name_), m.Name_);
                    // </tmpl>
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