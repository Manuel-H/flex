using System;

namespace com.Dunkingmachine.FlexSerialization
{
    public class FlexClassInfo
    {
        public Type Type;
        public string TypeName;
        public FlexMemberInfo[] MemberInfos;
        public bool SerializeNull;
    }
}