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
using System.Runtime.InteropServices;
using BOOL = System.Boolean;
using BYTE = System.Byte;
using DWORD = System.UInt32;
using HANDLE = System.IntPtr;
using HWND = System.IntPtr;
using LONG = System.Int32;
using UINT = System.UInt32;
using ULONG = System.UInt32;
using USHORT = System.UInt16;
using WPARAM = System.IntPtr;

namespace Auto_Keyboard_Backlight.RawInput
{
    /// <summary>
    /// Backend in order to use unmanaged Win32 <a href="https://docs.microsoft.com/en-us/windows/win32/inputdev/raw-input">Raw Input API</a> from User32.dll
    /// </summary>
    internal static class NativeMethods
    {
        #region consts

        public const uint GIDC_REMOVAL = 2;
        public const uint GIDC_ARRIVAL = 1;

        public const uint RIDI_PREPARSEDDATA = 0x20000005;
        public const uint RIDI_DEVICENAME = 0x20000007;
        public const uint RIDI_DEVICEINFO = 0x2000000b;

        public const uint RID_INPUT = 0x10000003;
        public const uint RID_HEADER = 0x10000005;

        public const uint RIM_INPUT = 0;
        public const uint RIM_INPUTSINK = 1;

        public const uint RIM_TYPEMOUSE = 0;
        public const uint RIM_TYPEKEYBOARD = 1;
        public const uint RIM_TYPEHID = 2;
        public const uint RIM_TYPEMAX = 2;

        public const uint MOUSE_MOVE_RELATIVE = 0;
        public const uint MOUSE_MOVE_ABSOLUTE = 1;
        public const uint MOUSE_VIRTUAL_DESKTOP = 0x02;
        public const uint MOUSE_ATTRIBUTES_CHANGED = 0x04;
        public const uint MOUSE_MOVE_NOCOALESCE = 0x08;

        public const uint RI_MOUSE_LEFT_BUTTON_DOWN = 0x0001;
        public const uint RI_MOUSE_LEFT_BUTTON_UP = 0x0002;
        public const uint RI_MOUSE_RIGHT_BUTTON_DOWN = 0x0004;
        public const uint RI_MOUSE_RIGHT_BUTTON_UP = 0x0008;
        public const uint RI_MOUSE_MIDDLE_BUTTON_DOWN = 0x0010;
        public const uint RI_MOUSE_MIDDLE_BUTTON_UP = 0x0020;
        public const uint RI_MOUSE_WHEEL = 0x0400;
        public const uint RI_MOUSE_HWHEEL = 0x0800;
        public const uint RI_MOUSE_BUTTON_1_DOWN = RI_MOUSE_LEFT_BUTTON_DOWN;
        public const uint RI_MOUSE_BUTTON_1_UP = RI_MOUSE_LEFT_BUTTON_UP;
        public const uint RI_MOUSE_BUTTON_2_DOWN = RI_MOUSE_RIGHT_BUTTON_DOWN;
        public const uint RI_MOUSE_BUTTON_2_UP = RI_MOUSE_RIGHT_BUTTON_UP;
        public const uint RI_MOUSE_BUTTON_3_DOWN = RI_MOUSE_MIDDLE_BUTTON_DOWN;
        public const uint RI_MOUSE_BUTTON_3_UP = RI_MOUSE_MIDDLE_BUTTON_UP;
        public const uint RI_MOUSE_BUTTON_4_DOWN = 0x0040;
        public const uint RI_MOUSE_BUTTON_4_UP = 0x0080;
        public const uint RI_MOUSE_BUTTON_5_DOWN = 0x0100;
        public const uint RI_MOUSE_BUTTON_5_UP = 0x0200;

        public const uint KEYBOARD_OVERRUN_MAKE_CODE = 0xFF;

        public const uint RI_KEY_MAKE = 0;
        public const uint RI_KEY_BREAK = 1;
        public const uint RI_KEY_E0 = 2;
        public const uint RI_KEY_E1 = 4;
        public const uint RI_KEY_TERMSRV_SET_LED = 8;
        public const uint RI_KEY_TERMSRV_SHADOW = 0x10;

