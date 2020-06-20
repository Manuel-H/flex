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
        protected string SeralizerPath => DataPath + "/Serializers";
        protected string NameSpace;
        protected readonly List<Type> ProcessedTypes = new List<Type>();
        protected CustomExtension[] CustomExtensions;
        protected bool SerializeNull;

        public virtual void Clear(string path)
        {
            foreach (var file in Directory.GetFiles(path+"/Serializers/", "*.cs"))
            {
                File.Delete(file);
            }
        }
        public void Build(Assembly assembly, string path, string nameSpace, CustomExtension[] customExtensions = null)
        {
            CustomExtensions = customExtensions;
            DataPath = path;
            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            if (!Directory.Exists(SeralizerPath))
                Directory.CreateDirectory(SeralizerPath);
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
            OnPostBuild();
        }

        protected abstract void OnPreBuild();

        protected abstract void OnPostBuild();
        protected void CreateSerializerClass(Type type)
        {
            if (ProcessedTypes.Contains(type))
                return;
            ProcessedTypes.Add(type);
            var content = CreateClassStringForType(type);
            File.WriteAllText(SeralizerPath + "/" + type.GetFullTypeName().Replace(".","") + "Serializer.cs", content);
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

            var ser = CreateSerializationMethod(type, members, extension, usings);
            var des = CreateDeserializationMethod(type, members, extension, usings);
            var cb = new StringBuilder();
            foreach (var @using in usings)
            {
                if (string.IsNullOrEmpty(@using))
                    continue;
                cb.AppendLine("using " + @using + ";");
            }

            cb.AppendLine();
            cb.AppendLine("namespace "+NameSpace);
            cb.AppendLine("{");
            cb.AppendLine("\tpublic static class " +type.GetFullTypeName() + "Serializer");
            cb.AppendLine("\t{");
            if (extension != null)
                cb.AppendLine(extension.Fields);
            cb.AppendLine(ser);
            cb.Append(des);
            cb.AppendLine("\t}");
            cb.Append("}");
            return cb.ToString();
        }

        protected abstract string CreateSerializationCode(Type type, MemberInfo[] members, List<string> usings);
        private string CreateSerializationMethod(Type type, MemberInfo[] members, CustomExtension extension, List<string> usings)
        {
            StringBuilder method = new StringBuilder();
            var fullname = type.GetFullTypeName();
            method.AppendLine("\t\tpublic static void Serialize(object @object, "+SerializerTypeString+" serializer)");
            method.AppendLine("\t\t{");
            method.AppendLine("\t\t\tvar item = (" + fullname + ")@object;");
            method.Append(CreateSerializationCode(type, members, usings));
            if (extension != null)
                method.AppendLine(extension.SerializeActions);
            method.AppendLine("\t\t}");
            return method.ToString();
        }

        protected abstract string CreateDeserializationCode(Type type, MemberInfo[] members, List<string> usings);
        
        protected abstract string SerializerTypeString { get; }
        private string CreateDeserializationMethod(Type type, MemberInfo[] members, CustomExtension extension, List<string> usings)
        {
            StringBuilder method = new StringBuilder();
            var fullname = type.GetFullTypeName();
            method.AppendLine("\t\tpublic static " + (type.IsValueType ? "object" : fullname) + " Deserialize(object @default, "+SerializerTypeString+" serializer)");
            method.AppendLine("\t\t{");
            if (type.IsValueType)
            {
                method.AppendLine("\t\t\tif (!(@default is "+fullname+" item))");
                method.AppendLine("\t\t\t\titem = new  "+ fullname + "();");
            }
            else
            {
                method.AppendLine("\t\t\tvar item = @default as "+fullname+" ?? new " + fullname + "();");
            }
            method.Append(CreateDeserializationCode(type, members, usings));
            if (extension != null)
                method.AppendLine(extension.DeserializeActions);
            method.AppendLine("\t\t\treturn item;");
            method.AppendLine("\t\t}");
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