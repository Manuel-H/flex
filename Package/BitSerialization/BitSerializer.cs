using System;
using System.Text;
using com.Dunkingmachine.Utility;

// Ignore some Inspections:
// ReSharper disable UnusedMember.Global

namespace com.Dunkingmachine.BitSerialization
{
    public class BitSerializer
    {
        private static StringEncoder _stringEncoder = new StringEncoder();
        private readonly BitBuffer _buffer;

        /// <summary>
        ///     Creates a serializing / writeOnly <see cref="BitSerializer"/>
        /// </summary>
        public BitSerializer()
        {
            _buffer = new BitBuffer();
        }

        /// <summary>
        ///     Creates a deserializing / readonly <see cref="BitSerializer"/>
        /// </summary>
        /// <param name="bytes"></param>
        public BitSerializer(byte[] bytes)
        {
            _buffer = new BitBuffer(bytes);
        }

        /// <summary>
        ///    True when the <see cref="BitSerializer"/> can only deserialize
        /// </summary>
        public bool IsReadonly => _buffer.BufferMode == BitBuffer.Mode.Read;

        /// <summary>
        ///     Returns all bytes from the buffer that have been written by now
        /// </summary>
        /// <returns>Byte array containing all serialized bits</returns>
        public byte[] GetBytes()
        {
            return _buffer.GetBytes();
        }

        /// <summary>
        ///     <seealso cref="BitBuffer.Skip"/>
        /// </summary>
        /// <param name="bits"></param>
        public void Skip(int bits)
        {
            _buffer.Skip(bits);
        }

        #region Enum

        public void WriteEnumInt(uint value)
        {
            if (value > 15)
            {
                _buffer.Write(1,1);
                if (value > 1023)
                {
                    _buffer.Write(1,1);
                    _buffer.Write(value, 32);
                }
                else
                {
                    _buffer.Write(0,1);
                    _buffer.Write(value, 10);
                }
            }
            else
            {
                _buffer.Write(0,1);
                _buffer.Write(value, 4);
            }
        }

        public uint ReadEnumInt()
        {
            return (uint) _buffer.Read(ReadBool() ? ReadBool() ? 32 : 10 : 4);
        }

        #endregion

        #region Numbers

        public void WriteVarInt(int value)
        {
            if (value >= 0)
            {
                _buffer.Write(0,1);
            }
            else
            {
                _buffer.Write(1,1);
                value++;
                value = -value;
            }

            if (value > 255)
            {
                _buffer.Write(1,1);
                if (value > 16383)
                {
                    _buffer.Write(1,1);
                    _buffer.Write((ulong) value, 31);
                }
                else
                {
                    _buffer.Write(0,1);
                    _buffer.Write((ulong) value, 14);
                }
            }
            else
            {
                _buffer.Write(0,1);
                _buffer.Write((ulong) value, 8);
            }
        }

        public int ReadVarInt()
        {
            var sign = _buffer.Read(1);
            var value = (int) _buffer.Read(ReadBool() ? ReadBool() ? 31 : 14 : 8);
            if (sign == 1)
            {
                value = -value - 1;
            }
            return value;
        }

        public void WriteVarLong(long value)
        {
            if (value >= 0)
            {
                _buffer.Write(0,1);
            }
            else
            {
                _buffer.Write(1,1);
                value++;
                value = -value;
            }

            do
            {
                _buffer.Write((ulong) (value & 127), 7);
                value >>= 7;
                _buffer.Write(value > 0 ? 1u : 0, 1);
            } while (value > 0);
        }

        public long ReadVarLong()
        {
            var sign = _buffer.Read(1);
            var value = 0L;
            var shift = 0;
            do
            {
                value |= (long) (_buffer.Read(7) << shift);
                shift += 7;
            } while (_buffer.Read(1) == 1);

            if (sign == 1)
            {
                value = -value-1;
            }
            return value;
        }

        public void WriteFloatLossless(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            var littleEndian = BitConverter.IsLittleEndian;
            for (var i = 0; i < 4; i++)
            {
                _buffer.Write(bytes[littleEndian ? i : 3 - i], 8);
            }
        }

        public float ReadFloatLossless()
        {
            var littleEndian = BitConverter.IsLittleEndian;
            var bytes = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                bytes[littleEndian ? i : 3 - i] = (byte) _buffer.Read(8);
            }

            return BitConverter.ToSingle(bytes, 0);
        }

        // ReSharper disable once CognitiveComplexity
        public void WriteFloatLossyAuto(float value)
        {
            var ovalue = value;
            if (value < 0)
                value = -value;
            if (value > 1023.99f || value < 0.0078125f)
            {
                _buffer.Write(0,3);
                WriteFloatLossless(ovalue);
                return;
            }

            if (value > 0.4999f)
            {
                if (value > 15.999f)
                {
                    if (value > 127.999f)
                    {
                        WriteQuantizedFloat(value, 1, 128, 17);
                    }
                    else
                    {
                        WriteQuantizedFloat(value, 2, 512, 16);
                    }
                }
                else if (value > 1.999f)
                {
                    WriteQuantizedFloat(value, 3, 4096, 16);
                }
                else
                {
                    WriteQuantizedFloat(value, 4, 16384, 15);
                }
            }
            else if (value > 0.03124999f)
            {
                if (value > 0.124999f)
                {
                    WriteQuantizedFloat(value, 5, 65536, 15);
                }
                else
                {
                    WriteQuantizedFloat(value, 6, 131072, 14);
                }
            }
            else
            {
                WriteQuantizedFloat(value, 7, 262144, 13);
            }
            _buffer.Write(ovalue < 0 ? 1u : 0, 1);
        }