        public const int WM_INPUT = 0x00FF;
        public const int WM_INPUT_DEVICE_CHANGE = 0x00FE;
        public const int WM_KEYFIRST = 0x0100;
        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_DEADCHAR = 0x0103;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_SYSCHAR = 0x0106;
        public const int WM_SYSDEADCHAR = 0x0107;
        public const int WM_UNICHAR = 0x0109;
        public const int WM_KEYLAST = 0x0109;
        public const int UNICODE_NOCHAR = 0xFFFF;

        public const uint VK_LBUTTON = 0x01;
        public const uint VK_RBUTTON = 0x02;
        public const uint VK_CANCEL = 0x03;
        public const uint VK_MBUTTON = 0x04;

        public const uint VK_XBUTTON1 = 0x05;
        public const uint VK_XBUTTON2 = 0x06;

        public const uint VK_BACK = 0x08;
        public const uint VK_TAB = 0x09;

        public const uint VK_CLEAR = 0x0C;
        public const uint VK_RETURN = 0x0D;

        public const uint VK_SHIFT = 0x10;
        public const uint VK_CONTROL = 0x11;
        public const uint VK_MENU = 0x12;
        public const uint VK_PAUSE = 0x13;
        public const uint VK_CAPITAL = 0x14;

        public const uint VK_KANA = 0x15;
        public const uint VK_HANGEUL = 0x15;
        public const uint VK_HANGUL = 0x15;

        public const uint VK_JUNJA = 0x17;
        public const uint VK_FINAL = 0x18;
        public const uint VK_HANJA = 0x19;
        public const uint VK_KANJI = 0x19;

        public const uint VK_ESCAPE = 0x1B;

        public const uint VK_CONVERT = 0x1C;
        public const uint VK_NONCONVERT = 0x1D;
        public const uint VK_ACCEPT = 0x1E;
        public const uint VK_MODECHANGE = 0x1F;

        public const uint VK_SPACE = 0x20;
        public const uint VK_PRIOR = 0x21;
        public const uint VK_NEXT = 0x22;
        public const uint VK_END = 0x23;
        public const uint VK_HOME = 0x24;
        public const uint VK_LEFT = 0x25;
        public const uint VK_UP = 0x26;
        public const uint VK_RIGHT = 0x27;
        public const uint VK_DOWN = 0x28;
        public const uint VK_SELECT = 0x29;
        public const uint VK_PRuint = 0x2A;
        public const uint VK_EXECUTE = 0x2B;
        public const uint VK_SNAPSHOT = 0x2C;
        public const uint VK_INSERT = 0x2D;
        public const uint VK_DELETE = 0x2E;
        public const uint VK_HELP = 0x2F;

        public const uint VK_LWIN = 0x5B;
        public const uint VK_RWIN = 0x5C;
        public const uint VK_APPS = 0x5D;

        public const uint VK_SLEEP = 0x5F;

        public const uint VK_NUMPAD0 = 0x60;
        public const uint VK_NUMPAD1 = 0x61;
        public const uint VK_NUMPAD2 = 0x62;
        public const uint VK_NUMPAD3 = 0x63;
        public const uint VK_NUMPAD4 = 0x64;
        public const uint VK_NUMPAD5 = 0x65;
        public const uint VK_NUMPAD6 = 0x66;
        public const uint VK_NUMPAD7 = 0x67;
        public const uint VK_NUMPAD8 = 0x68;
        public const uint VK_NUMPAD9 = 0x69;
        public const uint VK_MULTIPLY = 0x6A;
        public const uint VK_ADD = 0x6B;
        public const uint VK_SEPARATOR = 0x6C;
        public const uint VK_SUBTRACT = 0x6D;
        public const uint VK_DECIMAL = 0x6E;
        public const uint VK_DIVIDE = 0x6F;
        public const uint VK_F1 = 0x70;
        public const uint VK_F2 = 0x71;
        public const uint VK_F3 = 0x72;
        public const uint VK_F4 = 0x73;
        public const uint VK_F5 = 0x74;
        public const uint VK_F6 = 0x75;
        public const uint VK_F7 = 0x76;
        public const uint VK_F8 = 0x77;
        public const uint VK_F9 = 0x78;
        public const uint VK_F10 = 0x79;
        public const uint VK_F11 = 0x7A;
        public const uint VK_F12 = 0x7B;
        public const uint VK_F13 = 0x7C;
        public const uint VK_F14 = 0x7D;
        public const uint VK_F15 = 0x7E;
        public const uint VK_F16 = 0x7F;
        public const uint VK_F17 = 0x80;
        public const uint VK_F18 = 0x81;
        public const uint VK_F19 = 0x82;
        public const uint VK_F20 = 0x83;
        public const uint VK_F21 = 0x84;
        public const uint VK_F22 = 0x85;
        public const uint VK_F23 = 0x86;
        public const uint VK_F24 = 0x87;

