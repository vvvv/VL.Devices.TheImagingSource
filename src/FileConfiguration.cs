using ic4;
using System.Collections.Immutable;

namespace VL.Devices.TheImagingSource
{
    [ProcessNode]
    public class FileConfigurationNode
    {
        IConfiguration? configuration;
        string? file;

        public IConfiguration Update(string file)
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
        public string File { get; }
        public FileConfiguration(string file)
        {
            File = file;
        }
        
        public void Configure(PropertyMap propertyMap)
        {
            if(Path.Exists(File))propertyMap.DeSerialize(File);
        }
    }
}
