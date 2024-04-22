using CommunityToolkit.HighPerformance;
using Microsoft.Extensions.Logging;
using ic4;
using VL.Lib.Basics.Resources;
using VL.Lib.Basics.Video;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using System.Text;
using System.Collections.Immutable;

namespace VL.Devices.TheImagingSource
{
    internal class Acquisition : IVideoPlayer
    {
        public static Acquisition? Start(VideoIn videoIn, DeviceInfo deviceInfo, ILogger logger, Int2 resolution, int fps, IConfiguration configuration)
        {
            logger.Log(LogLevel.Information, "Starting image acquisition on {device}", deviceInfo.UniqueName);

            var grabber = new Grabber();
            grabber.DeviceOpen(deviceInfo);

            var pMap = grabber.DevicePropertyMap;

            if (grabber.IsDeviceOpen)
            {
                logger.Log(LogLevel.Information, "Opened device {device}", grabber.DeviceInfo.ModelName);
            }
            else
            {
                logger.LogError("Failed to open device");
                return null;
            }
            
            // Set the frame rate and resolution
            var frameRate = pMap.Find(ic4.PropId.AcquisitionFrameRate);
            float maxFPS = (float)frameRate.Maximum;
            float minFPS = (float)frameRate.Minimum;
            frameRate.TrySetValue(Math.Max(Math.Min(fps, maxFPS), minFPS));
            
            var width = pMap.Find(ic4.PropId.Width);
            width.TrySetValue(Math.Max(Math.Min(resolution.X, width.Maximum), width.Minimum)); //TrySetValue(resolution.X);

            var height = pMap.Find(ic4.PropId.Height);
            height.TrySetValue(Math.Max(Math.Min(resolution.Y, height.Maximum), height.Minimum)); //TrySetValue(resolution.Y);

            //apply static parameters
            configuration?.Configure(pMap);

            // Create a SnapSink. InMemoryConfiguration SnapSink allows grabbing single images (or image sequences) out of a data stream.
            var sink = new SnapSink(acceptedPixelFormat: PixelFormat.BGRa8);

            // Setup data stream from the video capture device to the sink and start image acquisition.
            grabber.StreamSetup(sink, ic4.StreamSetupOption.AcquisitionStart);

            //collect available properties
            var spb = new SpreadBuilder<PropertyInfo>();
            var pv = new StringBuilder();
            CollectPropertiesInfos(spb, pMap, pv);
            videoIn.PropertiesInfo = spb.ToSpread();
            
            videoIn.Info = $"Framerate range: [{minFPS}, {maxFPS}], current FPS: {pMap.GetValueString(ic4.PropId.AcquisitionFrameRate)}" +
                           $"\r\nWidth range: [{width.Minimum}, {width.Maximum}], current Width {pMap.GetValueString(ic4.PropId.Width)}" +
                           $"\r\nHeight range: [{height.Minimum}, {height.Maximum}], current Height {pMap.GetValueString(ic4.PropId.Height)}" +
                           $"\r\n";

            //properites list
            string props = "";
            var allProps = pMap.All;
            foreach (var prop in allProps)
            {
                if (prop.IsAvailable && !(prop.IsReadonly || prop.IsLocked))
                    if (prop.Type != PropertyType.Command)
                    {
                        props += $"\r\n{prop.Name} ({prop.Type}) Description: {prop.Description}";
                    }
            }
            videoIn.Info += props + $"\r\n";

            //properties tree
            Property r = pMap.FindCategory("Root");
            var sb = new StringBuilder();
            TraverseCategories(sb, r, "");
            videoIn.Info += $"\r\n" + sb.ToString();

            return new Acquisition(logger, grabber, sink, new Int2((int)width.Value, (int)height.Value), videoIn);//, frameRate.Value);
        }
        
        static void CollectPropertiesInfos(SpreadBuilder<PropertyInfo> spb, PropertyMap propertyMap, StringBuilder possibleValuies)
        {
            var props = propertyMap.All
                .Where(x => x.IsAvailable)
                .Where(x => !(x.IsReadonly || x.IsLocked))
                .Where(x => x.Type != PropertyType.Command)
                .Where(x => x.Type != PropertyType.Register);
            foreach (var p in props)
            {
                switch (p.Type)
                {
                    case PropertyType.Float:
                        if (p is PropFloat f) 
                        {
                            spb.Add(new PropertyInfo(f.Name, f.Value, f.Description, f.Minimum, f.Maximum, Spread<string>.Empty)); 
                        }
                        break;
                    case PropertyType.Integer:
                        if (p is PropInteger i) 
                        {
                            spb.Add(new PropertyInfo(i.Name, i.Value, i.Description, i.Minimum, i.Maximum, Spread<string>.Empty)); 
                        }
                        break;
                    case PropertyType.Boolean:
                        if (p is PropBoolean b) 
                        {
                            spb.Add(new PropertyInfo(b.Name, b.Value, b.Description, false, true, Spread<string>.Empty)); 
                        }
                        break;
                    case PropertyType.String:
                        if (p is PropString s) 
                        {
                            spb.Add(new PropertyInfo(s.Name, s.Value, s.Description, "", s.MaxLength, Spread<string>.Empty)); 
                        }
                        break;
                    case PropertyType.Enumeration:
                        if (p is PropEnumeration e)
                        {
                            spb.Add(new PropertyInfo(e.Name, e.SelectedEntry.Name, e.Description, "", "", e.Entries.Select(x => x.Name).ToSpread()));
                        }
                        break;
                    default:
                        // cannot set value
                        break;
                }
            }
        }

        
        static void TraverseCategories(StringBuilder sb, Property p, string offset)
        {
            if (p is PropCategory c)
            {
                sb.AppendLine($"{offset}--{ c.Name} ({ c.Type}) Description: { c.Description}");
                foreach (var cp in c.Features)
                {
                    if (cp.IsAvailable && !(cp.IsReadonly || cp.IsLocked))
                    {
                        if (cp.Type != PropertyType.Category)
                        {
                            sb.AppendLine($"{offset}    {cp.Name} ({cp.Type}) Description: {cp.Description}");
                        }
                        else
                        {
                            TraverseCategories(sb, cp, offset + "    ");
                        }
                    }
                }
            }
            else
            {
                sb.AppendLine($"\r\n{offset}{p.Name} ({p.Type}) Description: {p.Description}");
            }
            return;
        }

        private readonly IDisposable _idsPeakLibSubscription;
        private readonly ILogger _logger;
        private readonly Grabber _grabber;
        private readonly SnapSink _sink;
        private readonly Int2 _resolution;

        public Acquisition(ILogger logger, Grabber grabber, SnapSink sink, Int2 resolution, VideoIn videoIn)//, int fps)
        {
            _idsPeakLibSubscription = ImagingSourceLibrary.Use();
            _logger = logger;
            _grabber = grabber;
            _sink = sink;
            _resolution = resolution; 
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
