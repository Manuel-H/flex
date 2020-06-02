using System;
using System.Collections.Generic;

namespace com.Dunkingmachine.Utility
{
    public static class TypeExtensions
    {
        private static readonly Type TypeOfString = typeof(string);
        private static readonly Type TypeOfList = typeof(List<>);
        private static readonly Type TypeOfDictionary = typeof(Dictionary<,>);

        public static bool IsInstantiableType(this Type type)
        {
            return !type.IsAbstract && !type.IsInterface && (type.IsClass || (type.IsValueType && !type.IsScalarType()));
        }
        public static bool IsScalarType(this Type type)
        {
            return type.IsPrimitive || type == TypeOfString || type.IsEnum;
        }

        public static bool IsArrayList(this Type type)
        {
            return type.IsArray || type.IsGenericType && type.GetGenericTypeDefinition() == TypeOfList;
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == TypeOfDictionary;
        }

        public static Type GetArrayListElementType(this Type type)
        {
            return type.GetElementType() ?? type.GetGenericArguments()[0];
        }
        
        public static string GetFullTypeName(this Type type)
        {
            return type?.FullName?.Replace(type.Namespace + ".", "").Replace('+', '.') ?? "Null";
        }
    }
}