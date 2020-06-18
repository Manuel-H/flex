using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using com.Dunkingmachine.BitSerialization;
using com.Dunkingmachine.Utility;

namespace com.Dunkingmachine.FlexSerialization
{
    public class FlexBuilder : BitBuilder<FlexDataAttribute>
    {
        private const string MetaFileExtension = "flexmeta";
        private const string BuiltinNamespace = "com.Dunkingmachine.FlexSerialization";
        private Dictionary<string, FlexClassInfo> _infos = new Dictionary<string, FlexClassInfo>();

        private void BuildMetaFiles(string path)
        {
            LoadMetaFiles(path);
            GenerateClassInfos();
            WriteMetaFiles(path);
        }

        protected override void OnPostBuild()
        {
            BuildFlexMethodProvider(ProcessedTypes);
        }

        public override void Clear(string path)
        {
            File.Delete(path+"/FlexInitializer.cs");
            base.Clear(path);
        }

        private void BuildFlexMethodProvider(List<Type> types)
        {
            var cb = new StringBuilder();
            var usings = new List<string>
            {
                "System","System.Collections.Generic",NameSpace
            };
            foreach (var type in types)
            {
                if(!usings.Contains(type.Namespace))
                    usings.Add(type.Namespace);
            }
            foreach (var @using in usings)
            {
                cb.AppendLine("using " + @using + ";");
            }

            // if (_namespace != BUILTIN_NAMESPACE)
            //     cb.AppendLine("using " + _namespace+";");
            cb.AppendLine(Environment.NewLine);
            cb.AppendLine("namespace "+BuiltinNamespace );
            cb.AppendLine("{" );
            cb.AppendLine("\tpublic static class FlexInitializer" );
            cb.AppendLine("\t{");
            cb.AppendLine("\t\tpublic static void Initialize()");
            cb.AppendLine("\t\t{");
            cb.AppendLine("\t\t\tFlexConvert.Initializer.Initialize(SerializeMethods, DeserializeMethods);");
            cb.AppendLine("\t\t}");
            cb.AppendLine("\t\tprivate static readonly Dictionary<Type, Action<object, FlexSerializer>> SerializeMethods = new Dictionary<Type, Action<object, FlexSerializer>>");
            cb.AppendLine("\t\t{");
            cb.Append(string.Join("," + Environment.NewLine, types.Select(t => "\t\t\t{typeof(" + t.GetFullTypeName() + "), " + t.Name + "Serializer.Serialize}")));
            cb.AppendLine(Environment.NewLine+"\t\t};");
            cb.AppendLine("\t\tprivate static readonly Dictionary<Type, Func<object, FlexSerializer, object>> DeserializeMethods = new Dictionary<Type, Func<object, FlexSerializer, object>>");
            cb.AppendLine("\t\t{");
            cb.Append(string.Join("," + Environment.NewLine, types.Select(t => "\t\t\t{typeof(" + t.GetFullTypeName() + "), " + t.Name + "Serializer.Deserialize}")));
            cb.AppendLine(Environment.NewLine+"\t\t};");
            
            cb.AppendLine("\t}");
            cb.Append("}");
            File.WriteAllText(DataPath +"/FlexInitializer.cs", cb.ToString());
        }

        private void LoadMetaFiles(string path)
        {
            foreach (var meta in Directory.GetFiles(path).Where(p => Path.GetExtension(p) == "." + MetaFileExtension))
            {
                try
                {
                    var classinfo = FlexClassInfoUtility.ReadClassInfo(meta);
                    if (InstantiableTypes.TryGetValue(classinfo.TypeName, out var type))
                        classinfo.Type = type;
                    _infos[classinfo.TypeName] = classinfo;
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e);
                }
            }
        }

        private List<Type> _classInfosToProcess = new List<Type>();
        private List<Type> _processedClassInfos = new List<Type>();

