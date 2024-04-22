using ic4;
using System.Collections.Immutable;

namespace VL.Devices.TheImagingSource
{
    [ProcessNode]
    public class InMemoryConfigurationNode
    {
        IConfiguration? configuration;
        ImmutableDictionary<string, object>? parameters;

        public IConfiguration Update(ImmutableDictionary<string, object> parameters)
        {
            if (parameters != this.parameters)
            {
                this.parameters = parameters;
                configuration = new InMemoryConfiguration(parameters);

            }
            return configuration!;
        }
    }

    class InMemoryConfiguration : IConfiguration
    {
        public IReadOnlyDictionary<string, object> Properties { get; }

        public InMemoryConfiguration(IReadOnlyDictionary<string, object> properties)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public void Configure(PropertyMap propertyMap)
        {
            foreach (var param in Properties)
            {
                var p = propertyMap.Find(param.Key);
                if (p != null)
                {
                    switch (p.Type)
                    {
                        case PropertyType.Float:
                            if (p is PropFloat f) { f.TrySetValue((float)param.Value); }
                            break;
                        case PropertyType.Integer:
                            if (p is PropInteger i) { i.TrySetValue((int)param.Value); }
                            break;
                        case PropertyType.Boolean:
                            if (p is PropBoolean b) { b.TrySetValue((bool)param.Value); }
                            break;
                        case PropertyType.String:
                            if (p is PropString s) { s.Value = (string)param.Value; }
                            break;
                        case PropertyType.Enumeration:
                            if (p is PropEnumeration e)
                            {
                                if (e.Entries.Select(x => x.Name).Contains((string)param.Value))
                                {
                                    e.SelectedEntry = e.Entries.FirstOrDefault(x => x.Name == (string)param.Value);
                                }
                            }
                            break;
                        default:
                            // cannot set value
                            break;
                    }
                }
            }
        }
    }
}
