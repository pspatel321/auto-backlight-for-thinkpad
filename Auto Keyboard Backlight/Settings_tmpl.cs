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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Auto_Keyboard_Backlight
{
    public partial class Settings : INotifyPropertyChanged
    {
    
        #region Data

        public double Timeout { get => _raw.Timeout; set => _Set(ref _raw.Timeout, value, Meta.Timeout, 0, nameof(Timeout)); }

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
        public struct Raw
        {
            public double Timeout;
        }
        private Raw _raw;

        public int ToIndex(string key)
        {
            switch (key) {
                case nameof(Timeout): { return 0; }
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }
        public string ToName(int index)
        {
            switch (index) {
                case 0: { return nameof(Timeout); }
                default: { throw new ArgumentOutOfRangeException(); }
            }
        }
        public Settings(Settings s) : this() 
        {
            _raw = s._raw;
        }
        public Settings()
        {
            Meta = new Metadata(
                                Timeout:new Metadata<double>(def:60, min:1, max:double.PositiveInfinity, unit:"s", help:"Keyboard backlight timeout after user inactivity")
                                );
            _raw.Timeout = Meta.Timeout.Def;
            
            List = new ListView(this);
            Dictionary = new DictionaryView(this);
            PropertyChanged += (sender, e) => {
                string name = e.PropertyName;
                Dictionary.RaisePropertyChanged(name.ToString());
                List.RaisePropertyChanged(ToIndex(name).ToString());
            };
            Update();
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        public void Update() {
            RaisePropertyChanged(nameof(Timeout));
        }

        #endregion Internal

        #region DataViews

        public class ListView : IReadOnlyList<object>, INotifyPropertyChanged
        {
            public ListView(Settings settings)
            {
                s = settings;
            }
            public int Count => 1;
            public object this[int index]
            {
                get
                {
                    switch (index) {
                        case 0: { return s.Timeout; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (index) {
                        case 0: { s.Timeout = (double)value; return; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
            }
            public IEnumerator<object> GetEnumerator()
            {
                yield return s.Timeout;
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
            public int Count => 1;
            public IEnumerable<string> Keys
            {
                get
                {
                    yield return nameof(s.Timeout);
                }
            }
            public IEnumerable<object> Values
            {
                get
                {
                    yield return s.Timeout;
                }
            }
            public object this[string key]
            {
                get
                {
                    switch (key) {
                        case nameof(s.Timeout): { return s.Timeout; }
                        default: { throw new IndexOutOfRangeException(); }
                    }
                }
                set
                {
                    switch (key) {
                        case nameof(s.Timeout): { s.Timeout = (double)value; return; }
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
                yield return new KeyValuePair<string, object>(nameof(s.Timeout), s.Timeout);
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
            public Metadata(Metadata<double> Timeout)
            {
                this.Timeout = Timeout;
                
                List = new ListView(this);
                Dictionary = new DictionaryView(this);
            }
            public Metadata<double> Timeout { get; }

            #region MetadataViews
            
            public class ListView : IReadOnlyList<object>
            {
                public ListView(Metadata metadata)
                {
                    m = metadata;
                }
                public int Count => 1;
                public object this[int index]
                {
                    get
                    {
                        switch (index) {
                            case 0: { return m.Timeout; }
                            default: { throw new IndexOutOfRangeException(); }
                        }
                    }
                }
                public IEnumerator<object> GetEnumerator()
                {
                    yield return m.Timeout;
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
                public int Count => 1;
                public IEnumerable<string> Keys
                {
                    get
                    {
                        yield return nameof(m.Timeout);
                    }
                }
                public IEnumerable<object> Values
                {
                    get
                    {
                        yield return m.Timeout;
                    }
                }
                public object this[string key]
                {
                    get
                    {
                        switch (key) {
                            case nameof(m.Timeout): { return m.Timeout; }
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
                    yield return new KeyValuePair<string, object>(nameof(m.Timeout), m.Timeout);
                }
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

                private readonly Metadata m;
            }
            public DictionaryView Dictionary { get; }
            
            #endregion MetadataViews

        }
        public Metadata Meta { get; }

        #endregion Metadata

    }
}

