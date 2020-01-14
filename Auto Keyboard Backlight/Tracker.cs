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

using Auto_Keyboard_Backlight.PowerManagement;
using System;
using System.Collections.Generic;

namespace Auto_Keyboard_Backlight
{
    /// <summary>
    /// Track enter/exit of the computer power states (suspend/resume, lid close/open, display off/on).
    /// These transitions are monitored because the embedded controller (presumably) auto turns-off the light.
    /// </summary>
    public class Tracker
    {
        /// <summary>
        /// Triggered when computer entered suspend, lid-close, or display-off power state
        /// </summary>
        public event EventHandler Pause;
        /// <summary>
        /// Triggered when computer exitted with resume, lid-open, or display-on power state
        /// </summary>
        public event EventHandler Resume;
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
                        _pm.RegisterGuids(new List<PowerGuid> { _dispEvent, _lidEvent, _suspendEvent, _resumeEvent });
                    }
                    else
                    {
                        _pm.UnregisterGuids(new List<PowerGuid> { _dispEvent, _lidEvent, _suspendEvent, _resumeEvent });
                    }
                }
            }
        }
        /// <summary>
        /// Initialize a new instance to track computer power events
        /// </summary>
        /// <param name="pm">Access to a prepared <see cref="PowerManagement.PowerManagement"/> object to receive events</param>
        public Tracker(PowerManagement.PowerManagement pm)
        {
            _pm = pm;

            Func<byte[], object> IntToBool = (byte[] data) => 0 != BitConverter.ToUInt32(data, 0);
            _dispEvent = new PowerGuid(PowerGuid.GUID_CONSOLE_DISPLAY_STATE, IntToBool);
            _lidEvent = new PowerGuid(PowerGuid.GUID_LIDSWITCH_STATE_CHANGE, IntToBool);
            _suspendEvent = _pm.SuspendPowerGuid;
            _resumeEvent = _pm.ResumePowerGuid;

            _dispEvent.Notified += (sender, e) =>
            {
                if (!_enabled) return;

                // Display on/off (true == on)
                bool on = (bool)e.Data;
                bool off = !on;

                if (off)
                {
                    if (_mode == "")
                    {
                        _mode = "Display";
                        _PauseAction();
                    }
                }
                if (on)
                {
                    if (_mode == "Display")
                    {
                        _ResumeAction();
                        _mode = "";
                    }
                }
            };
            _lidEvent.Notified += (sender, e) =>
            {
                if (!_enabled) return;

                // Lid open/close (true == open)
                bool open = (bool)e.Data;
                bool close = !open;

                if (close)
                {
                    if (_mode == "")
                    {
                        _mode = "Lid";
                        _PauseAction();
                    }
                }
                if (open)
                {
                    if (_mode == "Lid")
                    {
                        _ResumeAction();
                        _mode = "";
                    }
                }
            };
            _suspendEvent.Notified += (sender, e) =>
            {
                if (!_enabled) return;

                if (_mode == "")
                {
                    _mode = "Suspend";
                    _PauseAction();
                }
            };
            _resumeEvent.Notified += (sender, e) =>
            {
                if (!_enabled) return;

                if (_mode == "Suspend")
                {
                    _ResumeAction();
                    _mode = "";
                }
            };
        }

        private bool _enabled = false;
        private string _mode = "";
        private PowerManagement.PowerManagement _pm;
        private PowerGuid _dispEvent;
        private PowerGuid _lidEvent;
        private PowerGuid _suspendEvent;
        private PowerGuid _resumeEvent;

        private void _PauseAction()
        {
            Pause?.Invoke(this, new EventArgs());
        }
        private void _ResumeAction()
        {
            Resume?.Invoke(this, new EventArgs());
        }
    }
}
