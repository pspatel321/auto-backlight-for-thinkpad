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

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Class to manage keyboard backlight operation.
    /// Backlight automatically illuminates with user activity on built-in mouse/keyboard. It stops after a timeout. 
    /// </summary>
    public class AutoKeyboardBacklightController
    {
        /// <summary>
        /// Get/set the timeout after user activity to turn off the backlight
        /// </summary>
        public double InactivityTimeout
        {
            get => _inactivityTimeout;
            set
            {
                if (_inactivityTimeout != value)
                {
                    _inactivityTimeout = value;
                    double tm = _inactivityTimeout / 5.0;
                    if (tm < 0.5) tm = 0.5;
                    if (tm > 5) tm = 5;
                    _timer.Interval = TimeSpan.FromSeconds(tm);
                }
            }
        }
        /// <summary>
        /// Get/set the brightness level used for the On state
        /// </summary>
        public int OnLevel
        {
            get => _onLevel;
            set
            {
                if (value < 0) value = 0;
                if (value > _backlight.Limit) value = _backlight.Limit;
                if (_onLevel != value)
                {
                    _onLevel = value;
                    _EnforceOnLevel(value);
                }
            }
        }
        /// <summary>
        /// Contains info on type of activity that triggered event
        /// </summary>
        public class ActivityEventArgs : EventArgs
        {
            /// <summary>
            /// Types of activities
            /// </summary>
            public enum Activity { OnLevelChanged, UserActivity, UserInactivity, PowerPause, PowerResume };
            /// <summary>
            /// Get the type of activity
            /// </summary>
            public Activity Act { get; }

            public ActivityEventArgs(Activity act)
            {
                Act = act;
            }
        }
        /// <summary>
        /// Event handler type for signaling activity events
        /// </summary>
        /// <param name="sender"><see cref="AutoKeyboardBacklightController"/> instance</param>
        /// <param name="e">data for this event</param>
        public delegate void ActivityEventHandler(object sender, ActivityEventArgs e);

        /// <summary>
        /// Triggered after an impoertant activity occurs
        /// </summary>
        public event ActivityEventHandler Activity;
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
                        _backlight.Write(_shouldBeLevel);

                        _tracker.Start();
                        _capture.Start();
                        _backlight.Start();
                        _backlight.Changed += _Changed;
                        Debug.WriteLine("{0} Timeout enable: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                    }
                    else
                    {
                        _backlight.Changed -= _Changed;
                        _backlight.Stop();
                        _capture.Stop();
                        _tracker.Stop();

                        _shouldBeLevel = 0;
                        _backlight.Write(_shouldBeLevel);
                        Debug.WriteLine("{0} Timeout disable: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                    }
                }
            }
        }
        /// <summary>
        /// Initialize keyboard backlight controller
        /// </summary>
        /// <param name="pm">Access to a prepared <see cref="PowerManagement.PowerManagement"/> object to receive events</param>
        /// <param name="ri">Access to a prepared <see cref="RawInput.RawInput"/> object to receive events</param>
        public AutoKeyboardBacklightController(PowerManagement.PowerManagement pm, RawInput.RawInput ri)
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
            _timer = new DispatcherTimer();
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
                if (_accum >= InactivityTimeout) _LightsOff();
            };
            InactivityTimeout = double.PositiveInfinity;
            _tracker = new PowerTracker(pm);
            _tracker.Resume += (sender, e) =>
            {
                _timer.Start();
                _LightsOn();
                _backlight.Changed += _Changed;

                Debug.WriteLine("{0} Timeout resume: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                Activity?.Invoke(this, new ActivityEventArgs(ActivityEventArgs.Activity.PowerResume));
            };
            _tracker.Pause += (sender, e) =>
            {
                _backlight.Changed -= _Changed;
                _timer.Stop();
                _LightsOff();

                Debug.WriteLine("{0} Timeout pause: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                Activity?.Invoke(this, new ActivityEventArgs(ActivityEventArgs.Activity.PowerPause));
            };
            _backlight = new Backlight();
        }

        private bool _enabled = false;
        private double _inactivityTimeout = double.NaN;
        private int _onLevel = 2;
        private int _shouldBeLevel = 0;
        private double _accum = 0;
        private DispatcherTimer _timer;
        private Capture _capture;
        private PowerTracker _tracker;
        private Backlight _backlight;

        // Change OnLevel from user keyboard shortcut
        private void _Changed(object sender, BacklightEventArgs e)
        {
            if (e.State != _shouldBeLevel)
            {
                _onLevel = e.State;
                _EnforceOnLevel(e.State);
            }
        }
        // Procedure to enforce a new OnLevel
        private void _EnforceOnLevel(int lvl)
        {
            if (lvl > 0)
            {
                _LightsOn();
                _capture.Start();
            }
            else
            {
                _LightsOff();
                _capture.Stop();
            }
            Activity?.Invoke(this, new ActivityEventArgs(ActivityEventArgs.Activity.OnLevelChanged));
            Debug.WriteLine("{0} Timeout change: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
        }
        // On key press, turn on light
        private void _LightsOn()
        {
            if (_shouldBeLevel != _onLevel)
            {
                _shouldBeLevel = _onLevel;
                _backlight.Write(_shouldBeLevel);
            }
            if (!_timer.IsEnabled)
            {
                Debug.WriteLine("{0} Timeout lightsOn: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                Activity?.Invoke(this, new ActivityEventArgs(ActivityEventArgs.Activity.UserActivity));
            }
            _accum = 0;
            _timer.Start();
        }
        // On timeout, turn off light
        private void _LightsOff()
        {
            if (_shouldBeLevel != 0)
            {
                _shouldBeLevel = 0;
                _backlight.Write(_shouldBeLevel);
            }
            if (_timer.IsEnabled)
            {
                Debug.WriteLine("{0} Timeout lightsOff: onLevel={1}", DateTime.Now.TimeOfDay, _onLevel);
                Activity?.Invoke(this, new ActivityEventArgs(ActivityEventArgs.Activity.UserInactivity));
            }
            _timer.Stop();
        }
    }
}
