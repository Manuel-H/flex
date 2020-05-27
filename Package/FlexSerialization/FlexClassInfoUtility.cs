using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public static class FlexClassInfoUtility
    {
        private const int SimpleInfoId = 0;
        private const int ArrayInfoId = 1;
        //private const int ObjectArrayInfoId = 3;
        private const int DictionaryInfoId = 2;
        private const int InfoBits = 2;
        private const int MemberIdBits= 16;
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
                case FlexSimpleTypeInfo simple:
                    WriteMemberInfo(serializer, simple);
                    return;
                case FlexArrayInfo array:
                    WriteMemberInfo(serializer, array);
                    return;
                case FlexDictionaryInfo dict:
                    WriteMemberInfo(serializer, dict);
                    return;
            }
        }

        private static FlexMemberInfo ReadMemberInfo(BitSerializer serializer)
        {
            var infotype = serializer.ReadInt(InfoBits);
            switch (infotype)
            {
                case SimpleInfoId:
                    return ReadSimpleInfo(serializer);
                case ArrayInfoId:
                    return ReadArrayInfo(serializer);
                case DictionaryInfoId:
                    return ReadDictionaryInfo(serializer);
            }
            throw new IndexOutOfRangeException("member info type out of range");
        }

        private static FlexSimpleTypeInfo ReadSimpleInfo(BitSerializer serializer)
        {
            return new FlexSimpleTypeInfo()
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                Detail = ReadDetail(serializer)
            };
        }

        private static FlexArrayInfo ReadArrayInfo(BitSerializer serializer)
        {
            return new FlexArrayInfo()
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                Detail = ReadDetail(serializer)
            };
        }

        private static FlexDictionaryInfo ReadDictionaryInfo(BitSerializer serializer)
        {
            return new FlexDictionaryInfo
            {
                MemberName = serializer.ReadString(),
                MemberId = serializer.ReadInt(MemberIdBits),
                KeyDetail = ReadDetail(serializer),
                ValueDetail = ReadDetail(serializer)
            };
        }

        private static FlexDetail ReadDetail(BitSerializer serializer)
        {
            if (serializer.ReadBool())
                return new FlexScalarDetail {MemberBits = serializer.ReadInt(MemberBitsBits), IsNumeric = serializer.ReadBool()};
            string[] assignableTypes = new string[serializer.ReadInt(8)];
            for (var i = 0; i < assignableTypes.Length; i++)
            {
                assignableTypes[i] = serializer.ReadString();
            }
            return new FlexObjectDetail {AssignableTypes = assignableTypes};
        }
        
        

        private static void WriteMemberInfo(BitSerializer serializer, FlexSimpleTypeInfo info)
        {
            serializer.WriteInt(SimpleInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            WriteDetail(serializer, info.Detail);
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexArrayInfo info)
        {
            serializer.WriteInt(ArrayInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            WriteDetail(serializer, info.Detail);
        }

        private static void WriteMemberInfo(BitSerializer serializer, FlexDictionaryInfo info)
        {
            serializer.WriteInt(DictionaryInfoId, InfoBits);
            serializer.WriteString(info.MemberName);
            serializer.WriteInt(info.MemberId, MemberIdBits);
            WriteDetail(serializer, info.KeyDetail);
            WriteDetail(serializer, info.ValueDetail);
        }

        private static void WriteDetail(BitSerializer serializer, FlexDetail detail)
        {
            switch (detail)
            {
                case FlexObjectDetail obJ:
                    WriteDetail(serializer, obJ);
                    break;
                case FlexScalarDetail scl:
                    WriteDetail(serializer, scl);
                    break;
            }
        }

        private static void WriteDetail(BitSerializer serializer, FlexObjectDetail detail)
        {
            serializer.WriteBool(false);
            serializer.WriteInt(detail.AssignableTypes.Length, 8);
            foreach (var assignableType in detail.AssignableTypes)
            {
                serializer.WriteString(assignableType);
            }
        }

        private static void WriteDetail(BitSerializer serializer, FlexScalarDetail detail)
        {
            serializer.WriteBool(true);
            serializer.WriteInt(detail.MemberBits, MemberBitsBits);
            serializer.WriteBool(detail.IsNumeric);
        }
    }
}