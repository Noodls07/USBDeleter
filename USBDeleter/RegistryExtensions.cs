using System;
using Microsoft.Win32;
using System.Collections.Generic;


namespace USBDeleter
{

    public static class RegistryExtensions
    {
        public static bool hasCildren(this RegistryKey key)
        {
            return key.SubKeyCount > 0;
        }

        public static bool hasKeyNames(this RegistryKey key)
        {
            return key.ValueCount > 0;
        }
    }
}
