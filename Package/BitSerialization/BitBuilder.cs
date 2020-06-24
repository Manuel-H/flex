using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using com.Dunkingmachine.Utility;

namespace com.Dunkingmachine.BitSerialization
{
    public abstract class BitBuilder<T> where T : Attribute
    {
        /// <summary>
        ///     Assembly that is being set when calling <see cref="Build"/>
        /// </summary>
        protected Assembly Assembly;

        /// <summary>
        ///     Path that is being set when calling <see cref="Build"/>
        /// </summary>
        protected string DataPath;

        /// <summary>
        ///     Namespace that is being set when calling <see cref="Build"/>
        /// </summary>
        protected string NameSpace;

        protected Dictionary<string, Type> DataTypes;
        protected Dictionary<string, Type> InstantiableTypes;

        protected readonly List<Type> ProcessedTypes = new List<Type>();

        protected CustomExtension[] CustomExtensions;

        protected bool SerializeNull;

        protected string SeralizerPath => DataPath + "/Serializers";

        /// <summary>
        ///     Auto-generates the serializing classes for the given assembly.
        /// </summary>
        /// <param name="assembly">Assembly containing all types that need to be serialized</param>
        /// <param name="path">Output path (absolute)</param>
        /// <param name="nameSpace">The namespace containing the auto-generated classes</param>
        /// <param name="customExtensions"></param>
        /// <exception cref="ArgumentNullException">
        ///     When either <paramref name="assembly"/>, <paramref name="nameSpace"/> or <paramref name="assembly"/> are null
        /// </exception>
        public void Build(Assembly assembly, string path, string nameSpace, CustomExtension[] customExtensions = null)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly) + " must not be null!");

            if (string.IsNullOrEmpty(nameSpace))
                throw new ArgumentNullException(nameof(nameSpace) + " must not be null or empty!");

            if (string.IsNullOrEmpty(path) || !Path.IsPathRooted(path))
                throw new ArgumentNullException(nameof(path) + " is not a valid path: " + path);

            Assembly = assembly;
            DataPath = path;
            NameSpace = nameSpace;
            CustomExtensions = customExtensions;

            if (!Directory.Exists(DataPath))
                Directory.CreateDirectory(DataPath);
            if (!Directory.Exists(SeralizerPath))
                Directory.CreateDirectory(SeralizerPath);

            InstantiableTypes = Assembly.ExportedTypes
                .Where(t => t.IsInstantiableType() && !t.IsGenericType && !IsNullable(t))
                .ToDictionary(t => t.FullName, t => t);

            var markedTypes = Assembly.ExportedTypes.Where(t => t.GetCustomAttribute<T>() != null);

            DataTypes = Assembly.ExportedTypes
                .Where(t => t.IsInstantiableType() && markedTypes.Any(t2 => t2.IsAssignableFrom(t)))
                .ToDictionary(t => t.FullName, t => t);

            OnPreBuild();

            foreach (var keyValuePair in DataTypes)
            {
                CreateSerializerClass(keyValuePair.Value);
            }

            OnPostBuild();
        }

        /// <summary>
        ///     Deletes all generated cs files at the specified path
        /// </summary>
        /// <param name="path">Absolute path to the <see cref="DataPath"/>, has to contain the subfolder 'Serializers'</param>
        public virtual void Clear(string path)
        {
            foreach (var file in Directory.GetFiles(path+"/Serializers/", "*.cs"))
            {
                File.Delete(file);
            }
        }

        protected abstract void OnPreBuild();

        protected abstract void OnPostBuild();

        protected abstract string CreateSerializationCode(Type type, MemberInfo[] members, List<string> usings);

        protected abstract string CreateDeserializationCode(Type type, MemberInfo[] members, List<string> usings);

        protected abstract string SerializerTypeString { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void CreateSerializerClass(Type type)
        {
            if (ProcessedTypes.Contains(type))
                return;

            ProcessedTypes.Add(type);
            var content = CreateClassStringForType(type);
            // ReSharper disable once PossibleNullReferenceException
            var className = type.FullName.Replace(".", "") + "Serializer.cs";
            File.WriteAllText(Path.Combine(SeralizerPath, className), content);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        // ReSharper disable once CognitiveComplexity
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
            cb.AppendLine("\tpublic static class " +type.GetFullCleanTypeName() + "Serializer");
            cb.AppendLine("\t{");
            if (extension != null)
                cb.AppendLine(extension.Fields);
            cb.AppendLine(ser);
            cb.Append(des);
            cb.AppendLine("\t}");
            cb.Append("}");
            return cb.ToString();
        }

        private string CreateSerializationMethod(Type type, MemberInfo[] members, CustomExtension extension, List<string> usings)
        {
            var method = new StringBuilder();
            var fullname = type.GetExtendedTypeName();
            method.AppendLine("\t\tpublic static void Serialize(object @object, "+SerializerTypeString+" serializer)");
            method.AppendLine("\t\t{");
            method.AppendLine("\t\t\tvar item = (" + fullname + ")@object;");
            method.Append(CreateSerializationCode(type, members, usings));
            if (extension != null)
                method.AppendLine(extension.SerializeActions);
            method.AppendLine("\t\t}");
            return method.ToString();
        }

        private string CreateDeserializationMethod(Type type, MemberInfo[] members, CustomExtension extension, List<string> usings)
        {
            var method = new StringBuilder();
            var fullname = type.GetExtendedTypeName();
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