        private void GenerateClassInfos()
        {
            _classInfosToProcess.AddRange(DataTypes.Values);
            var infos = new List<FlexClassInfo>();
            while (_classInfosToProcess.Count > 0)
            {
                var copy = _classInfosToProcess.Where(i => !_processedClassInfos.Contains(i)).ToList();
                _classInfosToProcess.Clear();
                _processedClassInfos.AddRange(copy);
                infos.AddRange(copy.Select(GenerateClassInfo));
            }

            ProcessClassInfos(infos);
        }

        private void ProcessClassInfos(List<FlexClassInfo> current)
        {
            foreach (var info in current)
            {
                if (_infos.TryGetValue(info.TypeName, out var old))
                    Merge(info, old);
                _infos[info.TypeName] = info;
                AssignIds(info);
            }
        }

        private void Merge(FlexClassInfo current, FlexClassInfo old)
        {
            var infos = current.MemberInfos.ToList();
            foreach (var oldInfo in old.MemberInfos)
            {
                var match = infos.Find(i => i.MemberName == oldInfo.MemberName);
                if (match == null)
                    infos.Add(oldInfo);
                else
                {
                    match.MemberId = oldInfo.MemberId;
                    if (!match.Merge(oldInfo))
                    {
                        throw new FlexException("Error creating meta data for class " + current.TypeName + ": Configuration of member " + match.MemberName +
                                                " was changed. This will break old serialized data. Please assign a new, previously unused name to the member, or revert the changes to its configuration.");
                    }
                }
            }

            current.MemberInfos = infos.ToArray();
        }

        private void AssignIds(FlexClassInfo info)
        {
            var max = Math.Max(info.MemberInfos.Max(i => i.MemberId + 1), 1);
            foreach (var memberInfo in info.MemberInfos)
            {
                if (memberInfo.MemberId > 0)
                    continue;
                memberInfo.MemberId = max++;
            }

            info.MemberInfos = info.MemberInfos.OrderBy(i => i.MemberId).ToArray();
        }

        private FlexClassInfo GenerateClassInfo(Type type)
        {
            var info = new FlexClassInfo {TypeName = type.GetFullTypeName(), Type = type};
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var minfos = new List<FlexMemberInfo>();
            foreach (var memberInfo in members)
            {
                if (memberInfo.GetCustomAttribute<FlexIgnoreAttribute>() != null)
                    continue;
                if (memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property)
                    continue;
                var isprivate = !(memberInfo as FieldInfo)?.IsPublic ?? ((PropertyInfo) memberInfo).GetSetMethod() == null;
                if (isprivate)
                {
                    continue;
                }

                if (memberInfo is PropertyInfo prop && prop.GetIndexParameters().Length > 0)
                    continue;
                minfos.Add(GenerateMemberInfo(memberInfo));
            }

            info.MemberInfos = minfos.ToArray();
            return info;
        }

        private FlexDetail GetDetail(MemberInfo info, Type type)
        {
            return type.IsScalarType()
                ? (FlexDetail) new FlexScalarDetail
                {
                    MemberBits = GetBits(info, type, out var defaultBits),
                    Type = type,
                    IsNumeric = type != typeof(string) && type != typeof(bool),
                    DefaultBits = defaultBits
                }
                : new FlexObjectDetail
                {
                    AssignableTypes = GetAssignableTypeStrings(type)
                };
        }

        private FlexMemberInfo GenerateMemberInfo(MemberInfo memberInfo)
        {
            var mtype = (memberInfo as FieldInfo)?.FieldType ?? ((PropertyInfo) memberInfo).PropertyType;

            if (mtype.IsArrayList())
            {
                return new FlexArrayInfo
                {
                    MemberName = memberInfo.Name,
                    IsList = !mtype.IsArray,
                    Detail = GetDetail(memberInfo, mtype.GetArrayListElementType())
                };
            }

            if (mtype.IsDictionary())
            {
                var gens = mtype.GetGenericArguments();
                return new FlexDictionaryInfo
                {
                    MemberName = memberInfo.Name,
                    KeyDetail = GetDetail(memberInfo, gens[0]),
                    ValueDetail = GetDetail(memberInfo, gens[1])
                };
            }

            if (mtype.IsScalarType() || mtype.IsClass || mtype.IsInterface || mtype.IsValueType)
            {
                return new FlexSimpleTypeInfo()
                {
                    MemberName = memberInfo.Name,
                    Detail = GetDetail(memberInfo, mtype)
                };
            }

            throw new FlexException("Type not supported: " + mtype.GetFullTypeName());
        }

