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
using System.Management;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Class to manage Auto Screen Brightness operation.
    /// Retrieves ambient light level periodically from webcam, maps it to a desired screen brightness level
    /// </summary>
    public class AutoScreenBrightnessController
    {
        /// <summary>
        /// Set the brightness slider through an awaitable Task
        /// </summary>
        /// <param name="br">Brightness value, 0 to 100</param>
        /// <returns>Task (awaitable)</returns>
        public Task SetBrightnessSlider(byte br)
        {
            return Task.Run(() =>
            {
                if (br < 0) br = 0;
                if (br > 100) br = 100;
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope("root\\WMI"), new SelectQuery("WmiMonitorBrightnessMethods")))
                using (ManagementObjectCollection objectCollection = searcher.Get())
                    foreach (ManagementObject mObj in objectCollection)
                        mObj.InvokeMethod("WmiSetBrightness", new object[] { 0, br });
            });
        }
        /// <summary>
        /// Get the brightness slider through an awaitable Task
        /// </summary>
        /// <returns>Task (awaitable)</returns>
        public Task<byte> GetBrightnessSlider()
        {
            return Task.Run(() =>
            {
                byte br = 0;
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(new ManagementScope("root\\WMI"), new SelectQuery("WmiMonitorBrightness")))
                using (ManagementObjectCollection objectCollection = searcher.Get())
                    foreach (ManagementObject mObj in objectCollection)
                        br = (byte)mObj.Properties["CurrentBrightness"].Value;
                return br;
            });
        }
        /// <summary>
        /// Get/set period for automatically polling ambient light level and outputing screen brightness level
        /// </summary>
        public double PollPeriod
        {
            get => _pollPeriod;
            set
            {
                if (_pollPeriod != value)
                {
                    _pollPeriod = value;
                    double tm = _pollPeriod;
                    if (tm < 1) tm = 1;
                    if (tm > int.MaxValue / 1000.0) tm = int.MaxValue / 1000.0;
                    _timer.Interval = TimeSpan.FromSeconds(tm);
                }
            }
        }
        /// <summary>
        /// Enable the hotkey
        /// </summary>
        public bool HotKeyEnabled
        {
            get => _hotKeyEnabled;
            set
            {
                if (_hotKeyEnabled != value)
                {
                    _hotKeyEnabled = value;
                    HotKey hk = _hke.HotKey;

                    // Re-register the hotkey if it is valid
                    if (hk != null)
                        if (hk.Key != System.Windows.Input.Key.None)
                            if (_hotKeyEnabled) _ghk.RegisterHotKey(_hke);
                            else _ghk.UnregisterHotKeys();
                }
            }
        }
        /// <summary>
        /// Get/set the hotkey
        /// </summary>
        public HotKey HotKey
        {
            get => _hke.HotKey;
            set
            {
                if (_hke.HotKey != value)
                {
                    _hke = new HotKeyEvent(value);
                    _hke.Triggered += (sender, e) => { if (Enabled) _ = Retrigger(); };
                    HotKey hk = _hke.HotKey;

                    // Re-register the hotkey if it is valid
                    if (hk != null)
                        if (hk.Key != System.Windows.Input.Key.None)
                            if (_hotKeyEnabled) _ghk.RegisterHotKey(_hke);
                            else _ghk.UnregisterHotKeys();
                }
            }
        }
        /// <summary>
        /// Get/set list of calibration points for calculating screen brightness output
        /// </summary>
        public List<DataPoint> LearnedPoints
        {
            get => _learnedPoints;
            set
            {
                if (_learnedPoints != value)
                {
                    _learnedPoints = value;
                    Calibration.CreateSmoothCurve(_learnedPoints, _smoothedPoints, _curvature);
                }
            }
        }
        /// <summary>
        /// Get/set the curvature parameter for smoothed spline generation
        /// </summary>
        public double Curvature
        {
            get => _curvature;
            set
            {
                if (_curvature != value)
                {
                    _curvature = value;
                    Calibration.CreateSmoothCurve(_learnedPoints, _smoothedPoints, _curvature);
                }
            }
        }
        /// <summary>
        /// Contains data for <see cref="BrightnessChanged"/> event
        /// </summary>
        public class BrightnessChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Intensity input (0 to 1)
            /// </summary>
            public double Intensity { get; }
            /// <summary>
            /// Brightness output (0 to 1)
            /// </summary>
            public double Brightness { get; }

            public BrightnessChangedEventArgs(double intensity, double brightness)
            {
                Intensity = intensity;
                Brightness = brightness;
            }
        }
        /// <summary>
        /// Event handler type for signaling brightness changes occurred
        /// </summary>
        /// <param name="sender"><see cref="AutoScreenBrightnessController"/> instance</param>
        /// <param name="e">Input and output of the last brightness operation</param>
        public delegate void BrightnessChangedEventHandler(object sender, BrightnessChangedEventArgs e);
        /// <summary>
        /// Triggered after ambient light level is sampled and used to change the screen brightness
        /// </summary>
        public event BrightnessChangedEventHandler BrightnessChanged;
        /// <summary>
        /// Start processing operations for this mode, same as <see cref="Enabled"/>=true.
        /// </summary>
        public void Start(bool trigNow = true)
        {
            _timer.Start();
            if (trigNow) _ = Retrigger();
        }
        /// <summary>
        /// Stop processing operations for this mode, same as <see cref="Enabled"/>=false.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }
        /// <summary>
        /// Enable the timer for periodically sampling ambient light level and changing screen brightness
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled != value)
                {
                    _enabled = value;
                    if (value) Start();
                    else Stop();
                }
            }
        }
        /// <summary>
        /// Manually cause a screen brightness refresh rather than waiting for timer to tick
        /// </summary>
        public async Task<Tuple<double, double>> Retrigger()
        {
            double intensity = await _webCam.Trigger();
            double br = Calibration.Interpolate(_smoothedPoints, intensity);
            if (!double.IsNaN(br))
                await SetBrightnessSlider((byte)Math.Round(br * 100.0));
            br = await GetBrightnessSlider() / 100.0;

            BrightnessChanged?.Invoke(this, new BrightnessChangedEventArgs(intensity, br));
            return new Tuple<double, double>(intensity, br);
        }

        /// <summary>
        /// Initialize screen brightness controller
        /// </summary>
        /// <param name="ghk">Access to a prepared <see cref="GlobalHotKey"/> object that will recieve hotkey messages</param>
        public AutoScreenBrightnessController(GlobalHotKey ghk)
        {
            _ghk = ghk;
            HotKey = new HotKey();

            _timer = new DispatcherTimer();
            _timer.Tick += (sender, ev) =>
            {
                if (Enabled) _ = Retrigger();
            };
            PollPeriod = double.PositiveInfinity;
            _webCam = new WebCam();
        }

        private bool _enabled = false;
        private double _pollPeriod = double.NaN;
        private bool _hotKeyEnabled = false;
        private List<DataPoint> _smoothedPoints = new List<DataPoint>();
        private List<DataPoint> _learnedPoints = new List<DataPoint>();
        private DispatcherTimer _timer;
        private WebCam _webCam;
        private GlobalHotKey _ghk;
        private HotKeyEvent _hke = new HotKeyEvent(null);
        private double _curvature = 0.5;
    }
}
