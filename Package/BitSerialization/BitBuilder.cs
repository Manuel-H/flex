using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using com.Dunkingmachine.Utility;

namespace com.Dunkingmachine.BitSerialization
{
    public abstract class BitBuilder<T> where T : Attribute
    {
        protected Assembly Assembly;
        protected Dictionary<string, Type> DataTypes;
        protected Dictionary<string, Type> InstantiableTypes;
        protected string DataPath;
        protected string NameSpace;
        protected readonly List<Type> ProcessedTypes = new List<Type>();
        protected CustomExtension[] CustomExtensions;
        protected bool SerializeNull;
        public void Build(Assembly assembly, string path, string nameSpace, CustomExtension[] customExtensions = null)
        {
            CustomExtensions = customExtensions;
            DataPath = path;
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            NameSpace = nameSpace;
            Assembly = assembly;
            InstantiableTypes = Assembly.ExportedTypes.Where(t => t.IsInstantiableType() && !t.IsGenericType && !IsNullable(t)).ToDictionary(t => t.GetFullTypeName(), t => t);
            var markedTypes = Assembly.ExportedTypes.Where(t => t.GetCustomAttribute<T>() != null);
            DataTypes = Assembly.ExportedTypes.Where(t => t.IsInstantiableType() && markedTypes.Any(t2 => t2.IsAssignableFrom(t))).ToDictionary(t => t.GetFullTypeName(), t => t);
            OnPreBuild();
            foreach (var keyValuePair in DataTypes)
            {
                CreateSerializerClass(keyValuePair.Value);
            }
        }

        protected abstract void OnPreBuild();
        
        protected void CreateSerializerClass(Type type)
        {
            if (ProcessedTypes.Contains(type))
                return;
            ProcessedTypes.Add(type);
            var content = CreateClassStringForType(type);
            File.WriteAllText(DataPath + "/" + type.Name + "Serializer.cs", content);
        }
        
        private string CreateClassStringForType(Type type)
        {
            var members = type.GetMembers(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var usings = new List<string>
            {
                type.Namespace,
                "System"
            };
            var extension = CustomExtensions?.FirstOrDefault(e => e.BaseType.IsAssignableFrom(type));
            if (extension != null)
            {
                foreach (var @using in extension.Usings)
                {
                    if(!usings.Contains(@using))
                        usings.Add(@using);
                }
            }

            var ser = CreateSerializationMethod();
            var des = CreateDeserializationMethod(type, members, extension, usings);
            var cb = new StringBuilder();
            foreach (var @using in usings)
            {
                if (string.IsNullOrEmpty(@using))
                    continue;
                cb.Append("using " + @using + ";" + Environment.NewLine);
            }

            cb.Append(Environment.NewLine);
            cb.Append("namespace "+NameSpace + Environment.NewLine);
            cb.Append("{" + Environment.NewLine);
            cb.Append("\tpublic static class " + type.Name + "Serializer" + Environment.NewLine);
            cb.Append("\t{" + Environment.NewLine);
            if (extension != null)
                cb.Append(extension.Fields + Environment.NewLine);
            cb.Append(ser);
            cb.Append(des);
            cb.Append("\t}" + Environment.NewLine);
            cb.Append("}");
            return cb.ToString();
        }

        private string CreateSerializationMethod()
        {
            return Environment.NewLine;
        }

        protected abstract string CreateDeserializationCode(Type type, MemberInfo[] members, List<string> usings);
        
        protected abstract string SerializerTypeString { get; }
        private string CreateDeserializationMethod(Type type, MemberInfo[] members, CustomExtension extension, List<string> usings)
        {
            StringBuilder method = new StringBuilder();
            var fullname = type.GetFullTypeName();
            method.Append("\t\tpublic static " + fullname + " Deserialize(object @default, "+SerializerTypeString+" serializer)" + Environment.NewLine);
            method.Append("\t\t{" + Environment.NewLine);
            method.Append("\t\t\tvar item = "+ (type.IsValueType ? "" :("@default as "+fullname+" ?? "))+"new " + fullname + "();" + Environment.NewLine);
            method.Append(CreateDeserializationCode(type, members, usings));
            if (extension != null)
                method.Append(extension.DeserializeActions + Environment.NewLine);
            method.Append("\t\t\treturn item;" + Environment.NewLine);
            method.Append("\t\t}" + Environment.NewLine);
            return method.ToString();
        }

        protected static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }
    }
    
    public class CustomExtension
    {
        public Type BaseType;
        public string[] Usings = {};
        public string Fields;
        public string DeserializeActions;
        public string SerializeActions;
    }
}