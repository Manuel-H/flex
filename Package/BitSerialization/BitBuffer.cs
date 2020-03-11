using System;
using System.Collections.Generic;

namespace com.Dunkingmachine.BitSerialization
{
    public class BitBuffer
    {
        public enum Mode {Read,Write}
        
        public Mode BufferMode { get; }

        private byte[] _buffer;
        private readonly List<byte> _listBuffer;
        private int _index;
        private int _bitIndex;
        private bool _closed;

        public BitBuffer()
        {
            BufferMode = Mode.Write;
            _listBuffer = new List<byte>{0x0};
        }

        public BitBuffer(byte[] byteBuffer)
        {
            BufferMode = Mode.Read;
            _buffer = byteBuffer;
            _closed = true;
        }

        public ulong Read(int amount)
        {
            if(BufferMode != Mode.Read)
                throw new Exception("BitBuffer not in read-mode!");
            var leftInCurrentByte = 8 - _bitIndex;
            ulong result;
            if (amount <= leftInCurrentByte)
            {
                result =  ((ulong)_buffer[_index] >> (leftInCurrentByte-amount)) & ((1u << amount) - 1);
                IncrementIndex(amount);
            }
            else
            {
                var leftToRead = amount-leftInCurrentByte;
                result =  (ulong)_buffer[_index] & ((1u << leftInCurrentByte)-1);
                IncrementIndex(leftInCurrentByte);
                while (leftToRead > 8)
                {
                    result |= (ulong)_buffer[_index] << (amount - leftToRead);
                    leftToRead -= 8;
                    IncrementIndex(8);
                }
                
                result |=  ((ulong)_buffer[_index] >> (8 - leftToRead)) << (amount - leftToRead);
                IncrementIndex(leftToRead);
            }
            return result;
        }

        public void Write(ulong bits, int amount)
        {
            if(BufferMode != Mode.Write)
                throw new Exception("BitBuffer not in write-mode!");
            if(_closed)
                throw new Exception("BitBuffer is closed!");
            
            var leftInCurrentByte = 8 - _bitIndex;
            if (amount <= leftInCurrentByte)
            {
                _listBuffer[_index] = (byte) ((_listBuffer[_index] << amount) | (byte) bits & ((1 << amount) - 1));
                IncrementIndex(amount);
                return;
            }
            
            _listBuffer[_index] = (byte) ((_listBuffer[_index] << leftInCurrentByte) | (byte) bits & ((1 << leftInCurrentByte) - 1));
            IncrementIndex(leftInCurrentByte);
            var leftToWrite = amount - leftInCurrentByte;
            while (leftToWrite > 8)
            {
                _listBuffer[_index] = (byte) (bits >> (amount - leftToWrite));
                IncrementIndex(8);
                leftToWrite -= 8;
            }

            _listBuffer[_index] = (byte) (bits >> (amount - leftToWrite));
            IncrementIndex(leftToWrite);
        }

        public byte[] GetBytes()
        {
            if(!_closed)
                Close();
            return _buffer;
        }

        private void Close()
        {
            _closed = true;
            _listBuffer[_index] <<= 8 - _bitIndex;
            _buffer = _listBuffer.ToArray();
        }

        private void IncrementIndex(int amount)
        {
            _bitIndex += amount;
            while (_bitIndex > 7)
            {
                _bitIndex -= 8;
                _index++;
                if (BufferMode == Mode.Write)
                    _listBuffer.Add(0x0);
            }
            
        }
    }
}