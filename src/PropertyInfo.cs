using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VL.Devices.TheImagingSource
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="DefaultValue"></param>
    public record PropertyInfo(string Name, object CurrentValue, string Description, object Minimum, object Maximum)
    {
    }
}