        public const uint VK_NAVIGATION_VIEW = 0x88;
        public const uint VK_NAVIGATION_MENU = 0x89;
        public const uint VK_NAVIGATION_UP = 0x8A;
        public const uint VK_NAVIGATION_DOWN = 0x8B;
        public const uint VK_NAVIGATION_LEFT = 0x8C;
        public const uint VK_NAVIGATION_RIGHT = 0x8D;
        public const uint VK_NAVIGATION_ACCEPT = 0x8E;
        public const uint VK_NAVIGATION_CANCEL = 0x8F;

        public const uint VK_NUMLOCK = 0x90;
        public const uint VK_SCROLL = 0x91;

        public const uint VK_OEM_NEC_EQUAL = 0x92;

        public const uint VK_OEM_FJ_JISHO = 0x92;
        public const uint VK_OEM_FJ_MASSHOU = 0x93;
        public const uint VK_OEM_FJ_TOUROKU = 0x94;
        public const uint VK_OEM_FJ_LOYA = 0x95;
        public const uint VK_OEM_FJ_ROYA = 0x96;

        public const uint VK_LSHIFT = 0xA0;
        public const uint VK_RSHIFT = 0xA1;
        public const uint VK_LCONTROL = 0xA2;
        public const uint VK_RCONTROL = 0xA3;
        public const uint VK_LMENU = 0xA4;
        public const uint VK_RMENU = 0xA5;

        public const uint VK_BROWSER_BACK = 0xA6;
        public const uint VK_BROWSER_FORWARD = 0xA7;
        public const uint VK_BROWSER_REFRESH = 0xA8;
        public const uint VK_BROWSER_STOP = 0xA9;
        public const uint VK_BROWSER_SEARCH = 0xAA;
        public const uint VK_BROWSER_FAVORITES = 0xAB;
        public const uint VK_BROWSER_HOME = 0xAC;

        public const uint VK_VOLUME_MUTE = 0xAD;
        public const uint VK_VOLUME_DOWN = 0xAE;
        public const uint VK_VOLUME_UP = 0xAF;
        public const uint VK_MEDIA_NEXT_TRACK = 0xB0;
        public const uint VK_MEDIA_PREV_TRACK = 0xB1;
        public const uint VK_MEDIA_STOP = 0xB2;
        public const uint VK_MEDIA_PLAY_PAUSE = 0xB3;
        public const uint VK_LAUNCH_MAIL = 0xB4;
        public const uint VK_LAUNCH_MEDIA_SELECT = 0xB5;
        public const uint VK_LAUNCH_APP1 = 0xB6;
        public const uint VK_LAUNCH_APP2 = 0xB7;

        public const uint VK_OEM_1 = 0xBA;
        public const uint VK_OEM_PLUS = 0xBB;
        public const uint VK_OEM_COMMA = 0xBC;
        public const uint VK_OEM_MINUS = 0xBD;
        public const uint VK_OEM_PERIOD = 0xBE;
        public const uint VK_OEM_2 = 0xBF;
        public const uint VK_OEM_3 = 0xC0;

