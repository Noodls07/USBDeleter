using System;
using Microsoft.Win32;
using System.Collections.Generic;


namespace USBDeleter
{

    public static class RegistryExtensions
    {
        public static bool hasCildren(this RegistryKey key)
        {
            bool has = false;
            
            if (key==null) return has;

            if (key.SubKeyCount > 0) has = true;

            return has;
        }

        public static bool hasKeyNames(this RegistryKey key)
        {
            bool has = false;

            if (key == null) return has;

            if (key.ValueCount > 0) has = true;

            return has;
        }
    }
}
