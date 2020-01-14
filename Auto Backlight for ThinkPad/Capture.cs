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

using Auto_Backlight_for_ThinkPad.RawInput;
using System;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Implement fully-configured Win32 Raw Input for capturing mouse/keyboard events from only the built-in mouse/keyboard
    /// </summary>
    public class Capture
    {
        /// <summary>
        /// Event triggered whenever by raw input to built-in mouse/keyboard
        /// </summary>
        public event EventHandler CaptureEvent;
        /// <summary>
        /// Start capturing mouse/keyboard events, same as <see cref="Enabled"/>=true.
        /// </summary>
        public void Start() => Enabled = true;
        /// <summary>
        /// Stop capturing mouse/keyboard events, same as <see cref="Enabled"/>=false.
        /// </summary>
        public void Stop() => Enabled = false;
        /// <summary>
        /// Get/set the enabled state of capturing, same as <see cref="Start"/>/<see cref="Stop"/>
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (value)
                    {
                        _ri.RegisterDeviceCollections(new DeviceCollection[] { _keyboardColl, _mouseColl, _touchpadColl });
                    }
                    else
                    {
                        _ri.UnregisterDeviceCollections();
                    }
                }
            }
        }
        /// <summary>
        /// Initialize a new instance configured to capture from built-in mouse/keyboard
        /// </summary>
        /// <param name="ri">Access to a prepared <see cref="RawInput.RawInput"/> object to receive events</param>
        public Capture(RawInput.RawInput ri)
        {
            _ri = ri;
            // Create mouse/keyboard/touchpad collection configs
            // INPUTSINK causes all input activity (foreground and background) to trigger messages
            _mouseColl.usagePage = 0x1;
            _mouseColl.usage = 0x2;
            _mouseColl.flags = DeviceCollection.Flag.RIDEV_INPUTSINK;
            _keyboardColl.usagePage = 0x1;
            _keyboardColl.usage = 0x6;
            _keyboardColl.flags = DeviceCollection.Flag.RIDEV_INPUTSINK;
            _touchpadColl.usagePage = 0xd;
            _touchpadColl.usage = 0x5;
            _touchpadColl.flags = DeviceCollection.Flag.RIDEV_INPUTSINK;

            DeviceInputEventHandler captureHandler = (sender, e) =>
            {
                if (!_enabled) return;
                CaptureEvent?.Invoke(this, new EventArgs());
            };
            // Connect capture handler to built-in mouse (trackpoint), keyboard, touchpad
            int f = 0;
            foreach (Device dev in _ri.EnumeratedDevices)
            {
                bool hook = false;
                hook |= dev.Type == Device.DeviceType.Mouse && dev.Path.StartsWith(@"\\?\ACPI#LEN0099#");
                hook |= dev.Type == Device.DeviceType.Keyboard && dev.Path.StartsWith(@"\\?\ACPI#LEN0071#");
                hook |= dev.Type == Device.DeviceType.Hid && dev.Info != null &&
                    ((HidInfo)(dev.Info)).vendorID == 0x06cb &&
                    ((HidInfo)(dev.Info)).productID == 0x0f &&
                    ((HidInfo)(dev.Info)).usagePage == 0xd &&
                    ((HidInfo)(dev.Info)).usage == 0x5;
                if (hook)
                {
                    f++;
                    dev.InputReceived += captureHandler;
                }
            }
            if (f != 3)
                throw new Exception("Failed to hook both Lenovo keyboard and mouse raw input devices");
        }

        private bool _enabled = false;
        private DeviceCollection _mouseColl = new DeviceCollection();
        private DeviceCollection _touchpadColl = new DeviceCollection();
        private DeviceCollection _keyboardColl = new DeviceCollection();
        private RawInput.RawInput _ri;
    }
}
