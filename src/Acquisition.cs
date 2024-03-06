using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using ic4;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace VL.Devices.TheImagingSource
{
    internal class Acquisition : IVideoPlayer
    {
        public static Acquisition? Start(VideoIn videoIn, DeviceInfo deviceInfo, ILogger logger, Int2 resolution, int fps)
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

            var frameRate = grabber.DevicePropertyMap.Find(ic4.PropId.AcquisitionFrameRate);
            float maxFPS = (float)frameRate.Maximum;
            float minFPS = (float)frameRate.Minimum;
            frameRate.TrySetValue(Math.Max(Math.Min(fps, maxFPS), minFPS));

            var width = grabber.DevicePropertyMap.Find(ic4.PropId.Width);
            width.TrySetValue(Math.Max(Math.Min(resolution.X, width.Maximum), width.Minimum)); //TrySetValue(resolution.X);

            var height = grabber.DevicePropertyMap.Find(ic4.PropId.Height);
            height.TrySetValue(Math.Max(Math.Min(resolution.Y, height.Maximum), height.Minimum)); //TrySetValue(resolution.Y);
            
            // Set the resolution and frame rate
            //grabber.DevicePropertyMap.SetValue(ic4.PropId.Width, resolution.X);
            //grabber.DevicePropertyMap.SetValue(ic4.PropId.Height, resolution.Y);
            //grabber.DevicePropertyMap.SetValue(ic4.PropId.AcquisitionFrameRate, Math.Max(Math.Min(fps, maxFPS), minFPS));

            // Create a SnapSink. A SnapSink allows grabbing single images (or image sequences) out of a data stream.
            var sink = new SnapSink(acceptedPixelFormat: PixelFormat.BGRa8);
            // Setup data stream from the video capture device to the sink and start image acquisition.
            grabber.StreamSetup(sink, ic4.StreamSetupOption.AcquisitionStart);

            //return debug info
            videoIn.Info = $"Framerate range: [{minFPS}, {maxFPS}], current FPS: {grabber.DevicePropertyMap.GetValueString(ic4.PropId.AcquisitionFrameRate)}" +
                           $"\r\nWidth range: [{width.Minimum}, {width.Maximum}], current Width {grabber.DevicePropertyMap.GetValueString(ic4.PropId.Width)}" +
                           $"\r\nHeight range: [{height.Minimum}, {height.Maximum}], current Height {grabber.DevicePropertyMap.GetValueString(ic4.PropId.Height)}";

            return new Acquisition(logger, grabber, sink, new Int2((int)width.Value, (int)height.Value));//, frameRate.Value);
        }

        private readonly IDisposable _idsPeakLibSubscription;
        private readonly ILogger _logger;
        private readonly Grabber _grabber;
        private readonly SnapSink _sink;
        private readonly Int2 _resolution;
        //private readonly int _fps;

        public Acquisition(ILogger logger, Grabber grabber, SnapSink sink, Int2 resolution)//, int fps)
        {
            _idsPeakLibSubscription = ImagingSourceLibrary.Use();
            _logger = logger;
            _grabber = grabber;
            _sink = sink;
            _resolution = resolution;
            //_fps = fps;
        }

        //public PixelFormat PixelFormat { get; set; } = new PixelFormat(PixelFormatName.BGRa8);

        public void Dispose()
        {
            _logger.Log(LogLevel.Information, "Stopping image acquisition");

            try
            {
                _grabber.StreamStop();
                _grabber.DeviceClose();
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

            var width = _resolution.X;
            var height = _resolution.Y;
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
