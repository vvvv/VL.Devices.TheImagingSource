using ic4;
using Path = VL.Lib.IO.Path;
using System.Collections.Immutable;

namespace VL.Devices.TheImagingSource
{
    [ProcessNode(Name = "FromFile")]
    public class FileConfigurationNode
    {
        IConfiguration? configuration;
        Path? file;

        public IConfiguration Update(Path file)
        {
            if (file != this.file)
            {
                this.file = file;
                configuration = new FileConfiguration(file);

            }
            return configuration!;
        }
    }

    class FileConfiguration : IConfiguration
    {
        public Path File { get; }
        public FileConfiguration(Path file)
        {
            File = file;
        }
        
        public void Configure(PropertyMap propertyMap)
        {
            if(File.Exists) propertyMap.DeSerialize(File.ToString());
        }
    }
}
