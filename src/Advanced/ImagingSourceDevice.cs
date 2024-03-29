﻿using System.Reactive.Linq;
using ic4;
using VL.Core.CompilerServices;

namespace VL.Devices.TheImagingSource.Advanced;

[Serializable]
public class ImagingSourceDevice : DynamicEnumBase<ImagingSourceDevice, ImagingSourceDeviceDefinition>
{
    public ImagingSourceDevice(string value) : base(value)
    {
    }

    [CreateDefault]
    public static ImagingSourceDevice CreateDefault()
    {
        return CreateDefaultBase();
    }
}

public class ImagingSourceDeviceDefinition : DynamicEnumDefinitionBase<ImagingSourceDeviceDefinition>
{
    private IDisposable? _imagingSourceLibrary;

    protected override void Initialize()
    {
        try
        {
            _imagingSourceLibrary = ImagingSourceLibrary.Use().DisposeBy(AppHost.Global);

            var icDevices = DeviceEnum.Devices;
        }
        catch
        {

        }

        base.Initialize();
    }

    //Return the current enum entries
    protected override IReadOnlyDictionary<string, object> GetEntries()
    {
        if (_imagingSourceLibrary is null)
        {
            return new Dictionary<string, object>()
            {
                { "Default", null! }
            };
        }

        var icDevices = DeviceEnum.Devices;

        var devices = new Dictionary<string, object>()
        {
            { "Default", DeviceEnum.Devices.FirstOrDefault()! }
        };

        foreach (var device in icDevices)
        {
            var name = device.ModelName + " - " + device.Serial;
            if (!devices.ContainsKey(name))
            {
                devices.Add(name, device);
            }
        }

        return devices;
    }

    //Optionally trigger a change of your enum. This will in turn call GetEntries() again
    protected override IObservable<object> GetEntriesChangedObservable()
    {
        if (_imagingSourceLibrary is null)
            return Observable.Empty<object>();

        var enumerator = new DeviceEnum();

        return Observable.FromEventPattern(enumerator, nameof(enumerator.DeviceListChanged));
    }

    //Optionally disable alphabetic sorting
    protected override bool AutoSortAlphabetically => false; //true is the default
}