        public const uint VK_GAMEPAD_A = 0xC3;
        public const uint VK_GAMEPAD_B = 0xC4;
        public const uint VK_GAMEPAD_X = 0xC5;
        public const uint VK_GAMEPAD_Y = 0xC6;
        public const uint VK_GAMEPAD_RIGHT_SHOULDER = 0xC7;
        public const uint VK_GAMEPAD_LEFT_SHOULDER = 0xC8;
        public const uint VK_GAMEPAD_LEFT_TRIGGER = 0xC9;
        public const uint VK_GAMEPAD_RIGHT_TRIGGER = 0xCA;
        public const uint VK_GAMEPAD_DPAD_UP = 0xCB;
        public const uint VK_GAMEPAD_DPAD_DOWN = 0xCC;
        public const uint VK_GAMEPAD_DPAD_LEFT = 0xCD;
        public const uint VK_GAMEPAD_DPAD_RIGHT = 0xCE;
        public const uint VK_GAMEPAD_MENU = 0xCF;
        public const uint VK_GAMEPAD_VIEW = 0xD0;
        public const uint VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1;
        public const uint VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2;
        public const uint VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3;
        public const uint VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4;
        public const uint VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5;
        public const uint VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6;
        public const uint VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7;
        public const uint VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8;
        public const uint VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9;
        public const uint VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA;

        public const uint VK_OEM_4 = 0xDB;
        public const uint VK_OEM_5 = 0xDC;
        public const uint VK_OEM_6 = 0xDD;
        public const uint VK_OEM_7 = 0xDE;
        public const uint VK_OEM_8 = 0xDF;

        public const uint VK_OEM_AX = 0xE1;
        public const uint VK_OEM_102 = 0xE2;
        public const uint VK_ICO_HELP = 0xE3;
        public const uint VK_ICO_00 = 0xE4;

        public const uint VK_PROCESSKEY = 0xE5;

        public const uint VK_ICO_CLEAR = 0xE6;

        public const uint VK_PACKET = 0xE7;

        public const uint VK_OEM_RESET = 0xE9;
        public const uint VK_OEM_JUMP = 0xEA;
        public const uint VK_OEM_PA1 = 0xEB;
        public const uint VK_OEM_PA2 = 0xEC;
        public const uint VK_OEM_PA3 = 0xED;
        public const uint VK_OEM_WSCTRL = 0xEE;
        public const uint VK_OEM_CUSEL = 0xEF;
        public const uint VK_OEM_ATTN = 0xF0;
        public const uint VK_OEM_FINISH = 0xF1;
        public const uint VK_OEM_COPY = 0xF2;
        public const uint VK_OEM_AUTO = 0xF3;
        public const uint VK_OEM_ENLW = 0xF4;
        public const uint VK_OEM_BACKTAB = 0xF5;

        public const uint VK_ATTN = 0xF6;
        public const uint VK_CRSEL = 0xF7;
        public const uint VK_EXSEL = 0xF8;
        public const uint VK_EREOF = 0xF9;
        public const uint VK_PLAY = 0xFA;
        public const uint VK_ZOOM = 0xFB;
        public const uint VK_NONAME = 0xFC;
        public const uint VK_PA1 = 0xFD;
        public const uint VK_OEM_CLEAR = 0xFE;

        #endregion consts

        #region structs

#pragma warning disable CS0649 

