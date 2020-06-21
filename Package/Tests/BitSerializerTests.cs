using System;
using System.Diagnostics;
using System.Text;
using com.Dunkingmachine.BitSerialization;
using NUnit.Framework;
using UnityEditor;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MyPackages.BitSerialization.Tests
{
    public class BitSerializerTests
    {

        [Test]
        public void TestBitBuffer()
        {
            var writebuffer = new BitBuffer();
            writebuffer.Write(123, 7);
            writebuffer.Write(1337, 13);
            writebuffer.Write(69, 9);
            writebuffer.Write(123456789876543210, 59);
            writebuffer.Write(21, 6);
            var bytes = writebuffer.GetBytes();
            var readbuffer = new BitBuffer(bytes);
            
            Assert.AreEqual(123, readbuffer.Read(7));
            Assert.AreEqual(1337, readbuffer.Read(13));
            Assert.AreEqual(69, readbuffer.Read(9));
            Assert.AreEqual(123456789876543210, readbuffer.Read(59));
            Assert.AreEqual(21, readbuffer.Read(6));

            writebuffer = new BitBuffer();
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
            Debug.Log(watch.ElapsedMilliseconds);
            bytes = writebuffer.GetBytes();
            readbuffer = new BitBuffer(bytes);
            watch.Restart();
            for (int i = 0; i < 100000; i++)
            {
                 readbuffer.Read(7);
                 readbuffer.Read(13);
                 readbuffer.Read(9);
                 readbuffer.Read(59);
                 readbuffer.Read(6);
            }

            Debug.Log(watch.ElapsedMilliseconds);
        }

        [Test]
        public void TestBoolSerialization()
        {
            var writer = new BitSerializer();
            writer.WriteBool(true);
            writer.WriteBool(1 == 0);
            writer.WriteBool(false);
            var bytes = writer.GetBytes();
            var reader = new BitSerializer(bytes);
            Assert.AreEqual(true, reader.ReadBool());
            Assert.AreEqual(1==0, reader.ReadBool());
            Assert.AreEqual(false, reader.ReadBool());
        }

        [Test]
        public void TestLossyFloatSerialization()
        {
            var writer = new BitSerializer();
            writer.WriteFloatLossyAuto(1.795f);
            writer.WriteFloatLossyAuto(0.0275f);
            writer.WriteFloatLossyAuto(-29.76f);
            writer.WriteFloatLossyAuto(56971f);
            writer.WriteFloatLossyAuto(0.00000145f);
            var bytes = writer.GetBytes();
            var reader = new BitSerializer(bytes);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 1.795f) < 0.0001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 0.0275f) < 0.00001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() + 29.76f) < 0.001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 56971f) < 0.00001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 0.00000145f) < 0.00001f);
        }
        
        [Test]
        public void TestFloatSerialization()
        {
            var serializer = new BitSerializer();
            serializer.WriteFloatLossless(0.0125f);
            serializer.WriteFloatLossless(1/461611f);
            serializer.WriteFloatLossless(-1586.123f);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(0.0125f, serializer.ReadFloatLossless());
            Assert.AreEqual(1/461611f, serializer.ReadFloatLossless());
            Assert.AreEqual(-1586.123f, serializer.ReadFloatLossless());
        }

        [Test]
        public void TestIntSerialization()
        {
            var serializer = new BitSerializer();
            serializer.WriteInt(1891, 12);
            serializer.WriteInt(-18151826, 30);
            serializer.WriteInt(-15, 6);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(1891, serializer.ReadInt(12));
            Assert.AreEqual(-18151826, serializer.ReadInt(30));
            Assert.AreEqual(-15, serializer.ReadInt(6));
        }

        [Test]
        public void TestVarIntSerialization()
        {
            var serializer = new BitSerializer();
            serializer.WriteVarInt(17);
            serializer.WriteVarInt(2896);
            serializer.WriteVarInt(-37899644);
            serializer.WriteVarLong(14165154651856);
            var bytes = serializer.GetBytes();
            serializer = new BitSerializer(bytes);
            Assert.AreEqual(17, serializer.ReadVarInt());
            Assert.AreEqual(2896, serializer.ReadVarInt());
            Assert.AreEqual(-37899644, serializer.ReadVarInt());
            Assert.AreEqual(14165154651856, serializer.ReadVarLong());
        }

        [Test]
        public void TestStringSerialization()
        {
            var serializer = new BitSerializer();
            serializer.WriteString("hello");
            serializer.WriteString("There once was a man from Peru;who dreamed he was eating his shoe. He woke with a fright in the middle of the night to find that his dream had come true.");
            serializer.WriteString("");
            serializer.WriteString("9SD)=js5adf09ü'*df0ß");
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual("hello", serializer.ReadString());
            Assert.AreEqual("There once was a man from Peru;who dreamed he was eating his shoe. He woke with a fright in the middle of the night to find that his dream had come true.", serializer.ReadString());
            Assert.AreEqual("", serializer.ReadString());
            Assert.AreEqual("9SD)=js5adf09ü'*df0ß", serializer.ReadString());
        }

        [Test, MenuItem("BitserializerTest/TestStringPerformance")]
        public static void TestStringPerformance()
        {
            StringPerformance("9SD)=js5adf09ü'*df0ß9SD)=js5adf0There once was a man from Peru;who dreamed he was eating his shoe. He woke with a fright in the middle of the night to find that his dream had come true.9ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adThere once was a man from Peru;who dreamed he was eating his shoe. He woke with a fright in the middle of the night to find that his dream had come true.f09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß9SD)=js5adf09ü'*df0ß");
        }

        private static void StringPerformance(string teststring)
        {
            var sw = new Stopwatch();
            var serializer = new BitSerializer();
            sw.Start();
            for (int i = 0; i < 1000; i++)
            {
                serializer.WriteString(teststring);
            }
            sw.Stop();
            var bytes = serializer.GetBytes();
            Debug.Log("Flex: "+bytes.Length+" bytes | "+sw.ElapsedMilliseconds+" ms");
            double length = bytes.Length;
            serializer = new BitSerializer();
            sw.Restart();
            for (int i = 0; i < 1000; i++)
            {
                serializer.WriteString(teststring, true);
            }
            sw.Stop();
            bytes = serializer.GetBytes();
            Debug.Log("Utf8: "+bytes.Length+" bytes | "+sw.ElapsedMilliseconds+" ms");
            Debug.Log("Compression: "+(1-length/bytes.Length).ToString("P1"));
        }
        [Test]
        public void TextMixedSerialization()
        {
            var serializer = new BitSerializer();
            for (int i = 0; i < 100; i++)
            {
                serializer.WriteInt(-18151826, 26);
                serializer.WriteString("9SD)=js5adf09ü'*df0ß");
                serializer.WriteFloatLossless(1 / 461611f);
                serializer.WriteBool(true);
                serializer.WriteVarInt(-378996);
            }
            var bytes = serializer.GetBytes();
            serializer = new BitSerializer(bytes);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(-18151826, serializer.ReadInt(26));
                Assert.AreEqual("9SD)=js5adf09ü'*df0ß", serializer.ReadString());
                Assert.AreEqual(1/461611f, serializer.ReadFloatLossless());
                Assert.AreEqual(true, serializer.ReadBool());
                Assert.AreEqual(-378996, serializer.ReadVarInt());
            }
        }
        
        [Test]
        public void WriteString_StringExceedsMaxLength_ExceptionIsThrown()
        {
            // Generate string that is longer than short.MaxValue
            var builder = new StringBuilder(short.MaxValue + 1);
            for (var i = 0; i < short.MaxValue + 1; i++)
            {
                builder.Append((char) Random.Range('a', 'Z'));
            }
            
            var serializer = new BitSerializer();
            Assert.Throws<Exception>(() => serializer.WriteString(builder.ToString()));
        }
    }
}
