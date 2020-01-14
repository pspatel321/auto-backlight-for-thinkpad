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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Store a keyboard hotkey as combination of modifiers and key
    /// </summary>
    public class HotKey
    {
        /// <summary>
        /// Get the base key
        /// </summary>
        public Key Key { get; }
        /// <summary>
        /// Get the modifiers (Control, Alt, Shift)
        /// </summary>
        public ModifierKeys Modifiers { get; }

        public HotKey(ModifierKeys modifiers = ModifierKeys.None, Key key = Key.None)
        {
            Key = key;
            Modifiers = modifiers;
        }
        public override string ToString()
        {
            var str = "";
            foreach (ModifierKeys mod in Enum.GetValues(typeof(ModifierKeys)))
                if (((int)Modifiers & (int)mod) != 0)
                    str += Enum.GetName(typeof(ModifierKeys), mod) + " + ";
            if (Key != Key.None)
                str += Key;
            return str;
        }
        public static bool TryParse(string s, out HotKey hotKey)
        {
            try
            {
                hotKey = Parse(s);
                return true;
            }
            catch (Exception)
            {
                hotKey = null;
                return false;
            }
        }
        public static HotKey Parse(string s)
        {
            var toks = s.Trim().Replace(" + ", "\n").Split(null);

            int key = 0;
            if (toks.Last().Length > 0)
                key = (int)Enum.Parse(typeof(Key), toks.Last(), true);
            int mods = 0;
            for (int i = 0; i < toks.Length - 1; i++)
            {
                int mod = (int)Enum.Parse(typeof(ModifierKeys), toks[i], true);
                mods |= mod;
            }
            return new HotKey((ModifierKeys)mods, (Key)key);
        }
    }
    /// <summary>
    /// Store an event for receiving hotkey triggers along with the key combination itself
    /// </summary>
    public class HotKeyEvent
    {
        /// <summary>
        /// Hotkey combination associated with this event
        /// </summary>
        public HotKey HotKey { get; }
        /// <summary>
        /// Triggered when a message is processed for pressing this hotkey combination
        /// </summary>
        public event EventHandler Triggered;

        public HotKeyEvent(HotKey hotKey)
        {
            HotKey = hotKey;
        }
        public void RaiseTriggered() => Triggered?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// An invisible pseudo-element that can be inserted in a WPF window to wrap accesses to the Win32 Hotkey API.
    /// It adds a hook-function for the host window's WndProc in order to access Win32 messages.
    /// </summary>
    public class GlobalHotKey : FrameworkElement
    {
        /// <summary>
        /// Enumerable of currently registered HotKeyEvents
        /// </summary>
        public IEnumerable<HotKeyEvent> RegisteredHotKeys
        {
            get
            {
                foreach (HotKeyEvent hk in _registeredHotKeys.Values)
                    yield return hk;
            }
        }
        /// <summary>
        /// Register hotkey, start triggering events for this hotkey
        /// </summary>
        /// <param name="hk">HotKeyEvent input</param>
        public void RegisterHotKey(HotKeyEvent hk) => RegisterHotKeys(new HotKeyEvent[] { hk });
        /// <summary>
        /// Register hotkeys, start triggering events for these hotkeys
        /// </summary>
        /// <param name="hks">list of HotKeyEvent inputs</param>
        public void RegisterHotKeys(IEnumerable<HotKeyEvent> hks)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            foreach (HotKeyEvent hk in hks)
            {
                int id = _registeredHotKeys.Count;
                if (!RegisterHotKey(_hwnd, id, (uint)hk.HotKey.Modifiers | 0x4000, (uint)KeyInterop.VirtualKeyFromKey(hk.HotKey.Key)))
                    throw new Exception("Bad return value from Win32 function");
                _registeredHotKeys[id] = hk;
            }
        }
        /// <summary>
        /// Unregister hotkey, stop triggering events for this hotkey
        /// </summary>
        /// <param name="hk">HotKeyEvent input</param>
        public void UnregisterHotKey(HotKeyEvent hk) => UnregisterHotKeys(new HotKeyEvent[] { hk });
        /// <summary>
        /// Unregister hotkeys, stop triggering events for these hotkeys
        /// </summary>
        /// <param name="hks">list of HotKeyEvent inputs, null = unregister all</param>
        public void UnregisterHotKeys(IEnumerable<HotKeyEvent> hks = null)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            if (hks == null) hks = RegisteredHotKeys.ToArray();
            foreach (HotKeyEvent hk in hks)
            {
                if (_registeredHotKeys.ContainsValue(hk))
                {
                    int id = -1;
                    foreach (var kv in _registeredHotKeys)
                        if (kv.Value == hk) id = kv.Key;

                    if (!UnregisterHotKey(_hwnd, id))
                        throw new Exception("Bad return value from Win32 function");
                    _registeredHotKeys.Remove(id);
                }
            }
        }
        /// <summary>
        /// Initialize window access.
        /// </summary>
        public GlobalHotKey() : base()
        {
            Initialized += (sender, e) =>
            {
                _hwnd = new WindowInteropHelper(Window.GetWindow(this)).EnsureHandle();
                HwndSource.FromHwnd(_hwnd).AddHook(new HwndSourceHook(_WndProc));
            };
        }

        // Process hotkey by id
        private void _ProcessId(int id)
        {
            if (_registeredHotKeys.ContainsKey(id))
            {
                var hk = _registeredHotKeys[id];
                hk.RaiseTriggered();
            }
        }
        // Receive WndProc message from Win32
        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr lResult = IntPtr.Zero;

            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                _ProcessId(wParam.ToInt32());
                handled = true;
            }
            return lResult;
        }
        private Dictionary<int, HotKeyEvent> _registeredHotKeys = new Dictionary<int, HotKeyEvent>();
        private IntPtr _hwnd;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
