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
        public IVideoSource Update(ImagingSourceDevice? device, Int2 resolution, int fps)
        {
            // By comparing the device info we can be sure that on re-connect of the device we see the change
            if (device?.Tag != _device || resolution != _resolution || fps != _fps)
            {
                _device = device?.Tag as DeviceInfo;
                _resolution = resolution;
                _fps = fps;
                _changedTicket++;
            }
            
            return this;
        }

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            var device = _device;
            if (device is null)
                return null;

            try
            {
                return Acquisition.Start(device, _logger, _resolution, _fps);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to start image acquisition");
                return null;
            }
        }

        int IVideoSource2.ChangedTicket => _changedTicket;

        public void Dispose()
        {
            _ic4LibSubscription.Dispose();
        }
    }
}
