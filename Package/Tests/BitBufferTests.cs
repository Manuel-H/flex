using System;
using com.Dunkingmachine.BitSerialization;
using NUnit.Framework;

namespace MyPackages.BitSerialization.Tests
{
    [TestFixture]
    public class BitBufferTests
    {
        [Test]
        public void Read_ModeIsWrite_ThrowsInvalidOperationException()
        {
            // Create empty buffer -> mode is Mode.Write new byte[]{0x00, 0xea}
            var buffer = new BitBuffer();
            Assert.Throws<InvalidOperationException>(() => buffer.Read(4));
        }
        
        [Test]
        public void Read_MoreBitsThenBufferContains_ThrowsIndexOutOfRangeException()
        {
            var buffer = new BitBuffer(new byte[0x0]);
            Assert.Throws<IndexOutOfRangeException>(() => buffer.Read(4));
        }
        
        [Test]
        public void Read_AmountBiggerThanAllowed_ThrowsInvalidOperationException()
        {
            var buffer = new BitBuffer(new byte[0x0]);
            Assert.Throws<InvalidOperationException>(() => buffer.Read(sizeof(ulong) * 8 + 1));
        }
        
        [Test]
        public void Write_ModeIsRead_ThrowsInvalidOperationException()
        {
            // Create non-empty buffer -> mode is Mode.Read
            var buffer = new BitBuffer(new byte[]{0x00, 0xea});
            Assert.Throws<InvalidOperationException>(() => buffer.Write(123, 4));
        }
        
        [Test]
        public void Write_BufferAlreadyClosed_ThrowsInvalidOperationException()
        {
            var buffer = new BitBuffer();
            buffer.GetBytes();
            Assert.Throws<InvalidOperationException>(() => buffer.Write(123, 4));
        }

        [Test]
        public void CreateReadBuffer_BufferIsNull_ThrowsArgumentNullException()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Assert.Throws<ArgumentNullException>(() => new BitBuffer(null));
        }
    }
}