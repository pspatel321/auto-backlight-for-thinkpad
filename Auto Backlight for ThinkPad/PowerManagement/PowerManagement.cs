using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Auto_Backlight_for_ThinkPad.PowerManagement
{
    using static NativeMethods;

    /// <summary>
    /// Event data describing the payload of a <see cref="PBT_POWERSETTINGCHANGE"/> message.
    /// </summary>
    public class PowerGuidEventArgs : EventArgs
    {
        /// <summary>
        /// Parsed form of data after <see cref="PowerGuid.ParseFunc"/> is applied to the raw data bytes. If <see cref="PowerGuid.ParseFunc"/> is null, this is a simple byte[].
        /// </summary>
        public object Data { get; }

        public PowerGuidEventArgs(object data)
        {
            Data = data;
        }
    }
    /// <summary>
    /// Event handler type triggered when a specific Guid is changing through a <see cref="PBT_POWERSETTINGCHANGE"/> message.
    /// </summary>
    /// <param name="sender"><see cref="PowerGuid"/> object responsible for triggering the event</param>
    /// <param name="e">Payload associated with the event</param>
    public delegate void PowerGuidEventHandler(object sender, PowerGuidEventArgs e);

    /// <summary>
    /// Provides an object-interface to the various Guids and their change-events supported by Power Management api.
    /// </summary>
    public partial class PowerGuid
    {
        /// <summary>
        /// The Guid number
        /// </summary>
        public Guid Guid { get; }
        /// <summary>
        /// Optional function to parse the raw bytes in payload to a more useful form
        /// </summary>
        public Func<byte[], object> ParseFunc { get; set; }
        /// <summary>
        /// Event triggered whenever a <see cref="PBT_POWERSETTINGCHANGE"/> message arrives with matching <see cref="Guid"/>
        /// </summary>
        public event PowerGuidEventHandler Notified;

        public PowerGuid(Guid guid, Func<byte[], object> parseFunc = null)
        {
            Guid = guid;
            ParseFunc = parseFunc;
        }
        public void RaiseNotified(object data) => Notified?.Invoke(this, new PowerGuidEventArgs(data));
    }

    /// <summary>
    /// An invisible pseudo-element that can be inserted in a WPF window to wrap accesses to the Win32 Power Management messaging API.
    /// It adds a hook-function for the host window's WndProc in order to access Win32 messages.
    /// </summary>
    public class PowerManagement : FrameworkElement
    {
        /// <summary>
        /// A special-case PowerGuid object to handle the Suspend event as if its just another Guid-based message
        /// User may specify this instance in <see cref="RegisterGuids(IEnumerable{PowerGuid})"/> and add handlers to its <see cref="PowerGuid.Notified"/> event.
        /// </summary>
        public PowerGuid SuspendPowerGuid { get { return _suspendPowerGuid; } }
        /// <summary>
        /// A special-case PowerGuid object to handle the Resume event as if its just another Guid-based message
        /// User may specify this instance in <see cref="RegisterGuids(IEnumerable{PowerGuid})"/> and add handlers to its <see cref="PowerGuid.Notified"/> event.
        /// </summary>
        public PowerGuid ResumePowerGuid { get { return _resumePowerGuid; } }
        /// <summary>
        /// Get list of currently registered PowerGuid objects that are receiving messages
        /// </summary>
        public IEnumerable<PowerGuid> RegisteredGuids
        {
            get
            {
                if (_suspendHandle != IntPtr.Zero)
                    yield return _suspendPowerGuid;
                if (_resumeHandle != IntPtr.Zero)
                    yield return _resumePowerGuid;
                foreach (PowerGuid g in _guids.Values)
                    yield return g;
            }
        }
        /// <summary>
        /// Register new PowerGuid to start receiving messages
        /// </summary>
        /// <param name="guids">PowerGuid to register</param>
        public void RegisterGuid(PowerGuid guid) => RegisterGuids(new PowerGuid[] { guid });
        /// <summary>
        /// Register new PowerGuids to start receiving messages
        /// </summary>
        /// <param name="guids">PowerGuids to register</param>
        public void RegisterGuids(IEnumerable<PowerGuid> guids)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            foreach (PowerGuid g in guids)
            {
                IntPtr handle = IntPtr.Zero;
                // For suspend/resume, register the combined notification if either are given as input
                if (g == SuspendPowerGuid || g == ResumePowerGuid)
                {
                    // Actually register if neither are already enabled
                    if (_suspendHandle == IntPtr.Zero && _resumeHandle == IntPtr.Zero)
                    {
                        handle = RegisterSuspendResumeNotification(_hwnd, 0);
                        if (IntPtr.Zero == handle)
                            throw new Exception("Bad return value from Win32 function");
                    }
                    // One is already registered, just borrow that handle
                    else
                    {
                        if (g == SuspendPowerGuid) handle = _resumeHandle;
                        if (g == ResumePowerGuid) handle = _suspendHandle;
                    }
                    if (g == SuspendPowerGuid) _suspendHandle = handle;
                    if (g == ResumePowerGuid) _resumeHandle = handle;
                    continue;
                }
                handle = RegisterPowerSettingNotification(_hwnd, g.Guid, 0);
                if (IntPtr.Zero == handle)
                    throw new Exception("Bad return value from Win32 function");
                _guids[handle] = g;
            }
        }
        /// <summary>
        /// Unregister PowerGuid to stop receiving messages
        /// </summary>
        /// <param name="guids">PowerGuid to unregister</param>
        public void UnregisterGuid(PowerGuid guid) => UnregisterGuids(new PowerGuid[] { guid });
        /// <summary>
        /// Unregister PowerGuids to stop receiving messages
        /// </summary>
        /// <param name="guids">PowerGuids to unregister, default = unregister all</param>
        public void UnregisterGuids(IEnumerable<PowerGuid> guids = null)
        {
            if (_hwnd == IntPtr.Zero) throw new Exception("Window not ready");
            if (guids == null) guids = RegisteredGuids.ToArray();
            foreach (PowerGuid g in guids)
            {
                IntPtr handle = IntPtr.Zero;
                // For suspend/resume, only unregister after both are given as input
                if (g == SuspendPowerGuid || g == ResumePowerGuid)
                {
                    if (g == SuspendPowerGuid)
                    {
                        handle = _suspendHandle;
                        _suspendHandle = IntPtr.Zero;
                    }
                    if (g == ResumePowerGuid)
                    {
                        handle = _resumeHandle;
                        _resumeHandle = IntPtr.Zero;
                    }
                    // Actually unregister only if both are disabled
                    if (_suspendHandle == IntPtr.Zero && _resumeHandle == IntPtr.Zero)
                    {
                        if (!UnregisterSuspendResumeNotification(handle))
                            throw new Exception("Bad return value from Win32 function");
                    }
                    continue;
                }
                if (_guids.ContainsValue(g))
                {
                    foreach (var kv in _guids)
                        if (kv.Value == g)
                            handle = kv.Key;

                    if (!UnregisterPowerSettingNotification(handle))
                        throw new Exception("Bad return value from Win32 function");
                    _guids.Remove(handle);
                }
            }
        }
        /// <summary>
        /// Initialize window access
        /// </summary>
        public PowerManagement() : base()
        {
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
            if (msg == WM_POWERBROADCAST)
            {
                if ((int)wParam == PBT_APMSUSPEND) _ProcessSuspend(ref lResult, hwnd, msg, wParam, lParam);
                if ((int)wParam == PBT_APMRESUMESUSPEND) _ProcessResume(ref lResult, hwnd, msg, wParam, lParam);
                if ((int)wParam == PBT_POWERSETTINGCHANGE) _ProcessPowerSettingChange(ref lResult, hwnd, msg, wParam, lParam);
                handled = true;
            }
            return lResult;
        }
        // Suspend event message
        private void _ProcessSuspend(ref IntPtr lResult, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            lResult = IntPtr.Zero;
            if (_suspendHandle != IntPtr.Zero)
                SuspendPowerGuid.RaiseNotified(null);
        }
        // Resume event message
        private void _ProcessResume(ref IntPtr lResult, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            lResult = IntPtr.Zero;
            if (_resumeHandle != IntPtr.Zero)
                ResumePowerGuid.RaiseNotified(null);
        }
        // Generic power setting change message
        private void _ProcessPowerSettingChange(ref IntPtr lResult, IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam)
        {
            lResult = IntPtr.Zero;

            // Get the structure from the message
            var power = new POWERBROADCAST_SETTING();
            power.guidPowerSetting = (Guid)Marshal.PtrToStructure(lParam, typeof(Guid));
            power.dwDataLength = (uint)Marshal.PtrToStructure(new IntPtr(lParam.ToInt64() + Marshal.SizeOf(typeof(Guid))), typeof(uint));
            power.bData = new byte[power.dwDataLength];
            Marshal.Copy(new IntPtr(lParam.ToInt64() + Marshal.SizeOf(typeof(Guid))) + Marshal.SizeOf(typeof(uint)), power.bData, 0, power.bData.Length);

            // Search internal list, raise event
            var pg = _guids.Where((i) => i.Value.Guid == power.guidPowerSetting).First().Value;
            pg.RaiseNotified(pg.ParseFunc != null ? pg.ParseFunc(power.bData) : power.bData);
        }
        private IntPtr _hwnd = IntPtr.Zero;
        private Dictionary<IntPtr, PowerGuid> _guids = new Dictionary<IntPtr, PowerGuid>();
        private PowerGuid _suspendPowerGuid = new PowerGuid(default);
        private IntPtr _suspendHandle = IntPtr.Zero;
        private PowerGuid _resumePowerGuid = new PowerGuid(default);
        private IntPtr _resumeHandle = IntPtr.Zero;
    }
}