        public struct RAWHID
        {
            public DWORD dwSizeHid;
            public DWORD dwCount;
            public BYTE[] bRawData;
        }
        public struct RAWINPUTDEVICE
        {
            public USHORT usUsagePage;
            public USHORT usUsage;
            public DWORD dwFlags;
            public HWND hwndTarget;
        }
        public struct RAWINPUTDEVICELIST
        {
            public HANDLE hDevice;
            public DWORD dwType;
        }
        public struct RAWINPUTHEADER
        {
            public DWORD dwType;
            public DWORD dwSize;
            public HANDLE hDevice;
            public WPARAM wParam;
        }
        public struct RAWKEYBOARD
        {
            public USHORT usMakecode;
            public USHORT usFlags;
            public USHORT usReserved;
            public USHORT usVKey;
            public DWORD dwMessage;
            public ULONG ulExtraInformation;
        }
        public struct RAWMOUSE
        {
            public USHORT usFlags;
            public ULONG ulButtons;
            public ULONG ulRawButtons;
            public LONG lLastX;
            public LONG lLastY;
            public ULONG ulExtraInformation;
        }
        public struct RID_DEVICE_INFO_HEADER
        {
            public DWORD cbSize;
            public DWORD dwType;
        }
        public struct RID_DEVICE_INFO_HID
        {
            public DWORD dwVendorID;
            public DWORD dwProductID;
            public DWORD dwVersionNumber;
            public USHORT usUsagePage;
            public USHORT usUsage;
        }
        public struct RID_DEVICE_INFO_KEYBOARD
        {
            public DWORD dwType;
            public DWORD dwSubType;
            public DWORD dwKeyboardMode;
            public DWORD dwNumberOfFunctionKeys;
            public DWORD dwNumberOfIndicators;
            public DWORD dwNumberOfKeysTotal;
        }
        public struct RID_DEVICE_INFO_MOUSE
        {
            public DWORD dwId;
            public DWORD dwNumberOfButtons;
            public DWORD dwSampleRate;
            public BOOL fHasHorizontalWheel;
        }

#pragma warning restore CS0649 

        #endregion structs

        #region helpers

        internal static byte[] ToBytes<T>(T t)
        {
            if (!typeof(T).IsValueType)
                throw new Exception("Type must be a ValueType (struct)");

            var len = Marshal.SizeOf(typeof(T));
            var ptr = Marshal.AllocHGlobal(len);
            var bytes = new byte[len];
            Marshal.StructureToPtr(t, ptr, false);
            Marshal.Copy(ptr, bytes, 0, len);
            Marshal.FreeHGlobal(ptr);
            return bytes;
        }

        internal static T ToStruct<T>(byte[] bytes)
        {
            if (!typeof(T).IsValueType)
                throw new Exception("Type must be a ValueType (struct)");

            var len = Marshal.SizeOf(typeof(T));
            if (bytes.Length < len)
                throw new ArgumentException("Not enough bytes in input array");
            var ptr = Marshal.AllocHGlobal(len);
            Marshal.Copy(bytes, 0, ptr, len);
            T t = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return t;
        }

        #endregion helpers

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputBuffer([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] byte[] pData, ref DWORD pcbSize, DWORD cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputData(HANDLE hRawInput, UINT uiCommand, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out] byte[] pData, ref DWORD pcbSize, DWORD cbSizeHeader);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputDeviceInfo(HANDLE hDevice, UINT uiCommand, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3), Out] byte[] pData, ref DWORD pcbSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRawInputDeviceList([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] RAWINPUTDEVICELIST[] pRawInputDeviceList, ref UINT puiNumDevices, DWORD cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern uint GetRegisteredRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] RAWINPUTDEVICE[] pRawInputDevices, ref UINT puiNumDevices, DWORD cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterRawInputDevices([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), In] RAWINPUTDEVICE[] pRawInputDevices, UINT uiNumDevices, DWORD cbSize);
    }

    public partial struct DeviceCollection
    {
        [Flags]
        public enum Flag
        {
            RIDEV_REMOVE = 0x00000001,
            RIDEV_EXCLUDE = 0x00000010,
            RIDEV_PAGEONLY = 0x00000020,
            RIDEV_NOLEGACY = 0x00000030,
            RIDEV_INPUTSINK = 0x00000100,
            RIDEV_CAPTUREMOUSE = 0x00000200,
            RIDEV_NOHOTKEYS = 0x00000200,
            RIDEV_APPKEYS = 0x00000400,
            RIDEV_EXINPUTSINK = 0x00001000,
            RIDEV_DEVNOTIFY = 0x00002000,
        }
    }
}