using System;
using System.Diagnostics;
using com.Dunkingmachine.BitSerialization;
using NUnit.Framework;

namespace MyPackages.BitSerialization.Tests
{
    [TestFixture]
    public class BitBufferTests
    {
        [Test]
        public void Write_ArbitraryValues_ValueIsCorrectAfterRead()
        {
            var writebuffer = new BitBuffer();
            writebuffer.Write(123, 7);
            writebuffer.Write(1337, 13);
            writebuffer.Write(69, 9);
            writebuffer.Write(21, 6);

            var bytes = writebuffer.GetBytes();
            var readbuffer = new BitBuffer(bytes);

            Assert.AreEqual(123, readbuffer.Read(7));
            Assert.AreEqual(1337, readbuffer.Read(13));
            Assert.AreEqual(69, readbuffer.Read(9));
            Assert.AreEqual(21, readbuffer.Read(6));
        }

        [Test]
        public void Write_SuperBigValue_ValueIsCorrectAfterRead()
        {
            var writebuffer = new BitBuffer();
            writebuffer.Write(123456789876543210, 59);
            var bytes = writebuffer.GetBytes();
            var readbuffer = new BitBuffer(bytes);
            Assert.AreEqual(123456789876543210, readbuffer.Read(59));
        }

        [Test]
        public void WriteRead_PerformanceTest()
        {
            var writebuffer = new BitBuffer();
            var watch = new Stopwatch();
            watch.Start();
            for (int i = 0; i < 100000; i++)
            {
                writebuffer.Write(123, 7);
                writebuffer.Write(1337, 13);
                writebuffer.Write(69, 9);
                writebuffer.Write(123456789876543210, 59);
                writebuffer.Write(21, 6);
            }
            watch.Stop();
            UnityEngine.Debug.Log($"Writing 500k values to buffer took {watch.ElapsedMilliseconds}ms");
            var bytes = writebuffer.GetBytes();
            var readbuffer = new BitBuffer(bytes);
            watch.Restart();
            for (int i = 0; i < 100000; i++)
            {
                readbuffer.Read(7);
                readbuffer.Read(13);
                readbuffer.Read(9);
                readbuffer.Read(59);
                readbuffer.Read(6);
            }

            UnityEngine.Debug.Log($"Reading 500k values from buffer took {watch.ElapsedMilliseconds}ms");
        }

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
