using com.Dunkingmachine.FlexSerialization;

namespace MyPackages.BitSerialization.Tests.TestClasses
{
    [FlexData]
    public abstract class DataBaseClass
    {
        public int Id = -1;
        public string Name;
    }
}