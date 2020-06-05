using System;
using System.Linq;

#pragma warning disable 660,661

namespace com.Dunkingmachine.FlexSerialization
{
    public abstract class FlexMemberInfo
    {
        public string MemberName;
        public int MemberId;

        public abstract bool Merge(FlexMemberInfo info);
    }

    public class FlexSimpleTypeInfo : FlexMemberInfo
    {
        public FlexDetail Detail;
        public override bool Merge(FlexMemberInfo info)
        {
            if (!(info is FlexSimpleTypeInfo simple))
                return false;
            return Detail.Merge(simple.Detail);
        }
    }

    public class FlexArrayInfo : FlexMemberInfo
    {
        public FlexDetail Detail;
        public bool IsList;
        
        public override bool Merge(FlexMemberInfo info)
        {
            if (!(info is FlexArrayInfo array))
                return false;
            return Detail.Merge(array.Detail);
        }
    }

    // public class FlexScalarArrayInfo : FlexArrayInfo
    // {
    //     public int ElementBits;
    //     public Type ElementType;
    // }
    //
    // public class FlexObjectArrayInfo : FlexArrayInfo
    // {
    //     public string[] AssignableTypes;
    // }

    public class FlexDictionaryInfo : FlexMemberInfo
    {
        public FlexDetail KeyDetail;
        public FlexDetail ValueDetail;
        
        public override bool Merge(FlexMemberInfo info)
        {
            if (!(info is FlexDictionaryInfo dict))
                return false;
            return KeyDetail.Merge(dict.KeyDetail) && ValueDetail.Merge(dict.ValueDetail);
        }
    }

    public abstract class FlexDetail
    {
        public abstract bool Merge(FlexDetail info);
    }
    public class FlexScalarDetail : FlexDetail
    {
        public Type Type;
        public bool IsNumeric;
        public int MemberBits;
        public bool DefaultBits;
        
        public override bool Merge(FlexDetail info)
        {
            if (!(info is FlexScalarDetail scalar))
                return false;
            return IsNumeric == scalar.IsNumeric && MemberBits == scalar.MemberBits && DefaultBits == scalar.DefaultBits;
        }
    }

    public class FlexObjectDetail : FlexDetail
    {
        public string[] AssignableTypes;
        public override bool Merge(FlexDetail info)
        {
            if (!(info is FlexObjectDetail obj))
                return false;
            AssignableTypes = obj.AssignableTypes.Concat(AssignableTypes).Distinct().ToArray();
            return true;
        }
    }
}