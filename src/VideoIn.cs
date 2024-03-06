using Microsoft.Extensions.Logging;
using System.ComponentModel;
using ic4;
using VL.Lib.Basics.Video;
using VL.Model;
using VL.Devices.TheImagingSource.Advanced;

namespace VL.Devices.TheImagingSource
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

        internal string Info { get; set; } = "";
        //internal float Exposure { get; private set; }

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
            //float exposure,
            out string Info)
        {
            /*
            bool readDeviceInfo = false;
            if (device?.Tag != _device)
            {
                readDeviceInfo = true;
            }
            
            if (readDeviceInfo)
            {
                ReadDeviceInfo(device?.Tag);
                readDeviceInfo = false;
            }
            */

            // By comparing the device info we can be sure that on re-connect of the device we see the change
            // resolution and fps could be seted at runtime with grabber.propertymap
            if (device?.Tag != _device || resolution != _resolution || fps != _fps)
            {
                _device = device?.Tag as DeviceInfo;
                _resolution = resolution;
                _fps = fps;
                _changedTicket++;
            }            

            if(this.Info != null)
            {
                Info = this.Info;
            }
            else
            {
                Info = "";
            }

            return this;
        }

        /*
        private void ReadDeviceInfo(DeviceInfo device)
        {
            // Conflict if that specific device is currently in use by image acquisition below
            using (var grabber = new Grabber())
            {
                try
                {                    
                    grabber.DeviceOpen(device);
                    try
                    {
                        // Read device properties
                        var frameRate = grabber.DevicePropertyMap.Find(ic4.PropId.AcquisitionFrameRate);
                        var width = grabber.DevicePropertyMap.Find(ic4.PropId.Width);
                        var height = grabber.DevicePropertyMap.Find(ic4.PropId.Height);
                        Info = $"Framerate range: [{frameRate.Minimum}, {frameRate.Maximum}];" +
                               $"\r\nWidth range: [{width.Minimum}, {width.Maximum}];" +
                               $"\r\nHeight range: [{width.Minimum}, {width.Maximum}];";
                    }
                    catch (Exception e)
                    {
                        // Reading properties crashed
                        _logger.LogError(e, "Failed to read device properties");

                    }
                    finally
                    {
                        grabber.DeviceClose();
                    }
                }
                catch (Exception e)
                {
                    //DeviceOpen crashed, probably because already in use, see comment above
                    _logger.LogError(e, "DeviceOpen crashed, probably because already in use, see comment above");
                }
            }
        }
        */

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

        public void Dispose()
        {
            _ic4LibSubscription.Dispose();
        }
    }
}
