/// Steps to implement your own enum based on this template:
/// 1) Rename "VideoOutput" to what your enum should be named
/// 2) Rename "VideoOutputDefinition" accordingly
/// 3) Implement the definitions GetEntries() 
/// 
/// For more details regarding the template, see:
/// https://thegraybook.vvvv.org/reference/extending/writing-nodes.html#dynamic-enums

using System.Reactive.Linq;
using VL.Core.CompilerServices;
using VL.Lib.Collections;
using VL.Lib;

using ic4;
using System.Reactive.Disposables;

namespace VL.ImagingSource;

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
        
        foreach(var device in icDevices)
        {
            var name = device.ModelName + device.Serial;
            if(!devices.ContainsKey(name))
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