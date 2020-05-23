using System;

namespace com.Dunkingmachine.FlexSerialization
{
    [AttributeUsage(AttributeTargets.Class)]
    public class FlexDataAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FlexDataMemberAttribute : Attribute
    {
        public int Bits;

        public FlexDataMemberAttribute(int bits)
        {
            Bits = bits;
        }
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class FlexFloatAttribute : FlexDataMemberAttribute
    {
        public int Exponent;

        public FlexFloatAttribute(int bits, int exponent) : base(bits)
        {
            Exponent = exponent;
        }
    }
}