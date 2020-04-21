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

using Accord.Video.DirectShow;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Auto_Backlight_for_ThinkPad
{
    /// <summary>
    /// Simplified access to image intensity from the integrated webcam
    /// </summary>
    public class WebCam
    {
        /// <summary>
        /// Contains intensity event data
        /// </summary>
        public class ResultEventArgs : EventArgs
        {
            /// <summary>
            /// The average intensity level (range 0 to 1), 0 = black, 1 = white
            /// </summary>
            public double Intensity { get; }

            public ResultEventArgs(double intensity)
            {
                Intensity = intensity;
            }
        }
        /// <summary>
        /// Event handler type for processing result of an image intensity polling operation
        /// </summary>
        /// <param name="sender"><see cref="WebCam"/> instance</param>
        /// <param name="e">Contains intensity data</param>
        public delegate void ResultEventHandler(object sender, ResultEventArgs e);
        /// <summary>
        /// Triggered when results of image intensity polling operation are ready
        /// </summary>
        public event ResultEventHandler ResultReady;
        /// <summary>
        /// Sample the image intensity one time, awaitable for the result, or hook to event
        /// </summary>
        /// <returns>intensity result (Task awaitable)</returns>
        public async Task<double> Trigger()
        {
            double intensity = await _cam.GetIntensity();
            ResultReady?.Invoke(this, new ResultEventArgs(intensity));
            return intensity;
        }

        // private class packaging away the actual camera operations
        private class Cam
        {
            /// <summary>
            /// Initialize access to the camera hardware
            /// </summary>
            public Cam()
            {
                // Event used to wait for completion of camera tasks
                _finished = new EventWaitHandle(false, EventResetMode.ManualReset);

                // Find the integrated camera device
                var coll = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (!coll.Any(fi => fi.MonikerString.StartsWith(@"@device:pnp:\\?\usb#vid_5986&pid_2115")))
                    throw new Exception("Integrated Camera device not found");
                _vdev = new VideoCaptureDevice(coll.First(fi => fi.MonikerString.StartsWith(@"@device:pnp:\\?\usb#vid_5986&pid_2115")).MonikerString);

                // Initial settings and hook handlers
                _vdev.VideoResolution = _vdev.VideoCapabilities.First(sn => sn.FrameSize == new System.Drawing.Size(320, 240));
                _vdev.PlayingFinished += (sender, e) => _finished.Set();
                _vdev.NewFrame += (sender, e) =>
                {
                    int o_exp = -3;
                    CameraControlFlags o_flag = CameraControlFlags.Auto;
                    if (_cnt == 0)
                    {
                        // 1st frame, camera is open and working. Change settings to fixed constants.
                        _vdev.GetCameraProperty(CameraControlProperty.Exposure, out o_exp, out o_flag);
                        _vdev.SetCameraProperty(CameraControlProperty.Exposure, -3, CameraControlFlags.Manual);
                    }
                    if (_cnt == 4)
                    {
                        // 5th frame, keep this frame. Revert camera settings. Signal to stop querying for frames.
                        _img = e.Frame.Clone(new System.Drawing.Rectangle(0, 0, e.Frame.Width, e.Frame.Height), System.Drawing.Imaging.PixelFormat.DontCare);
                        _vdev.SetCameraProperty(CameraControlProperty.Exposure, o_exp, o_flag);
                        _vdev.SignalToStop();
                    }
                    _cnt++;
                };
            }
            /// <summary>
            /// Get the average image intensity through an awaitable Task (long background operation)
            /// </summary>
            /// <returns>awaitable Task providing the intensity result</returns>
            public Task<double> GetIntensity()
            {
                return Task.Run(() =>
                {
                    double ret = double.NaN;
                    // Lock incase multiple GetIntensity operations are trying to run simultaneously
                    lock (_vdev)
                    {
                        // Wait for stop or timeout after starting, timeout happens if camera is in use
                        _vdev.Start();
                        if (!_finished.WaitOne(TimeSpan.FromSeconds(3)))
                        {
                            _vdev.SignalToStop();
                            _vdev.WaitForStop();
                        }
                        if (_img != null)
                        {
                            // Post process image by calculating color stats
                            var stat = new Accord.Imaging.ImageStatisticsYCbCr(_img);
                            // Signature for the no-picture image, happens if camera cover is closed
                            if (stat.Y.Mean != 5.348498821258544921875E-1f || stat.Cb.Mean != -1.96078442968428134918212890625E-3f || stat.Cr.Mean != -1.96078442968428134918212890625E-3f)
                                ret = stat.Y.Mean;
                        }
                        _ResetVars();
                    }
                    return ret;
                });
            }
            // Reset internal vars to receive next batch
            private void _ResetVars()
            {
                _cnt = 0;
                _img?.Dispose();
                _img = null;
                _finished.Reset();
            }

            private VideoCaptureDevice _vdev;
            private EventWaitHandle _finished;
            private System.Drawing.Bitmap _img;
            private int _cnt;
        }
        private Cam _cam = new Cam();
    }
}