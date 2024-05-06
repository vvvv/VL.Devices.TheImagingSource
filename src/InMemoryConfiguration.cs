using ic4;
using Microsoft.Extensions.Logging;
using System.Collections.Immutable;

namespace VL.Devices.TheImagingSource
{
    [ProcessNode(Name = "SetProperty")]
    public class ConfigNode<T> : IConfiguration
    {
        private readonly ILogger logger;

        IConfiguration? input;
        string? key;
        T? value;
        FreshConfig? output;

        public ConfigNode([Pin(Visibility = Model.PinVisibility.Hidden)] NodeContext nodeContext)
        {
            this.logger = nodeContext.GetLogger();
        }

        public IConfiguration Update(IConfiguration input, string key, T value)
        {
            if (input != this.input || key != this.key || !EqualityComparer<T>.Default.Equals(value, this.value))
            {
                this.input = input;
                this.key = key;
                this.value = value;
                output = new FreshConfig(this);
            }
            return output!;
        }

        void IConfiguration.Configure(PropertyMap propertyMap)
        {
            input?.Configure(propertyMap);

            var p = propertyMap.Find(key);
            if (p is null)
            {
                logger.LogError("Property with name {key} not found.", key);
                return;
            }

            if (p is PropFloat f)
            {
                if (value is float fv)
                {
                    if (!f.TrySetValue(fv))
                        logger.LogError("Failed to set value: property IsReadonly {Readonly}, IsLocked {Locked}", f.IsReadonly, f.IsLocked);
                }
                else
                {
                    logger.LogError("Failed to set value: type missmatch, expecting a float");
                }
            }

            if (p is PropInteger i)
            {
                if (value is int iv)
                {
                    if (!i.TrySetValue(iv))
                        logger.LogError("Failed to set value: property IsReadonly {Readonly}, IsLocked {Locked}", i.IsReadonly, i.IsLocked);
                }
                else
                {
                    logger.LogError("Failed to set value: type missmatch, expecting an integer");
                }
            }

            if (p is PropBoolean b)
            {
                if (value is bool bv)
                {
                    if (!b.TrySetValue(bv))
                        logger.LogError("Failed to set value: property IsReadonly {Readonly}, IsLocked {Locked}", b.IsReadonly, b.IsLocked);
                }
                else
                {
                    logger.LogError("Failed to set value: type missmatch, expecting a boolean");
                }
            }

            if (p is PropString s)
            {
                if (value is string sv)
                {
                    if (!s.IsReadonly && !s.IsLocked && s.IsAvailable)
                    {
                        s.Value = sv;
                    }
                    else
                    {
                        logger.LogError("Failed to set value: property IsReadonly {Readonly}, IsLocked {Locked}", s.IsReadonly, s.IsLocked);
                    }
                        
                }
                else
                {
                    logger.LogError("Failed to set value: type missmatch, expecting a string");
                }
            }

            if (p is PropEnumeration e)
            {
                if (value is string ev)
                    if (!e.IsReadonly && !e.IsLocked && e.IsAvailable)
                    {
                        if (e.Entries.Select(x => x.Name).Contains(ev))
                        {
                            e.SelectedEntry = e.Entries.FirstOrDefault(x => x.Name == ev);
                        }
                        else
                        {
                            logger.LogError("Failed to set value: not a valid enum entry");
                        }
                    }
                    else
                    {
                        logger.LogError("Failed to set value: property IsReadonly {Readonly}, IsLocked {Locked}", e.IsReadonly, e.IsLocked);
                    }
                else
                {
                    logger.LogError("Failed to set value: type missmatch, expecting a string");
                }
            }
        }
    }

    // Utility so downstream sinks see the change. Forwards the Config call.
    internal sealed class FreshConfig : IConfiguration
    {
        private readonly IConfiguration original;

        public FreshConfig(IConfiguration original)
        {
            this.original = original;
        }

        public void Configure(PropertyMap propertyMap)
        {
            original.Configure(propertyMap);
        }
    }
}
