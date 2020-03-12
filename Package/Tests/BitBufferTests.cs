using System;
using com.Dunkingmachine.BitSerialization;
using NUnit.Framework;

namespace MyPackages.BitSerialization.Tests
{
    [TestFixture]
    public class BitBufferTests
    {
        [Test]
        public void Read_ModeIsWrite_ThrowsException()
        {
            // Create empty buffer -> mode is Mode.Write new byte[]{0x00, 0xea}
            var buffer = new BitBuffer();
            Assert.Throws<Exception>(() => buffer.Read(4));
        }
        
        [Test]
        public void Write_ModeIsRead_ThrowsException()
        {
            // Create non-empty buffer -> mode is Mode.Read
            var buffer = new BitBuffer(new byte[]{0x00, 0xea});
            Assert.Throws<Exception>(() => buffer.Write(123, 4));
        }
        
        [Test]
        public void Write_BufferAlreadyClosed_ThrowsException()
        {
            var buffer = new BitBuffer();
            buffer.GetBytes();
            Assert.Throws<Exception>(() => buffer.Write(123, 4));
        }
    }
}