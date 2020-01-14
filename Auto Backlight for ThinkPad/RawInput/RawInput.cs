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
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Auto_Backlight_for_ThinkPad.RawInput
{
    using static NativeMethods;

    /// <summary>
    /// Contains data passed to a <see cref="DeviceInputEventHandler"/>.
    /// </summary>
    public class DeviceInputEventArgs : EventArgs
    {
        /// <summary>
        /// Get the data object. Depending on <see cref="Device.Type"/> of the sender, it could be <see cref="MouseData"/>, <see cref="KeyboardData"/>, or <see cref="HidData"/>.
        /// </summary>
        public object Data { get; }
        public DeviceInputEventArgs(object data)
        {
            Data = data;
        }
    }
    /// <summary>
    /// Event handler type for processing input events from a specific <see cref="Device"/>.
    /// </summary>
    /// <param name="sender"><see cref="Device"/> object responsible for triggering the event</param>
    /// <param name="e">Contains input data for this event</param>
    public delegate void DeviceInputEventHandler(object sender, DeviceInputEventArgs e);

    /// <summary>
    /// Represents a hardware device attached to the PC. User should not need to create any instances of this class. They are created internally by <see cref="RawInput.EnumerateDevices">, and retrieved by <see cref="RawInput.EnumeratedDevices"/>.
    /// </summary>
    public class Device
    {
        public enum DeviceType
        {
            Mouse = 0,
            Keyboard = 1,
            Hid = 2
        }
        /// <summary>
        /// Get the type of device (Mouse, Keyboard, or Hid)
        /// </summary>
        public DeviceType Type { get; }
        /// <summary>
        /// Get the unique device instance path (as found in Device Manager)
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Get the extra information structure. User should cast the object to <see cref="MouseInfo"/>, <see cref="KeyboardInfo"/>, or <see cref="HidInfo"/>
        /// </summary>
        public object Info { get; }
        /// <summary>
        /// Event triggered when input data is processed for this device.
        /// </summary>
        public event DeviceInputEventHandler InputReceived;

        public Device(DeviceType type, string name, object info)
        {
            Type = type;
            Path = name;
            Info = info;
        }
        public void RaiseInputReceived(object data) => InputReceived?.Invoke(this, new DeviceInputEventArgs(data));
    }
    /// <summary>
    /// Contains data passed to a <see cref="RawInputEventHandler"/>.
    /// </summary>
    public class RawInputEventArgs : DeviceInputEventArgs
    {
        /// <summary>
        /// Get the device which triggered the event.
        /// </summary>
        public Device Device { get; }

        public RawInputEventArgs(Device device, object data) : base(data)
        {
            Device = device;
        }
    }
    /// <summary>
    /// Event handler type for processing input events.
    /// </summary>
    /// <param name="sender">The <see cref="RawInput"/> object responsible for triggering the event</param>
    /// <param name="e">Contains input data for this event</param>
    public delegate void RawInputEventHandler(object sender, RawInputEventArgs e);

    /// <summary>
    /// Struct used to configure which input messages are received. Used for <see cref="RegisterRawInputDevices(RAWINPUTDEVICE[], uint, uint)"/> and similar.
    /// The <see cref="DeviceCollection"/> describes a filter that allows certain groups of devices to notify the host <see cref="RawInput"/>.
    /// </summary>
    public partial struct DeviceCollection
    {
        public ushort usagePage;
        public ushort usage;
        public Flag flags;
    }
    /// <summary>
    /// Struct descibing information for an HID type device, used in <see cref="Device.Info"/>
    /// </summary>
    public struct HidInfo
    {
        public uint vendorID;
        public uint productID;
        public uint versionNumber;
        public ushort usagePage;
        public ushort usage;
    }
    /// <summary>
    /// Struct describing information for a keyboard type device, used in <see cref="Device.Info"/>
    /// </summary>
    public struct KeyboardInfo
    {
        public uint type;
        public uint subType;
        public uint keyboardMode;
        public uint numberOfFunctionKeys;
        public uint numberOfIndicators;
        public uint numberOfKeysTotal;
    }
    /// <summary>
    /// Struct describing information for a mouse type device, used in <see cref="Device.Info"/>
    /// </summary>
    public struct MouseInfo
    {
        public uint id;
        public uint numberOfButtons;
        public uint sampleRate;
        public bool hasHorizontalWheel;
    }
    /// <summary>
    /// Struct describing data for Hid type device, used in <see cref="DeviceInputEventArgs.Data"/>
    /// </summary>
    public struct HidData
    {
        public byte[][] rawData;
    }
    /// <summary>
    /// Struct describing data for keyboard type device, used in <see cref="DeviceInputEventArgs.Data"/>
    /// </summary>
    public struct KeyboardData
    {
        public ushort makecode;
        public ushort flags;
        public ushort reserved;
        public ushort vKey;
        public uint message;
        public uint extraInformation;
    }
    /// <summary>
    /// Struct describing data for mouse type device, used in <see cref="DeviceInputEventArgs.Data"/>
    /// </summary>
    public struct MouseData
    {
        public ushort flags;
        public uint buttons;
        public uint rawButtons;
        public int lastX;
        public int lastY;
        public uint extraInformation;
    }

    /// <summary>
    /// An invisible pseudo-element that can be inserted in a WPF window to wrap accesses to the Win32 Raw Input messaging API.
    /// It adds a hook-function for the host window's WndProc in order to access Win32 messages.
    /// </summary>
    public class RawInput : FrameworkElement
    {
        /// <summary>
        /// Event for universal raw input by any incoming raw input message triggered by a WM_INPUT message to the host window.
        /// </summary>
        public event RawInputEventHandler InputReceived;
        /// <summary>
        /// Event raised when a new device is added triggered by a WM_INPUT_DEVICE_CHANGE message to the host window.
        /// Only works if <see cref="DeviceCollection.RIDEV_DEVNOTIFY"/> flag is specified in the registered <see cref="DeviceCollection"/>.
        /// </summary>
        public event RawInputEventHandler DeviceAdded;
        /// <summary>
        /// Event raised when a new device is removed triggered by a WM_INPUT_DEVICE_CHANGE message to the host windows's WndProc.
        /// Only works if <see cref="DeviceCollection.RIDEV_DEVNOTIFY"/> flag is specified in the registered <see cref="DeviceCollection"/>.
        /// </summary>
        public event RawInputEventHandler DeviceRemoved;
        /// <summary>
        /// Get an enumerator to internal hardware devices to access detailed info and specific events. The list is updated by calling <see cref="EnumerateDevices"/>.
        /// </summary>
        public IEnumerable<Device> EnumeratedDevices
        {
            get
            {
                foreach (Device.DeviceType t in _nullDevices.Keys)
                    yield return _nullDevices[t];
                foreach (IntPtr k in _devices.Keys)
                    yield return _devices[k];
            }
        }
        /// <summary>
        /// Add new raw input device collection to request messages, starts raw input messages.
        /// </summary>
        /// <param name="cfg">Device collection to register</param>
        public void RegisterDeviceCollection(DeviceCollection cfg) => RegisterDeviceCollections(new DeviceCollection[] { cfg });
        /// <summary>
        /// Add new raw input device collections to request messages, starts raw input messages.
        /// </summary>
        /// <param name="cfg">Device collections to register</param>
        public void RegisterDeviceCollections(IEnumerable<DeviceCollection> cfg)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            if (cfg.Count() == 0) return;
            var rids = new RAWINPUTDEVICE[cfg.Count()];
            int i = 0;
            foreach (DeviceCollection dc in cfg)
            {
                rids[i].usUsage = dc.usage;
                rids[i].usUsagePage = dc.usagePage;
                rids[i].dwFlags = (uint)dc.flags;
                rids[i].hwndTarget = _hwnd;
                i++;
            }

            if (!RegisterRawInputDevices(rids, (uint)rids.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new Exception("Failed call to RegisterRawInputDevices().");
            foreach (DeviceCollection dc in cfg)
                _registeredCollections.Add(dc);
        }
        /// <summary>
        /// Remove raw input device collection, stops raw input messages.
        /// </summary>
        /// <param name="cfg">Device collection to stop</param>
        public void UnregisterDeviceCollection(DeviceCollection cfg) => UnregisterDeviceCollections(new DeviceCollection[] { cfg });
        /// <summary>
        /// Remove raw input device collections, stops raw input messages.
        /// </summary>
        /// <param name="cfg">Device collections to stop, null = remove all currently registered device collections</param>
        public void UnregisterDeviceCollections(IEnumerable<DeviceCollection> cfg = null)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            if (cfg == null) cfg = RegisteredDeviceCollections.ToArray();
            if (cfg.Count() == 0) return;
            var rids = new RAWINPUTDEVICE[cfg.Count()];
            int i = 0;
            foreach (DeviceCollection dc in cfg)
            {
                rids[i].usUsage = dc.usage;
                rids[i].usUsagePage = dc.usagePage;
                rids[i].dwFlags = (uint)DeviceCollection.Flag.RIDEV_REMOVE;
                rids[i].hwndTarget = IntPtr.Zero;
                i++;
            }

            if (!RegisterRawInputDevices(rids, (uint)rids.Length, (uint)Marshal.SizeOf(typeof(RAWINPUTDEVICE))))
                throw new Exception("Failed call to Win32 RegisterRawInputDevices().");
            foreach (DeviceCollection dc in cfg)
                _registeredCollections.Remove(dc);
        }
        /// <summary>
        /// Retrieve list of currently registered raw input device collections.
        /// </summary>
        /// <returns>Currently registered device collections</returns>
        public IEnumerable<DeviceCollection> RegisteredDeviceCollections
        {
            get
            {
                foreach (DeviceCollection dc in _registeredCollections)
                    yield return dc;
            }
        }
        /// <summary>
        /// Rebuild the <see cref="EnumeratedDevices"/> list by querying for currently attached hardware.
        /// </summary>
        /// <returns>List of available devices, same as <see cref="EnumeratedDevices"/></returns>
        public IEnumerable<Device> EnumerateDevices()
        {
            // Remove devices
            foreach (IntPtr hDevice in _devices.Keys)
                _EnumerateRemove(hDevice);

            // Populate list of current devices
            uint numDevices = 0;
            if (0 > GetRawInputDeviceList(null, ref numDevices, (uint)(Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)))))
                throw new Exception("Bad return value from Win32 function");
            if (numDevices <= 0) return EnumeratedDevices;
            var ridList = new RAWINPUTDEVICELIST[numDevices];
            if (0 >= GetRawInputDeviceList(ridList, ref numDevices, (uint)(Marshal.SizeOf(typeof(RAWINPUTDEVICELIST)))))
                throw new Exception("Bad return value from Win32 function");

            // Add devices, looping through each struct in the array
            foreach (RAWINPUTDEVICELIST rid in ridList)
            {
                if (rid.dwType >= 0 && rid.dwType <= RIM_TYPEMAX && rid.hDevice != null)
                    _EnumerateAdd(rid.hDevice);
            }
            return EnumeratedDevices;
        }
        /// <summary>
        /// Initialize window access. Automatically run <see cref="EnumerateDevices"/> to build internal devices list.
        /// </summary>
        public RawInput() : base()
        {
            _nullDevices[Device.DeviceType.Mouse] = new Device(Device.DeviceType.Mouse, "", null);
            _nullDevices[Device.DeviceType.Keyboard] = new Device(Device.DeviceType.Keyboard, "", null);
            _nullDevices[Device.DeviceType.Hid] = new Device(Device.DeviceType.Hid, "", null);
            EnumerateDevices();

            Initialized += (sender, e) =>
            {
                _hwnd = new WindowInteropHelper(Window.GetWindow(this)).EnsureHandle();
                HwndSource.FromHwnd(_hwnd).AddHook(new HwndSourceHook(_WndProc));
            };
        }

        // WndProc special message handling
        private IntPtr _WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            IntPtr lResult = IntPtr.Zero;
            if (msg == WM_INPUT)
            {
                _ProcessInput(ref lResult, hwnd, msg, wParam, lParam);
                handled = true;
            }
            if (msg == WM_INPUT_DEVICE_CHANGE)
            {
                _ProcessDeviceChange(ref lResult, hwnd, msg, wParam, lParam);
                handled = true;
            }
            return lResult;
        }
        // Device change message
        private void _ProcessDeviceChange(ref IntPtr lResult, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            lResult = IntPtr.Zero;

            if ((uint)wParam == GIDC_ARRIVAL) _EnumerateAdd(lParam);
            if ((uint)wParam == GIDC_REMOVAL) _EnumerateRemove(lParam);
        }
        // Get info on a device handle and add it to the internal list
        private bool _EnumerateAdd(IntPtr hDevice)
        {
            uint size = 0;
            byte[] data = new byte[0];

            // Get name string
            if (0 > GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, null, ref size))
                throw new Exception("Bad return value from Win32 function");
            if (size <= 0) return false;
            data = new byte[size];
            if (0 >= GetRawInputDeviceInfo(hDevice, RIDI_DEVICENAME, data, ref size))
                throw new Exception("Bad return value from Win32 function");
            string name = Encoding.ASCII.GetString(data).Trim('\0');

            // Get info struct
            if (0 > GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, null, ref size))
                throw new Exception("Bad return value from Win32 function");
            if (size <= 0) return false;
            data = new byte[size];
            if (0 >= GetRawInputDeviceInfo(hDevice, RIDI_DEVICEINFO, data, ref size))
                throw new Exception("Bad return value from Win32 function");
            var infoHdr = ToStruct<RID_DEVICE_INFO_HEADER>(data);
            var infoHdrSz = Marshal.SizeOf(typeof(RID_DEVICE_INFO_HEADER));
            var infoBytes = new byte[size - infoHdrSz];
            Array.Copy(data, infoHdrSz, infoBytes, 0, infoBytes.Length);

            // Convert to specific info
            object info = null;
            if (infoHdr.dwType == RIM_TYPEMOUSE)
            {
                var raw = ToStruct<RID_DEVICE_INFO_MOUSE>(infoBytes);
                info = new MouseInfo { id = raw.dwId, numberOfButtons = raw.dwNumberOfButtons, sampleRate = raw.dwSampleRate, hasHorizontalWheel = raw.fHasHorizontalWheel };
            }
            if (infoHdr.dwType == RIM_TYPEKEYBOARD)
            {
                var raw = ToStruct<RID_DEVICE_INFO_KEYBOARD>(infoBytes);
                info = new KeyboardInfo { type = raw.dwType, subType = raw.dwSubType, keyboardMode = raw.dwKeyboardMode, numberOfFunctionKeys = raw.dwNumberOfFunctionKeys, numberOfIndicators = raw.dwNumberOfIndicators, numberOfKeysTotal = raw.dwNumberOfKeysTotal };
            }
            if (infoHdr.dwType == RIM_TYPEHID)
            {
                var raw = ToStruct<RID_DEVICE_INFO_HID>(infoBytes);
                info = new HidInfo { productID = raw.dwProductID, vendorID = raw.dwVendorID, versionNumber = raw.dwVersionNumber, usagePage = raw.usUsagePage, usage = raw.usUsage };
            }
            if (info == null) return false;

            // Add to dictionary, trigger events
            var dev = new Device((Device.DeviceType)infoHdr.dwType, name, info);
            if (_devices.ContainsKey(hDevice))
                throw new Exception("Trying to adding device handle that already exists");

            _devices.Add(hDevice, dev);
            DeviceAdded?.Invoke(this, new RawInputEventArgs(_devices[hDevice], null));
            return true;
        }
        // Remove a device handle from the internal list
        private bool _EnumerateRemove(IntPtr hDevice)
        {
            if (_devices.ContainsKey(hDevice))
            {
                DeviceRemoved?.Invoke(this, new RawInputEventArgs(_devices[hDevice], null));
                _devices.Remove(hDevice);
                return true;
            }
            return false;
        }
        // Raw input message
        private void _ProcessInput(ref IntPtr lResult, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            lResult = IntPtr.Zero;

            // Get data packet
            uint size = 0;
            if (0 > GetRawInputData(lParam, RID_INPUT, null, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))))
                throw new Exception("Bad return value from Win32 function");
            if (size <= 0) return;
            var bytes = new byte[size];
            if (0 >= GetRawInputData(lParam, RID_INPUT, bytes, ref size, (uint)Marshal.SizeOf(typeof(RAWINPUTHEADER))))
                throw new Exception("Bad return value from Win32 function");

            // Extract the header and raw data portions
            var header = ToStruct<RAWINPUTHEADER>(bytes);
            if (header.dwSize != size) throw new Exception("Size field mismatch");
            int hdrSize = Marshal.SizeOf(typeof(RAWINPUTHEADER));
            var data = new byte[size - hdrSize];
            Array.Copy(bytes, hdrSize, data, 0, data.Length);

            // Parse data bytes
            object obj = null;
            if (header.dwType == RIM_TYPEMOUSE)
            {
                var raw = ToStruct<RAWMOUSE>(data);
                obj = new MouseData { flags = raw.usFlags, buttons = raw.ulButtons, rawButtons = raw.ulRawButtons, lastX = raw.lLastX, lastY = raw.lLastY, extraInformation = raw.ulExtraInformation };
            }
            if (header.dwType == RIM_TYPEKEYBOARD)
            {
                var raw = ToStruct<RAWKEYBOARD>(data);
                obj = new KeyboardData { makecode = raw.usMakecode, flags = raw.usFlags, reserved = raw.usReserved, vKey = raw.usVKey, message = raw.dwMessage, extraInformation = raw.ulExtraInformation };
            }
            if (header.dwType == RIM_TYPEHID)
            {
                // Hid data contains 2x DWORD header plus variable-length stream of bytes
                var raw = new RAWHID();
                raw.dwSizeHid = BitConverter.ToUInt32(data, 0);
                raw.dwCount = BitConverter.ToUInt32(data, 4);
                int len = (int)(raw.dwSizeHid * raw.dwCount);
                if (len < data.Length - 8) throw new Exception("RawData array length too small to fit requested elements count");
                raw.bRawData = new byte[len];
                Array.Copy(data, 8, raw.bRawData, 0, raw.bRawData.Length);

                var arrays = new byte[raw.dwCount][];
                for (int i = 0; i < arrays.Length; i++)
                {
                    arrays[i] = new byte[raw.dwSizeHid];
                    Array.Copy(raw.bRawData, i * raw.dwSizeHid, arrays[i], 0, arrays[i].Length);
                }
                obj = new HidData { rawData = arrays };
            }

            // IntPtr.Zero means there is no associated device
            if (header.hDevice == IntPtr.Zero)
            {
                // Trigger universal event & null device's event
                if (header.dwType >= 0 && header.dwType <= 2)
                {
                    Device.DeviceType tp = 0;
                    if (header.dwType == RIM_TYPEMOUSE) tp = Device.DeviceType.Mouse;
                    if (header.dwType == RIM_TYPEKEYBOARD) tp = Device.DeviceType.Keyboard;
                    if (header.dwType == RIM_TYPEHID) tp = Device.DeviceType.Hid;
                    var dev = _nullDevices[tp];
                    InputReceived?.Invoke(this, new RawInputEventArgs(dev, obj));
                    dev.RaiseInputReceived(obj);
                }
            }
            // Data does have an associated device
            else
            {
                // Lookup device in dictionary
                if (_devices.ContainsKey(header.hDevice))
                {
                    // Trigger universal event & device object's event
                    var dev = _devices[header.hDevice];
                    InputReceived?.Invoke(this, new RawInputEventArgs(dev, obj));
                    dev.RaiseInputReceived(obj);
                }
            }
        }
        private Dictionary<IntPtr, Device> _devices = new Dictionary<IntPtr, Device>();
        private Dictionary<Device.DeviceType, Device> _nullDevices = new Dictionary<Device.DeviceType, Device>();
        private IntPtr _hwnd = IntPtr.Zero;
        private List<DeviceCollection> _registeredCollections = new List<DeviceCollection>();
    }
}
