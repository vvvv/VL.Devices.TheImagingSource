using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ic4;
using VL.Lib.Basics.Video;
using VL.Model;
using System.Security.AccessControl;
using System.ComponentModel;

namespace VL.ImagingSource
{
    [ProcessNode]
    public class VideoIn : IVideoSource2, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDisposable _ic4LibSubscription;

        private int _changedTicket;
        private DeviceInfo? _device;
        private Int2 _resolution;
        private int _fps;

        public VideoIn([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
            _ic4LibSubscription = ImagingSourceLibrary.Use();
        }

        [return: Pin(Name = "Output")]
        public IVideoSource Update(
            ImagingSourceDevice? device, 
            [DefaultValue("640, 480")] Int2 resolution,
            [DefaultValue("30")] int fps,
            float exposure,
            out string xxxxxxx)
        {
            bool readDeviceInfo = false;
            // By comparing the device info we can be sure that on re-connect of the device we see the change
            if (device?.Tag != _device || resolution != _resolution || fps != _fps)
            {
                _device = device?.Tag as DeviceInfo;
                _resolution = resolution;
                _fps = fps;
                _changedTicket++;
            }

            if (readDeviceInfo)
            {
                ReadDeviceInfo();
            }

            Exposure = exposure;

            xxxxxxx = "lala";
            
            return this;
        }

        private void ReadDeviceInfo()
        {
            // Conflict if that specific device is currently in use by image acquisition below
            using (var grabber = new Grabber())
            {
                try
                {
                    grabber.DeviceOpen(_device);
                    try
                    {
                        // Read device properties
                    }
                    catch (Exception e)
                    {
                        // Reading properties crashed
                    }
                    finally
                    {
                        grabber.DeviceClose();
                    }
                }
                catch (Exception ex)
                {
                    // DeviceOpen crashed, probably because already in use, see comment above
                }
            }
        }

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            var device = _device;
            if (device is null)
                return null;

            try
            {
                return Acquisition.Start(this, device, _logger, _resolution, _fps);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start image acquisition");
                return null;
            }
        }

        int IVideoSource2.ChangedTicket => _changedTicket;

        internal float Exposure { get; private set; }

        public void Dispose()
        {
            _ic4LibSubscription.Dispose();
        }
    }
}