        private string[] GetAssignableTypeStrings(Type type)
        {
            return GetAssignableTypes(type).Select(t => t.GetFullTypeName()).ToArray();
        }

        private List<Type> GetAssignableTypes(Type mtype)
        {
            var subclasses = Assembly.ExportedTypes
                .Where(t => ((t.IsClass && !t.IsAbstract) || (t.IsValueType && !t.IsEnum)) && mtype.IsAssignableFrom(t) && !t.IsGenericType && !IsNullable(t))
                .ToList();
            if (((mtype.IsClass && !mtype.IsAbstract) || mtype.IsValueType) && !mtype.IsGenericType && !IsNullable(mtype) && !subclasses.Contains(mtype))
                subclasses.Insert(0, mtype);
            _classInfosToProcess.AddRange(subclasses.Where(t => !_processedClassInfos.Contains(t)));
            return subclasses;
        }

        private int GetBitsNeeded(FlexNumericRangeAttribute attribute)
        {
            //TODO
            return 32;
        }

        private int GetBits(MemberInfo info, Type type, out bool defaultBits)
        {
            defaultBits = false;
            var fm = info.GetCustomAttribute<FlexFloatRangeAttribute>();
            if (fm != null)
            {
                if (type != typeof(float) && type != typeof(double))
                    throw new FlexException("FlexFloatAttribute can only be placed on floating point types!");
                return GetBitsNeeded(fm);
            }

            var dm = info.GetCustomAttribute<FlexNumericRangeAttribute>();
            if (dm != null)
                return GetBitsNeeded(dm);
            defaultBits = true;
            if (type.IsEnum)
                type = type.GetEnumUnderlyingType();
            if (type == typeof(double) || type == typeof(long) || type == typeof(ulong))
                return 64;
            if (type == typeof(short) || type == typeof(ushort))
                return 16;
            if (type == typeof(byte) || type == typeof(sbyte))
                return 8;
            if (type == typeof(string))
                return -1;
            if (type == typeof(bool))
                return 1;
            return 32;
        }

        private void WriteMetaFiles(string path)
        {
            foreach (var flexClassInfo in _infos.Values)
            {
                FlexClassInfoUtility.WriteClassInfo(flexClassInfo, Path.Combine(path, flexClassInfo.TypeName + "." + MetaFileExtension));
            }
        }

        protected override void OnPreBuild()
        {
            BuildMetaFiles(SeralizerPath);
        }

        protected override string SerializerTypeString => "FlexSerializer";

        protected override string CreateSerializationCode(Type type, MemberInfo[] members, List<string> usings)
        {
            if (!_infos.TryGetValue(type.GetFullTypeName(), out var meta))
                throw new FlexException("Somehow no meta file for type " + type.GetFullTypeName() + " was created!");
            var method = new StringBuilder();
            foreach (var memberInfo in members)
            {
                if (memberInfo.MemberType != MemberTypes.Field && memberInfo.MemberType != MemberTypes.Property)
                    continue;
                if (memberInfo.GetCustomAttribute<FlexIgnoreAttribute>() != null)
                    continue;
                var isprivate = !(memberInfo as FieldInfo)?.IsPublic ?? ((PropertyInfo) memberInfo).GetSetMethod() == null;
                if (isprivate)
                {
                    if (memberInfo.GetCustomAttribute<FlexNumericRangeAttribute>() != null)
                        Logger.LogWarning("member " + memberInfo.Name + " in " + type.Name + " is private, but wants to be deserialized. pls make public kthx");
                    continue;
                }

                if (memberInfo is PropertyInfo prop && prop.GetIndexParameters()?.Length > 0)
                    continue;

                var memberMeta = meta.MemberInfos.FirstOrDefault(m => m.MemberName == memberInfo.Name);
                if (memberMeta == null)
                    throw new FlexException("Somehow no meta info for member " + memberInfo.Name + " in type " + type.GetFullTypeName() + " was created!");

                var mtype = (memberInfo as FieldInfo)?.FieldType ?? ((PropertyInfo) memberInfo).PropertyType;

                if (!usings.Contains(mtype.Namespace))
                    usings.Add(mtype.Namespace);

                method.AppendLine("\t\t\tserializer.WriteId(" + memberMeta.MemberId + ");");
                CreateReading(method, usings, memberInfo, memberMeta, mtype, "item." + memberInfo.Name, createClass: type.Assembly == Assembly);
            }

            method.AppendLine("\t\t\tserializer.WriteId(FlexSerializer.EndStructureId);");
            return method.ToString();
        }

