using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public static class FlexClassInfoUtility
    {
        private const int ScalarInfoId = 0;
        private const int ObjectInfoId = 1;
        private const int ScalarArrayInfoId = 2;
        private const int ObjectArrayInfoId = 3;
        private const int InfoBits = 2;
        private const int MemberIdBits = 16;
        private const int MemberBitsBits = 6;
        public static FlexClassInfo ReadClassInfo(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var serializer = new BitSerializer(bytes);
            var infos = new List<FlexMemberInfo>();
            var classinfo = new FlexClassInfo
            {
                TypeName = serializer.ReadString()
            };
            while (!serializer.LastByte)
            {
                infos.Add(ReadMemberInfo(serializer));
            }

            classinfo.MemberInfos = infos.ToArray();
            return classinfo;
        }

        public static void WriteClassInfo(FlexClassInfo info, string path)
        {
            var serializer = new BitSerializer();
            serializer.WriteString(info.TypeName);
            foreach (var memberInfo in info.MemberInfos)
            {
                WriteMemberInfo(serializer, memberInfo);
            }
            File.WriteAllBytes(path, serializer.GetBytes());
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexMemberInfo info)
        {
            switch (info)
            {
                case FlexScalarInfo scalar:
                    WriteMemberInfo(serializer, scalar);
                    return;
                case FlexObjectInfo obj:
                    WriteMemberInfo(serializer, obj);
                    return;
                case FlexScalarArrayInfo scalarArr:
                    WriteMemberInfo(serializer, scalarArr);
                    return;
                case FlexObjectArrayInfo objArr:
                    WriteMemberInfo(serializer, objArr);
                    return;
            }
        }

        private static FlexMemberInfo ReadMemberInfo(BitSerializer serializer)
        {
            var infotype = serializer.ReadInt(InfoBits);
            switch (infotype)
            {
                case ScalarInfoId:
                    return ReadScalarInfo(serializer);
                case ObjectInfoId:
                    return ReadObjectInfo(serializer);
                case ScalarArrayInfoId:
                    return ReadScalarArrayInfo(serializer);
                case ObjectArrayInfoId:
                    return ReadObjectArrayInfo(serializer);
            }
            throw new IndexOutOfRangeException("member info type out of range");
        }

        private static FlexScalarInfo ReadScalarInfo(BitSerializer serializer)
        {
            return new FlexScalarInfo
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                MemberBits = serializer.ReadInt(MemberBitsBits)
            };
        }

        private static FlexObjectInfo ReadObjectInfo(BitSerializer serializer)
        {
            return new FlexObjectInfo
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                AssignableTypes = Enumerable.Range(0, serializer.ReadInt(8)).Select(i => serializer.ReadString()).ToArray()
            };
        }

        private static FlexScalarArrayInfo ReadScalarArrayInfo(BitSerializer serializer)
        {
            return new FlexScalarArrayInfo
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                ElementBits = serializer.ReadInt(MemberBitsBits)
            };
        }

        private static FlexObjectArrayInfo ReadObjectArrayInfo(BitSerializer serializer)
        {
            return new FlexObjectArrayInfo
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                AssignableTypes = Enumerable.Range(0, serializer.ReadInt(8)).Select(i => serializer.ReadString()).ToArray()
            };
        }
        
        

        private static void WriteMemberInfo(BitSerializer serializer, FlexScalarInfo info)
        {
            serializer.WriteInt(ScalarInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            serializer.WriteInt(info.MemberBits, MemberBitsBits);
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexObjectInfo info)
        {
            serializer.WriteInt(ObjectInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            serializer.WriteInt(info.AssignableTypes.Length, 8);
            foreach (var assignableType in info.AssignableTypes)
            {
                serializer.WriteString(assignableType);
            }
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexScalarArrayInfo info)
        {
            serializer.WriteInt(ScalarArrayInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            serializer.WriteInt(info.ElementBits, MemberBitsBits);
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexObjectArrayInfo info)
        {
            serializer.WriteInt(ObjectArrayInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            serializer.WriteInt(info.AssignableTypes.Length, 8);
            foreach (var assignableType in info.AssignableTypes)
            {
                serializer.WriteString(assignableType);
            }
        }
    }
}