using System;
using System.Text;

namespace com.Dunkingmachine.BitSerialization
{
    public class BitSerializer
    {

        private readonly BitBuffer _buffer;
        public BitBuffer.Mode BufferMode => _buffer.BufferMode;
        public bool LastByte => _buffer.LastByte;

        public BitSerializer()
        {
            _buffer = new BitBuffer();
        }

        public BitSerializer(byte[] bytes)
        {
            _buffer = new BitBuffer(bytes);
        }

        public byte[] GetBytes()
        {
            return _buffer.GetBytes();
        }

        public void Skip(int bits)
        {
            _buffer.Skip(bits);
        }

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
        public void WriteVarInt(int value)
        {
            if (value >= 0)
            {
                _buffer.Write(0,1);
            }
            else
            {
                _buffer.Write(1,1);
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
                value = -value;
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
                value = -value;
            return value;
        }
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
                _buffer.Write((ulong) -value, bits-1);
            }
        }

        public int ReadInt(int bits)
        {
            var sign = _buffer.Read(1);
            var value = (int) _buffer.Read(bits-1);
            if (sign == 1)
                value = -value;
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
        
        public byte ReadByte()
        {
            return (byte) _buffer.Read(8);
        }

        public void WriteByte(byte value)
        {
            _buffer.Write(value, 8);
        }

        public void WriteBool(bool value)
        {
            _buffer.Write(value ? 1u : 0,1);
        }

        public bool ReadBool()
        {
            return _buffer.Read(1) == 1u;
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

        public void WriteString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var length = bytes.Length;
            if(length > short.MaxValue)
                throw new Exception("String length exceeds allowed size!");
            _buffer.Write(length > 127 ? 1U : 0, 1); //write category: 0 = small string, 1 = big string
            _buffer.Write((ulong) length, length > 127 ? 15 : 7); //write length using bits allowed by category
            foreach (var b in bytes)
            {
                _buffer.Write(b, 8);
            }
        }

        public string ReadString()
        {
            var category = _buffer.Read(1);
            var length = (int)_buffer.Read(category == 1 ? 15 : 7);
            var bytes = new byte[length];
            for (var i = 0; i < length; i++)
            {
                bytes[i] = (byte) _buffer.Read(8);
            }

            return Encoding.UTF8.GetString(bytes);
        }
    }
}