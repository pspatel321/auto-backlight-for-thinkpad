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
using System.Diagnostics;
using System.Windows.Threading;

namespace Auto_Keyboard_Backlight
{
    /// <summary>
    /// Class to manage operation in Timeout mode.
    /// In this mode, backlight automatically illuminates with user activity on built-in mouse/keyboard. It stops after a timeout. 
    /// </summary>
    public class Timeout
    {
        /// <summary>
        /// Get/set the timeout after user activity in order to turn off the backlight.
        /// </summary>
        public double Seconds
        {
            get => _seconds;
            set
            {
                if (_seconds != value)
                {
                    _seconds = value;
                    double tm = _seconds / 5.0;
                    if (tm < 0.5) tm = 0.5;
                    if (tm > 5) tm = 5;
                    _timer.Interval = TimeSpan.FromSeconds(tm);

                    Debug.WriteLine("{0} Timeout mode Seconds: Seconds={1}", DateTime.Now.TimeOfDay, Seconds);
                }
            }
        }
        /// <summary>
        /// Start processing operations for this mode, same as <see cref="Enabled"/>=true.
        /// </summary>
        public void Start() => Enabled = true;
        /// <summary>
        /// Stop processing operations for this mode, same as <see cref="Enabled"/>=false.
        /// </summary>
        public void Stop() => Enabled = false;
        /// <summary>
        /// Get/set the enabled state, same as <see cref="Start"/>/<see cref="Stop"/>.
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
                        _shouldBeLevel = 0;
                        App.Backlight.Write(_shouldBeLevel);

                        _tracker.Start();
                        _capture.Start();
                        App.Backlight.Changed += _Changed;
                        Debug.WriteLine("{0} Timeout mode enable: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                    }
                    else
                    {
                        App.Backlight.Changed -= _Changed;
                        _capture.Stop();
                        _tracker.Stop();

                        _shouldBeLevel = 0;
                        App.Backlight.Write(_shouldBeLevel);
                        Debug.WriteLine("{0} Timeout mode disable: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                    }
                }
            }
        }
        /// <summary>
        /// Save the onlevel to registry
        /// </summary>
        /// <returns>true on success</returns>
        public bool SaveToRegistry()
        {
            try
            {
                App.RegistryKey.SetValue("onLevel", _onLevel);
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }
        /// <summary>
        /// Get the last saved onlevel from registry
        /// </summary>
        /// <returns>true on success</returns>
        public bool RestoreFromRegistry()
        {
            try
            {
                _onLevel = (int)App.RegistryKey.GetValue("onLevel");
                return true;
            }
            catch (Exception)
            {
            }
            return false;
        }
        /// <summary>
        /// Initialize
        /// </summary>
        /// <param name="pm">Access to a prepared <see cref="PowerManagement.PowerManagement"/> object to receive events</param>
        /// <param name="ri">Access to a prepared <see cref="RawInput.RawInput"/> object to receive events</param>
        public Timeout(PowerManagement.PowerManagement pm, RawInput.RawInput ri)
        {
            _capture = new Capture(ri);
            _capture.CaptureEvent += (sender, e) =>
            {
                if (!_enabled)
                {
                    _capture.Stop();
                    return;
                }

                // Immediately stop, to reduce message load (preserve battery)
                // Will be started again in 1 timer interval
                _capture.Stop();
                _LightsOn();
            };
            Seconds = double.PositiveInfinity;
            _timer.Tick += (sender, e) =>
            {
                if (!_enabled)
                {
                    _timer.Stop();
                    return;
                }

                // Allow another round of capturing input
                _capture.Start();

                // Keyboard light timeout reached
                _accum += _timer.Interval.TotalSeconds;
                if (_accum >= Seconds) _LightsOff();
            };
            _tracker = new Tracker(pm);
            _tracker.Pause += (sender, e) =>
            {
                App.Backlight.Changed -= _Changed;
                _LightsOff();
            };
            _tracker.Resume += (sender, e) => Debug.WriteLine("{0} Timeout mode resume: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
            _tracker.Resume += (sender, e) =>
            {
                _LightsOn();
                App.Backlight.Changed += _Changed;
            };
            _tracker.Pause += (sender, e) => Debug.WriteLine("{0} Timeout mode pause: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
        }

        private bool _enabled = false;
        private double _seconds = double.NaN;
        private int _onLevel = 2;
        private int _shouldBeLevel = 0;
        private double _accum = 0;
        private DispatcherTimer _timer = new DispatcherTimer();
        private Capture _capture;
        private Tracker _tracker;

        // On backlight changed event, update the OnLevel to new user choice
        private void _Changed(object sender, BacklightEventArgs e)
        {
            if (e.State != _shouldBeLevel)
            {
                _onLevel = e.State;
                if (e.State > 0)
                {
                    _LightsOn();
                    _capture.Start();
                }
                else
                {
                    _LightsOff();
                    _capture.Stop();
                }
                Debug.WriteLine("{0} Timeout mode change: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
            }
        }
        // On key press, turn on light
        private void _LightsOn()
        {
            if (_shouldBeLevel != _onLevel)
            {
                _shouldBeLevel = _onLevel;
                App.Backlight.Write(_shouldBeLevel);
            }
            if (!_timer.IsEnabled) Debug.WriteLine("{0} Timeout mode lightsOn: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
            _accum = 0;
            _timer.Start();
        }
        // On timeout, turn off light
        private void _LightsOff()
        {
            if (_shouldBeLevel != 0)
            {
                _shouldBeLevel = 0;
                App.Backlight.Write(_shouldBeLevel);
            }
            if (_timer.IsEnabled) Debug.WriteLine("{0} Timeout mode lightsOff: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
            _timer.Stop();
        }
    }
}