        private void WriteQuantizedFloat(float value, ulong n, int mult, int bits)
        {
            _buffer.Write(n,3);
            _buffer.Write((ulong)(value*mult+0.5f), bits);
        }

        private float ReadFloatLossyAuto(ulong cfg)
        {
            switch (cfg)
            {
                case 1:
                    return ReadQuantizedFloat(128, 17);
                case 2:
                    return ReadQuantizedFloat(512, 16);
                case 3:
                    return ReadQuantizedFloat(4096, 16);
                case 4:
                    return ReadQuantizedFloat(16384, 15);
                case 5:
                    return ReadQuantizedFloat(65536, 15);
                case 6:
                    return ReadQuantizedFloat(131072, 14);
                case 7:
                    return ReadQuantizedFloat(262144, 13);
                default:
                    throw new Exception("What the Fuck Did You Just Bring Upon This Cursed Land");
            }
        }

        public float ReadFloatLossyAuto()
        {
            var cfg = _buffer.Read(3);
            if (cfg == 0)
                return ReadFloatLossless();
            var value = ReadFloatLossyAuto(cfg);
            if (_buffer.Read(1) == 1)
                value = -value;
            return value;
        }

        private float ReadQuantizedFloat(int mult, int bits)
        {
            return ((float) _buffer.Read(bits)) / mult;
        }

        public void WriteInt(int value, int bits)
        {
            if (value >= 0)
            {
                _buffer.Write(0, 1);
                _buffer.Write((ulong) value, bits-1);
            }
            else
            {
                _buffer.Write(1, 1);
                _buffer.Write((ulong) -++value, bits-1);
            }
        }

        public int ReadInt(int bits)
        {
            var sign = _buffer.Read(1);
            var value = (int) _buffer.Read(bits-1);
            if (sign == 1)
            {
                value = -value-1;
            }
            return value;
        }

        public ulong Read(int bits)
        {
            return _buffer.Read(bits);
        }

        public void Write(ulong value, int bits)
        {
            _buffer.Write(value,bits);
        }

        public void WriteUInt(uint value, int bits)
        {
            _buffer.Write(value, bits);
        }

        public uint ReadUInt(int bits)
        {
            var value = (uint) _buffer.Read(bits);
            return value;
        }

        #endregion

        #region Bytes

        public byte ReadByte()
        {
            return (byte) _buffer.Read(8);
        }

        public void WriteByte(byte value)
        {
            _buffer.Write(value, 8);
        }

        #endregion

        #region Booleans

        public void WriteBool(bool value)
        {
            _buffer.Write(value ? 1u : 0,1);
        }

        public bool ReadBool()
        {
            return _buffer.Read(1) == 1u;
        }

        #endregion

        #region Strings

        public void WriteString(string value, bool forceUtf8 = false)
        {
            var chars = value.ToCharArray();
            var set = forceUtf8 ? null :_stringEncoder.GetEncodingSet(chars);
            if (set == null) //encode with utf8
            {
                _buffer.Write(0, 3);
                byte[] bytes = Encoding.UTF8.GetBytes(chars);
                WriteStringLength(bytes.Length);
                foreach (var b in bytes)
                {
                    _buffer.Write(b, 8);
                }
            }
            else
            {
                _buffer.Write((ulong) set.Index, 3);
                WriteStringLength(chars.Length);
                set.Encode(_buffer, chars);
            }

        }

        private void WriteStringLength(int length)
        {
            if(length > short.MaxValue)
                throw new InvalidOperationException("String length exceeds allowed size!");
            _buffer.Write(length > 127 ? 1U : 0, 1); //write category: 0 = small string, 1 = big string
            _buffer.Write((ulong) length, length > 127 ? 15 : 7); //write length using bits allowed by category
        }

        public string ReadString()
        {
            var setIndex = _buffer.Read(3);
            if (setIndex == 0)
            {
                var length = ReadStringLength();
                var bytes = new byte[length];
                for (var i = 0; i < length; i++)
                {
                    bytes[i] = (byte) _buffer.Read(8);
                }

                return Encoding.UTF8.GetString(bytes);
            }
            else
            {
                var set = _stringEncoder.GetEncodingSet((int) setIndex);
                var length = ReadStringLength();
                return set.Decode(_buffer, length);
            }

        }

        private int ReadStringLength()
        {
            var category = _buffer.Read(1);
            return (int)_buffer.Read(category == 1 ? 15 : 7);
        }

        #endregion
    }
}
