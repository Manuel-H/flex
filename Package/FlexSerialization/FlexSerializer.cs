using System;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public enum FlexToken {ScalarValue, BeginArray, EndArray, BeginObject, EndObject}
    public enum FlexInstruction {}
    
    public class FlexSerializer : BitSerializer
    {
        public const int EndStructureId = 0;
        public const int IsNullId = 1;

        private int _currentId = -1;

        public int CurrentId => _currentId == -1 ? ReadId() : _currentId;

        public int ReadId()
        {
            return _currentId = ReadInt(ReadBool() ? 5 : 9);
        }

        public void WriteId(int id)
        {
            WriteBool(id > 31);
            WriteInt(id, id > 31 ? 9 : 5);
        }
        public FlexToken ReadToken()
        {
            if (!ReadBool())
                return FlexToken.ScalarValue;
            switch (ReadInt(2))
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
                    WriteInt(0,2);
                    break;
                case FlexToken.EndArray:
                    WriteInt(1,2);
                    break;
                case FlexToken.BeginObject:
                    WriteInt(2,2);
                    break;
                case FlexToken.EndObject:
                    WriteInt(3,2);
                    break;
            }
        }
    }
}