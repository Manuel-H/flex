using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace com.Dunkingmachine.BitSerialization
{
    public abstract class BitBuilder<T> where T : Attribute
    {
        protected Assembly Assembly;
        protected Dictionary<string, Type> DataTypes;
        public void Build(Assembly assembly, string path, string nameSpace)
        {
            Assembly = assembly;
            DataTypes = Assembly.ExportedTypes.Where(t => t.GetCustomAttribute<T>() != null).ToDictionary(GetFullTypeName, t => t);
        }
        
        protected static string GetFullTypeName(Type type)
        {
            return type.FullName.Replace(type.Namespace + ".", "").Replace('+', '.');
        }
    }
}