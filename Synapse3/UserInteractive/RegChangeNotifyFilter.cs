using System;

namespace Synapse3.UserInteractive
{
    [Flags]
    public enum RegChangeNotifyFilter
    {
        Key = 0x1,
        Attribute = 0x2,
        Value = 0x4,
        Security = 0x8
    }
}