        private void CreateReading(StringBuilder method, List<string> usings, MemberInfo memberInfo, FlexMemberInfo info, Type mtype, string access,
            string indents = "\t\t\t", bool createClass = true)
        {
            if (mtype.IsScalarType())
            {
                var detail = (FlexScalarDetail) ((info as FlexSimpleTypeInfo)?.Detail ?? ((FlexArrayInfo) info).Detail);
                method.AppendLine(indents + GetWriteString(detail, access) + ";");
            }
            else if (mtype.IsArray || (mtype.IsGenericType && mtype.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var etype = mtype.GetElementType() ?? mtype.GetGenericArguments()[0];
                ;
                if (!usings.Contains(etype.Namespace))
                    usings.Add(etype.Namespace);
                method.AppendLine(indents + "serializer.WriteArrayLength(" + access + (mtype.IsArray ? ".Length" : ".Count") + ");");
                method.AppendLine(indents + "for (var i = 0; i < " + access + (mtype.IsArray ? ".Length" : ".Count") + "; i++)");
                method.AppendLine(indents + "{");
                CreateReading(method, usings, memberInfo, info, etype, access + "[i]", indents + "\t", createClass);
                method.AppendLine(indents + "}");
            }
            else if (mtype.IsDictionary())
            {
                //TODO
                method.AppendLine(indents + "//TODO");
            }
            else if (mtype.IsClass || mtype.IsInterface || mtype.IsValueType)
            {
                if (!createClass)
                    return;
                var subclasses = InstantiableTypes.Values.Where(mtype.IsAssignableFrom).ToList();
                if (((mtype.IsClass && !mtype.IsAbstract) || mtype.IsValueType) && !mtype.IsGenericType && !IsNullable(mtype) && !subclasses.Contains(mtype))
                    subclasses.Insert(0, mtype);
                var detail = (FlexObjectDetail) ((info as FlexSimpleTypeInfo)?.Detail ?? ((FlexArrayInfo) info).Detail);
                if (detail.AssignableTypes.Length == 0)
                {
                    Logger.LogError("No instantiable types found for base class " + mtype.Name);
                    return;
                }

                if (!mtype.IsValueType)
                {
                    method.AppendLine(indents + "if (" + access + " == null)");
                    method.AppendLine(indents + "\tserializer.WriteTypeIndex(0);");
                    method.AppendLine(indents + "else");
                }

                method.AppendLine(indents + "{");
                if (detail.AssignableTypes.Length > 1)
                {
                    method.AppendLine(indents + "switch(" + access + ")");
                    method.AppendLine(indents + "{");
                    for (var i = 0; i < detail.AssignableTypes.Length; i++)
                    {
                        method.AppendLine(indents + "\tcase " + detail.AssignableTypes[i] + ":");
                        method.AppendLine(indents + "\t\tserializer.WriteTypeIndex(" + (i + 1) + ");");

                        method.AppendLine(indents + "\t\t" + detail.AssignableTypes[i].Replace(".", "") + "Serializer.Serialize(" + access + ", serializer);");
                        method.AppendLine(indents + "\t\tbreak;");
                    }

                    method.AppendLine(indents + "\tdefault:");
                    method.AppendLine(indents + "\t\tthrow new Exception(\"No serializer for \" + t" + memberInfo.Name + "+ \" created!\");" +
                                      Environment.NewLine);
                    method.AppendLine(indents + "}");
                }
                else
                {
                    method.AppendLine(indents + "\tserializer.WriteTypeIndex(1);");
                    method.AppendLine(indents + "\t" + detail.AssignableTypes[0].Replace(".", "") + "Serializer.Serialize(" + access + ", serializer);");
                }

                method.AppendLine(indents + "}");
            }
        }

        protected override string CreateDeserializationCode(Type type, MemberInfo[] members, List<string> usings)
        {
            if (!_infos.TryGetValue(type.GetFullTypeName(), out var meta))
                throw new FlexException("Somehow no meta file for type " + type.GetFullTypeName() + " was created!");
            usings.Add("com.Dunkingmachine.FlexSerialization");
            var method = new StringBuilder();
            method.AppendLine("\t\t\tvar id = serializer.ReadId();");
            method.AppendLine("\t\t\twhile (id != FlexSerializer.EndStructureId)");
            method.AppendLine("\t\t\t{");
            method.AppendLine("\t\t\t\tswitch (id)");
            method.AppendLine("\t\t\t\t{");


            foreach (var memberMeta in meta.MemberInfos)
            {
                var memberInfo = members.FirstOrDefault(m => memberMeta.MemberName == m.Name);
                if (memberInfo == null)
                {
                    method.AppendLine("\t\t\t\t\tcase " + memberMeta.MemberId + ":");
                    switch (memberMeta)
                    {
                        case FlexArrayInfo flexArrayInfo:
                            method.AppendLine("\t\t\t\t\t\tvar aLength" + memberMeta.MemberName + " = serializer.ReadArrayLength();");
                            method.AppendLine("\t\t\t\t\t\t" + "for (var i = 0; i < aLength" + memberMeta.MemberName + "; i++)");
                            method.AppendLine("\t\t\t\t\t\t" + "{");
                            CreateRemovedAssignment("\t\t\t\t\t\t\t", flexArrayInfo.Detail, method);
                            method.AppendLine("\t\t\t\t\t\t" + "}");
                            break;
                        case FlexDictionaryInfo flexDictionaryInfo:
                            //todo
                            break;
                        case FlexSimpleTypeInfo simple:
                            CreateRemovedAssignment("\t\t\t\t\t\t", simple.Detail, method);

                            break;
                    }
                    method.AppendLine("\t\t\t\t\t\tbreak;");
                    continue;
                }

                var mtype = (memberInfo as FieldInfo)?.FieldType ?? ((PropertyInfo) memberInfo).PropertyType;

                if (!usings.Contains(mtype.Namespace))
                    usings.Add(mtype.Namespace);
                method.AppendLine("\t\t\t\t\tcase " + memberMeta.MemberId + ":");
                CreateAssignment(method, usings, memberInfo, mtype, "item." + memberInfo.Name + "= {0}", "item." + memberInfo.Name, memberMeta,
                    createClass: type.Assembly == Assembly);

                method.AppendLine("\t\t\t\t\t\tbreak;");
            }

            method.AppendLine("\t\t\t\t}");
            method.AppendLine("\t\t\t\tid = serializer.ReadId();");
            method.AppendLine("\t\t\t}");
            return method.ToString();
        }

        private void CreateRemovedAssignment(string indents, FlexDetail detail, StringBuilder method)
        {
            switch (detail)
            {
                case FlexObjectDetail flexObjectDetail:
                    CreateObjectAssignments(method, "<deleted field>", "{0}", "null", indents, flexObjectDetail,
                        InstantiableTypes.Where(kvp => flexObjectDetail.AssignableTypes.Contains(kvp.Key)).Select(kvp => kvp.Value).ToList(), false);
                    break;
                case FlexScalarDetail flexScalarDetail:
                    if (flexScalarDetail.MemberBits == -1)
                        method.AppendLine(indents+"serializer.ReadString();");
                    else
                        method.AppendLine(indents+"serializer.Read(" + flexScalarDetail.MemberBits + ");");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CreateAssignment(StringBuilder method, List<string> usings, MemberInfo memberInfo, Type mtype, string assignment, string access,
            FlexMemberInfo info,
            string indents = "\t\t\t\t\t\t", bool createClass = true)
        {
            if (mtype.IsScalarType())
            {
                var detail = (FlexScalarDetail) ((info as FlexSimpleTypeInfo)?.Detail ?? ((FlexArrayInfo) info).Detail);
                method.AppendLine(indents + string.Format(assignment, GetReadString(detail, memberInfo)) + ";");
            }
            else if (mtype.IsArray || (mtype.IsGenericType && mtype.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var etype = mtype.GetElementType() ?? mtype.GetGenericArguments()[0];
                ;
                if (mtype.IsArray && !usings.Contains("System.Collections.Generic"))
                    usings.Add("System.Collections.Generic");
                if (!usings.Contains(etype.Namespace))
                    usings.Add(etype.Namespace);
                method.AppendLine(indents + "var aLength" + memberInfo.Name + " = serializer.ReadArrayLength();");

                method.AppendLine(indents + "var list" + memberInfo.Name + " = " + string.Format(assignment,
                    "new " + (mtype.IsArray
                        ? etype.GetFullTypeName() + "[aLength" + memberInfo.Name + "]"
                        : "List<" + etype.GetFullTypeName() + ">(aLength" + memberInfo.Name + ")") + ";"));
                method.AppendLine(indents + "for (var i = 0; i < aLength" + memberInfo.Name + "; i++)");
                method.AppendLine(indents + "{");
                CreateAssignment(method, usings, memberInfo, etype,
                    mtype.IsArray ? "list" + memberInfo.Name + "[i] = {0}" : "list" + memberInfo.Name + ".Add({0})", "list" + memberInfo.Name, info,
                    indents + "\t", createClass);
                method.AppendLine(indents + "}");
            }
            else if (mtype.IsDictionary())
            {
                //TODO
                method.AppendLine(indents + "//TODO");
            }
            else if (mtype.IsClass || mtype.IsInterface || mtype.IsValueType)
            {
                if (!createClass)
                    return;
                var subclasses = InstantiableTypes.Values.Where(mtype.IsAssignableFrom).ToList();
                if (((mtype.IsClass && !mtype.IsAbstract) || mtype.IsValueType) && !mtype.IsGenericType && !IsNullable(mtype) && !subclasses.Contains(mtype))
                    subclasses.Insert(0, mtype);
                var detail = (FlexObjectDetail) ((info as FlexSimpleTypeInfo)?.Detail ?? ((FlexArrayInfo) info).Detail);
                CreateObjectAssignments(method, mtype.Name, assignment, access, indents, detail, subclasses);
            }
        }

        private void CreateObjectAssignments(StringBuilder method, string mtypename, string assignment, string access, string indents, FlexObjectDetail detail,
            List<Type> subclasses, bool handleNull = true)
        {
            if (detail.AssignableTypes.Length == 0)
            {
                Logger.LogError("No instantiable types found for base class " + mtypename);
                return;
            }


            method.AppendLine(indents + "switch(serializer.ReadTypeIndex())");
            method.AppendLine(indents + "{");
            method.AppendLine(indents + "\tcase " + 0 + ":");
            if (handleNull)
            {
                method.AppendLine(indents + "\t\t" + string.Format(assignment, "null") + ";");
            }
            method.AppendLine(indents + "\t\tcontinue;");
            for (var i = 0; i < detail.AssignableTypes.Length; i++)
            {
                method.AppendLine(indents + "\tcase " + (i + 1) + ":");
                CreateObjectAssignment(method, assignment, access, indents + "\t\t", subclasses, detail, i);
                method.AppendLine(indents + "\t\tbreak;");
            }

            method.AppendLine(indents + "\tdefault:");
            method.AppendLine(indents + "\t\tthrow new Exception(\"No deserializer created!\");" +
                              Environment.NewLine);
            method.AppendLine(indents + "}");
        }

        private void CreateObjectAssignment(StringBuilder method, string assignment, string access, string indents, List<Type> subclasses,
            FlexObjectDetail detail, int i)
        {
            var subclass = subclasses.Find(t => t.GetFullTypeName() == detail.AssignableTypes[i]);
            if (subclass != null)
            {
                CreateSerializerClass(subclass);
                method.AppendLine(indents + string.Format(assignment,
                    subclass.GetFullTypeName().Replace(".", "") + "Serializer.Deserialize(" + access + ", serializer)") + ";");
            }
            else if (_infos.TryGetValue(detail.AssignableTypes[i], out var subclassInfo) && subclassInfo.Type != null)
            {
                //no longer assignable? just deserialize the type without assigning to forward the serializer
                CreateSerializerClass(subclassInfo.Type);
                method.AppendLine(indents + subclassInfo.TypeName.Replace(".", "") + "Serializer.Deserialize(" + access + ", serializer)" + ";");
            }
            else
            {
                throw new NotImplementedException("Deleting types previously used in serialization process is not supported!");
            }
        }

        private static string GetWriteString(FlexScalarDetail detail, string access)
        {
            var type = detail.Type;
            var bits = detail.MemberBits;
            if (type == typeof(float))
                return detail.DefaultBits ? "serializer.WriteFloatLossyAuto(" + access + ")" : "serializer.WriteFloatLossless(" + access + ")";
            if (type == typeof(int))
                return detail.DefaultBits ? "serializer.WriteVarInt(" + access + ")" : "serializer.WriteInt(" + access + "," + bits + ")";
            if (type == typeof(uint))
                return "serializer.WriteUInt(" + access + "," + bits + ")";
            if (type == typeof(ulong))
                return "serializer.WriteULong(" + access + "," + bits + ")";
            if (type == typeof(double))
                return "serializer.WriteDoubleLossless(" + access + ")"; //TODO:quantized double
            if (type == typeof(string))
                return "serializer.WriteString(" + access + ")";
            if (type == typeof(bool))
                return "serializer.WriteBool(" + access + ")";
            //var typename = type.FullName;
            if (type.IsEnum)
            {
                var et = Enum.GetUnderlyingType(type);
                if (et == typeof(byte))
                {
                    return "serializer.WriteByte((byte)" + access + ")";
                }

                return detail.DefaultBits
                    ? "serializer.WriteEnumInt((uint)(" + et.Name + ")" + access + ")"
                    : "serializer.WriteInt((int)" + access + "," + bits + ")";
            }

            throw new NotImplementedException("missing type " + type.Name);
        }

        private static string GetReadString(FlexScalarDetail detail, MemberInfo info)
        {
            var type = detail.Type;
            var bits = detail.MemberBits;
            if (type == typeof(float))
                return detail.DefaultBits ? "serializer.ReadFloatLossyAuto()" : "serializer.ReadFloatLossless()";
            if (type == typeof(int))
                return detail.DefaultBits ? "serializer.ReadVarInt()" : "serializer.ReadInt(" + bits + ")";
            if (type == typeof(uint))
                return "serializer.ReadUInt(" + bits + ")";
            if (type == typeof(ulong))
                return "serializer.ReadULong(" + bits + ")";
            if (type == typeof(double))
                return "serializer.ReadDoubleLossless()"; //TODO:quantized double
            if (type == typeof(string))
                return "serializer.ReadString()";
            if (type == typeof(bool))
                return "serializer.ReadBool()";
            var typename = type.FullName;
            if (type.IsEnum)
            {
                var et = Enum.GetUnderlyingType(type);
                if (et == typeof(byte))
                {
                    return "(" + typename + ")serializer.ReadByte()";
                }

                return detail.DefaultBits
                    ? "(" + typename + ")(" + et.Name + ") serializer.ReadEnumInt()"
                    : "(" + typename + ") serializer.ReadInt(" + bits + ")";
            }

            throw new NotImplementedException("missing type " + type.Name);
        }
    }
}