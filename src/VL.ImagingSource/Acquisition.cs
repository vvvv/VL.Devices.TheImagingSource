using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using ic4;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace VL.ImagingSource
{
    internal class Acquisition : IVideoPlayer
    {
        public static Acquisition? Start(DeviceInfo deviceInfo, ILogger logger)
        {
            logger.Log(LogLevel.Information, "Starting image acquisition on {device}", deviceInfo.UniqueName);

            var grabber = new Grabber();
            grabber.DeviceOpen(deviceInfo);

            if (grabber.IsDeviceOpen)
            {
                logger.Log(LogLevel.Information, "Opened device {device}", grabber.DeviceInfo.ModelName);
            }
            else
            {
                logger.LogError("Failed to open device");
                return null;
            }

            // Set the resolution to 640x480
            grabber.DevicePropertyMap.SetValue(ic4.PropId.Width, 640);
            grabber.DevicePropertyMap.SetValue(ic4.PropId.Height, 480);

            // Create a SnapSink. A SnapSink allows grabbing single images (or image sequences) out of a data stream.
            var sink = new SnapSink(acceptedPixelFormat: PixelFormat.BGRa8);
            // Setup data stream from the video capture device to the sink and start image acquisition.
            grabber.StreamSetup(sink, ic4.StreamSetupOption.AcquisitionStart);

            return new Acquisition(logger, grabber, sink);
        }

        private readonly IDisposable _idsPeakLibSubscription;
        private readonly ILogger _logger;
        private readonly Grabber _grabber;
        private readonly SnapSink _sink;

        public Acquisition(ILogger logger, Grabber grabber, SnapSink sink)
        {
            _idsPeakLibSubscription = ImagingSourceLibrary.Use();
            _logger = logger;
            _grabber = grabber;
            _sink = sink;
        }

        //public PixelFormat PixelFormat { get; set; } = new PixelFormat(PixelFormatName.BGRa8);

        public void Dispose()
        {
            _logger.Log(LogLevel.Information, "Stopping image acquisition");

            try
            {
                _grabber.StreamStop();
                _grabber.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected exception while stopping remote acquisition");
            }
        }

        public unsafe IResourceProvider<VideoFrame>? GrabVideoFrame()
        {
            var image = _sink.SnapSingle(TimeSpan.FromSeconds(1));

            var width = 640;
            var height = 480;
            var stride = image.BufferSize;

            var memoryOwner = new UnmanagedMemoryManager<BgraPixel>(image.Ptr, (int)image.BufferSize);

            var pitch = (int)image.Pitch - width * sizeof(BgraPixel);
            var memory = memoryOwner.Memory.AsMemory2D(0, height, width, pitch);
            var videoFrame = new VideoFrame<BgraPixel>(memory);
            return ResourceProvider.Return(videoFrame, (memoryOwner, image),
                static x =>
                {
                    ((IDisposable)x.memoryOwner).Dispose();
                    x.image.Dispose();
                });
        }
    }
}
