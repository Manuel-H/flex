using System;

namespace com.Dunkingmachine.FlexSerialization
{
    /// <summary>
    ///     Put over class to mark this type as a data type, so that it's included in the auto-generation of
    ///     serializer classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class FlexDataAttribute : Attribute
    {

    }

    /// <summary>
    ///     Put over members to exclude them from serialization
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FlexIgnoreAttribute : Attribute
    {

    }

    /// <summary>
    ///     Place this attribute on numeric data type members to assign a custom range of min/max values,
    ///     which decreases the number of bits used to serialize the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FlexNumericRangeAttribute : Attribute
    {
        public long MinValue;
        public long MaxValue;
        public FlexNumericRangeAttribute(long maxValue, long minValue = 0)
        {
            MaxValue = maxValue;
            MinValue = minValue;
        }
    }

    /// <summary>
    ///     Place this attribute on floating point data members to compress their values.
    ///     Can severely reduce size if the range is small and only a few decimals are of significance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FlexFloatRangeAttribute : FlexNumericRangeAttribute
    {
        public byte Decimals;

        public FlexFloatRangeAttribute(long maxValue, long minValue, byte decimals) : base(maxValue, minValue)
        {
            Decimals = decimals;
        }
    }
}
