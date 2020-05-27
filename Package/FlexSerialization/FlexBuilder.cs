using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using com.Dunkingmachine.BitSerialization;
using com.Dunkingmachine.Utility;
using UnityEngine;

namespace com.Dunkingmachine.FlexSerialization
{
    public class FlexBuilder : BitBuilder<FlexDataAttribute>
    {
        private const string MetaFileExtension = "flexmeta";
        private Dictionary<string, FlexClassInfo> _infos = new Dictionary<string, FlexClassInfo>();

        private void BuildMetaFiles(string path)
        {
            LoadMetaFiles(path);
            GenerateClassInfos();
            WriteMetaFiles(path);
        }

        private void LoadMetaFiles(string path)
        {
            foreach (var meta in Directory.GetFiles(path).Where(p => Path.GetExtension(p) == "."+MetaFileExtension))
            {
                try
                {
                    var classinfo = FlexClassInfoUtility.ReadClassInfo(meta);
                    _infos[classinfo.TypeName] = classinfo;
                }
                catch (Exception e)
                {
                    Debug.LogWarning(e);
                }
            }
        }

        //TODO: recursive type handling - implement when combining with actual serialization method building
        private void GenerateClassInfos()
        {
            var infos = DataTypes.Select(kvp => GenerateClassInfo(kvp.Value)).ToList();
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
                    if(!match.Merge(oldInfo))
                    {
                        throw new FlexException("Error creating meta data for class " + current.TypeName + ": Configuration of member " + match.MemberName +
                                             " was changed. This will break old serialized data. Please assign a new, previously unused name to the member, or revert the changes to its configuration.");
                        
                    }
                }
            }
        }

        private void AssignIds(FlexClassInfo info)
        {
            var max = Math.Max(info.MemberInfos.Max(i => i.MemberId + 1), 2);
            foreach (var memberInfo in info.MemberInfos)
            {
                memberInfo.MemberId = max++;
            }

            info.MemberInfos = info.MemberInfos.OrderBy(i => i.MemberId).ToArray();
        }

        private FlexClassInfo GenerateClassInfo(Type type)
        {
            var info = new FlexClassInfo {TypeName = type.GetFullTypeName()};
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);
            var minfos = new List<FlexMemberInfo>();
            foreach (var memberInfo in members)
            {
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
                    MemberBits = GetBits(info, type),
                    Type = type,
                    IsNumeric = type != typeof(string) && type != typeof(bool)
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
            return subclasses;
        }

        public static bool IsNullable(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
        }

        private int GetBitsNeeded(FlexNumericRangeAttribute attribute)
        {
            //TODO
            return 10;
        }
        private int GetBits(MemberInfo info, Type type)
        {
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
            BuildMetaFiles(DataPath);
        }
    }
}