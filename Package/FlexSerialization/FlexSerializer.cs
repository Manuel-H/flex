using System;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public enum FlexToken {ScalarValue, BeginArray, EndArray, BeginObject, EndObject}
    public enum FlexInstruction {}
    
    public class FlexSerializer : BitSerializer
    {
        public const int EndStructureId = 0;

        //private int _currentId = -1;

        //public int CurrentId => _currentId == -1 ? ReadId() : _currentId;

        public FlexSerializer() : base()
        {
            
        }

        public FlexSerializer(byte[] bytes) : base(bytes)
        {
            
        }
        public int ReadId()
        {
            return (int) ReadUInt(ReadBool() ? 9 : 5);
        }

        public void WriteId(int id)
        {
            WriteBool(id > 31);
            WriteUInt((uint) id, id > 31 ? 9 : 5);
        }

        public int ReadArrayLength()
        {
            if (!ReadBool())
                return (int) ReadUInt(4);
            if (!ReadBool())
            {
                var length = ReadUInt(8);
                if (length == 0) //this pattern is interpreted as null
                    return -1;
                return (int) length;
            }
            return (int) ReadUInt(20);
        }
        public void WriteArrayLength(int length)
        {
            if (length == -1) //-1 is interpreted as null
            {
                WriteBool(true);
                WriteBool(false);
                WriteUInt(0, 8);
                return;
            }
            WriteBool(length > 15);
            if (length > 15)
            {
                WriteBool(length > 255);
                WriteUInt((uint) length, length > 255 ? 20 : 8);
            } else WriteUInt((uint) length, 4);
        }

        public int ReadTypeIndex()
        {
            return (int) ReadUInt(ReadBool() ? 7 : 3);
        }

        public void WriteTypeIndex(int index)
        {
            WriteBool(index > 7);
            WriteUInt((uint) index ,index > 7 ? 7 : 3);
        }
        public FlexToken ReadToken()
        {
            if (!ReadBool())
                return FlexToken.ScalarValue;
            switch (ReadUInt(2))
            {
                case 0: return FlexToken.BeginArray;
                case 1: return FlexToken.EndArray;
                case 2: return FlexToken.BeginObject;
                case 3: return FlexToken.EndObject;
            }
            throw new Exception("Impossible Result");
        }

        public void WriteToken(FlexToken token)
        {
            WriteBool(token != FlexToken.ScalarValue);
            switch (token)
            {
                case FlexToken.BeginArray:
                    WriteUInt(0,2);
                    break;
                case FlexToken.EndArray:
                    WriteUInt(1,2);
                    break;
                case FlexToken.BeginObject:
                    WriteUInt(2,2);
                    break;
                case FlexToken.EndObject:
                    WriteUInt(3,2);
                    break;
            }
        }
    }
}