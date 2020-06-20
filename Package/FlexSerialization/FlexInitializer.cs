using System;
using System.Collections.Generic;

namespace com.Dunkingmachine.FlexSerialization
{
    /// <summary>
    /// This class exists so no compile errors occur when clearing generated classes. It is not actually used.
    /// </summary>
    public static class FlexInitializer
    {
        public static void Initialize()
        {
            throw new FlexException("You need to build serializer classes before calling this method!");
        }
    }
}