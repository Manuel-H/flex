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
        
        public void WriteVarInt(long value)
        {
            if (value >= 0)
            {
                _buffer.Write(0,1);
            }
            else
            {
                _buffer.Write(1,1);
                value *= -1;
            }
            
            do
            {
                _buffer.Write((ulong) (value & 127), 7);
                value >>= 7;
                _buffer.Write(value > 0 ? 1u : 0, 1);
            } while (value > 0);
        }

        public long ReadVarInt()
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
                value *= -1;
            return value;
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
                _buffer.Write((ulong) (value * -1), bits-1);
            }
        }

        public int ReadInt(int bits)
        {
            var sign = _buffer.Read(1);
            var value = (int) _buffer.Read(bits-1);
            if (sign == 1)
                value *= -1;
            return value;
        }
        
        public void WriteUInt(uint value, int bits)
        {
            _buffer.Write((ulong) (value * -1), bits);
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

        public void WriteByite(byte value)
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