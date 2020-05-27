using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using com.Dunkingmachine.Utility;

namespace com.Dunkingmachine.BitSerialization
{
    public abstract class BitBuilder<T> where T : Attribute
    {
        protected Assembly Assembly;
        protected Dictionary<string, Type> DataTypes;
        protected string DataPath;
        protected string NameSpace;
        public void Build(Assembly assembly, string path, string nameSpace)
        {
            DataPath = path;
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            NameSpace = nameSpace;
            Assembly = assembly;
            DataTypes = Assembly.ExportedTypes.Where(t => t.GetCustomAttribute<T>() != null).ToDictionary(t => t.GetFullTypeName(), t => t);
            OnPreBuild();
        }

        protected abstract void OnPreBuild();
    }
}