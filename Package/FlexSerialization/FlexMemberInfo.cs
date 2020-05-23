namespace com.Dunkingmachine.FlexSerialization
{
    public abstract class FlexMemberInfo
    {
        public string MemberName;
        public int MemberId;
    }

    public class FlexScalarInfo : FlexMemberInfo
    {
        public int MemberBits;
    }

    public class FlexObjectInfo : FlexMemberInfo
    {
        public string[] AssignableTypes;
    }

    public abstract class FlexArrayInfo : FlexMemberInfo
    {
        
    }

    public class FlexScalarArrayInfo : FlexArrayInfo
    {
        public int ElementBits;
    }

    public class FlexObjectArrayInfo : FlexArrayInfo
    {
        public string[] AssignableTypes;
    }
}