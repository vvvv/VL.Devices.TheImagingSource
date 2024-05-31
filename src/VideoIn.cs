using Microsoft.Extensions.Logging;
using System.ComponentModel;
using ic4;
using VL.Lib.Basics.Video;
using VL.Model;
using VL.Devices.TheImagingSource.Advanced;
using System.Reactive.Subjects;
using System.Reactive.Linq;

namespace VL.Devices.TheImagingSource
{
    [ProcessNode]
    public class VideoIn : IVideoSource2, IDisposable
    {
        private readonly ILogger _logger;
        private readonly IDisposable _ic4LibSubscription;
        private readonly BehaviorSubject<Acquisition?> _aquicitionStarted = new BehaviorSubject<Acquisition?>(null);

        private int _changedTicket;
        private DeviceInfo? _device;
        private Int2 _resolution;
        private int _fps;
        private IConfiguration? _configuration;


        internal string Info { get; set; } = "";
        internal Spread<PropertyInfo> PropertyInfos { get; set; } = new SpreadBuilder<PropertyInfo>().ToSpread();

        public VideoIn([Pin(Visibility = PinVisibility.Hidden)] NodeContext nodeContext)
        {
            _logger = nodeContext.GetLogger();
            _ic4LibSubscription = ImagingSourceLibrary.Use();
        }

        [return: Pin(Name = "Output")]
        public VideoIn Update(
            ImagingSourceDevice? device, 
            [DefaultValue("640, 480")] Int2 resolution,
            [DefaultValue("30")] int FPS,
            IConfiguration configuration,
            out string Info)
        {
            // By comparing the device info we can be sure that on re-connect of the device we see the change
            if (device?.Tag != _device || resolution != _resolution || FPS != _fps || configuration != _configuration)
            {
                _device = device?.Tag as DeviceInfo;
                _resolution = resolution;
                _fps = FPS;
                _configuration = configuration;
                _changedTicket++;
            }

            Info = this.Info;
            
            return this;
        }

        internal IObservable<Acquisition> AcquisitionStarted => _aquicitionStarted.Where(a => a != null && !a.IsDisposed)!;

        IVideoPlayer? IVideoSource2.Start(VideoPlaybackContext ctx)
        {
            var device = _device;
            if (device is null)
                return null;

            try
            {
                var result = Acquisition.Start(this, device, _logger, _resolution, _fps, _configuration);
                _aquicitionStarted.OnNext(result);
                return result;
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
