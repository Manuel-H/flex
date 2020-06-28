using System;
using System.Reflection;

namespace com.Dunkingmachine.Utility
{
    public static class MemberInfoExtensions
    {
        public static bool IsPrivate(this MemberInfo memberInfo)
            => !(memberInfo as FieldInfo)?.IsPublic ?? ((PropertyInfo) memberInfo).GetSetMethod() == null;

        public static bool IsIndexProperty(this MemberInfo memberInfo)
            => memberInfo is PropertyInfo prop && prop.GetIndexParameters().Length > 0;

        public static Type GetUnderlyingType(this MemberInfo memberInfo)
            => (memberInfo as FieldInfo)?.FieldType ?? ((PropertyInfo) memberInfo).PropertyType;
    }
}
