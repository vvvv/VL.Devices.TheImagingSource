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
        private IConfiguration _configuration;


        internal string Info { get; set; } = "";
        internal Spread<PropertyInfo> PropertiesInfo { get; set; } = new SpreadBuilder<PropertyInfo>().ToSpread();

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
            IConfiguration configuration,
            out Spread<PropertyInfo> PropertiesInfo,
            out string Info)
        {
            // By comparing the device info we can be sure that on re-connect of the device we see the change
            // resolution and fps could be seted at runtime with grabber.propertymap
            if (device?.Tag != _device || resolution != _resolution || fps != _fps || configuration != _configuration)
            {
                _device = device?.Tag as DeviceInfo;
                _resolution = resolution;
                _fps = fps;
                _configuration = configuration;
                _changedTicket++;
            }

            PropertiesInfo = this.PropertiesInfo;
            Info = this.Info;
            
            return this;
        }


        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            var device = _device;
            if (device is null)
                return null;

            try
            {
                return Acquisition.Start(this, device, _logger, _resolution, _fps, _configuration);
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
