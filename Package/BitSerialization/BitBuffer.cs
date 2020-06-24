#region

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

#endregion

namespace com.Dunkingmachine.BitSerialization
{
    internal class BitBuffer
    {
        public enum Mode { Read, Write }

        private readonly List<byte> _listBuffer;
        private int _bitIndex;

        private byte[] _buffer;
        private readonly int _bufferLength;
        private bool _closed;
        private int _index;

        /// <summary>
        ///     Creates an empty, default BitBuffer with Write-Mode
        /// </summary>
        public BitBuffer()
        {
            BufferMode = Mode.Write;
            _listBuffer = new List<byte> {0x0};
        }

        /// <summary>
        ///     Creates a default BitBuffer from the given byte buffer with Read-Mode
        /// </summary>
        /// <param name="byteBuffer"></param>
        /// <exception cref="ArgumentNullException">Thrown when calling constructor with null value</exception>
        public BitBuffer(byte[] byteBuffer)
        {
            BufferMode = Mode.Read;
            _buffer = byteBuffer ??
                      throw new ArgumentNullException($"Cannot initialize {nameof(BitBuffer)} with a null value!");
            _closed = true;
            _bufferLength = _buffer.Length * 8;
        }

        /// <summary>
        ///     True when buffer reached the last byte
        /// </summary>
        public bool LastByte => _index == _buffer.Length - 1;

        /// <summary>
        ///     Buffer mode <see cref="Mode" /> is set when creating the buffer object and can not be changed afterwards!
        /// </summary>
        public Mode BufferMode { get; }

        /// <summary>
        ///     Skips the given amount of bits from the current buffer position by setting the current buffer index accordingly
        /// </summary>
        /// <param name="amount">Number of bits to skip in this buffer</param>
        public void Skip(int amount)
        {
            IncrementIndex(amount);
        }

        /// <summary>
        ///     Reads the given amount of bits from the buffer and returns their binary value
        /// </summary>
        /// <param name="amount">Amount of bits that should be read</param>
        /// <returns>
        ///     Binary value of the amount of the specified bits
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///     Throws when not in read mode.
        ///     Throws when <paramref name="amount"/> is bigger than size of an ulong
        /// </exception>
        public ulong Read(int amount)
        {
            if (BufferMode != Mode.Read)
                throw new InvalidOperationException("BitBuffer not in read-mode!");

            if (amount > sizeof(ulong) * 8)
                throw new InvalidOperationException(
                    $"Cannot read more than {sizeof(ulong) * 8} bits at once. Amount of bits specified: {amount}");

            if (_index + amount > _bufferLength)
                throw new IndexOutOfRangeException("Cannot read more bits from buffer than that are left!\n" +
                                                   $"Current index: {_index}, buffer length: {_bufferLength}, amount: {amount}");

            var leftInCurrentByte = 8 - _bitIndex;
            ulong result;
            if (amount <= leftInCurrentByte)
            {
                result = ((ulong) _buffer[_index] >> (leftInCurrentByte - amount)) & ((1u << amount) - 1);
                IncrementIndex(amount);
            }
            else
            {
                var leftToRead = amount - leftInCurrentByte;
                result = (ulong) _buffer[_index] & ((1u << leftInCurrentByte) - 1);
                IncrementIndex(leftInCurrentByte);
                while (leftToRead > 8)
                {
                    result |= (ulong) _buffer[_index] << (amount - leftToRead);
                    leftToRead -= 8;
                    IncrementIndex(8);
                }

                result |= ((ulong) _buffer[_index] >> (8 - leftToRead)) << (amount - leftToRead);
                IncrementIndex(leftToRead);
            }

            return result;
        }

        /// <summary>
        ///     Writes a value for the given amount of bits. For performance reasons it's not being checked
        ///     whether the amount of bits is sufficient for the given value
        /// </summary>
        /// <param name="bits">value to write into the buffer</param>
        /// <param name="amount">Amount of bits this value shall reserve</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Write(ulong bits, int amount)
        {
            if (BufferMode != Mode.Write)
                throw new InvalidOperationException("BitBuffer not in write-mode!");
            if (_closed)
                throw new InvalidOperationException("BitBuffer is closed!");

            var leftInCurrentByte = 8 - _bitIndex;
            if (amount <= leftInCurrentByte)
            {
                _listBuffer[_index] = (byte) ((_listBuffer[_index] << amount) | ((byte) bits & ((1 << amount) - 1)));
                IncrementIndex(amount);
                return;
            }

            _listBuffer[_index] = (byte) ((_listBuffer[_index] << leftInCurrentByte) |
                                          ((byte) bits & ((1 << leftInCurrentByte) - 1)));
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

        /// <summary>
        ///     If not yet closed, closes the buffer and returns its bytes
        /// </summary>
        /// <returns></returns>
        public byte[] GetBytes()
        {
            if (!_closed)
                Close();
            return _buffer;
        }

        private void Close()
        {
            if (_closed) return;

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
