using System;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.FlexSerialization
{
    public enum FlexToken {ScalarValue, BeginArray, EndArray, BeginObject, EndObject}
    public class FlexSerializer : BitSerializer
    {
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