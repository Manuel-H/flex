using System;
using System.Diagnostics;
using System.Text;
using com.Dunkingmachine.BitSerialization;
using NUnit.Framework;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

namespace MyPackages.BitSerialization.Tests
{
    public class BitSerializerTests
    {
        private enum TestEnum
        {
            TestValueA,
            TestValueB,
            TestValueC,
            TestValueD,
            TestValueE,
        }

        [Test]
        public void WriteReadBool_BoolValues_ValueWrittenEqualsValueRead()
        {
            var writer = new BitSerializer();
            writer.WriteBool(false);
            writer.WriteBool(true);
            var bytes = writer.GetBytes();
            var reader = new BitSerializer(bytes);
            Assert.AreEqual(false, reader.ReadBool());
            Assert.AreEqual(true, reader.ReadBool());
        }

        [Test]
        public void WriteReadFloatLossyAuto_ArbitraryLossyFloatValues_ValueWrittenEqualsValueRead()
        {
            var writer = new BitSerializer();
            writer.WriteFloatLossyAuto(1.795f);
            writer.WriteFloatLossyAuto(0.0275f);
            writer.WriteFloatLossyAuto(-29.76f);
            writer.WriteFloatLossyAuto(56971f);
            writer.WriteFloatLossyAuto(0.00000145f);
            writer.WriteFloatLossyAuto(0);
            var bytes = writer.GetBytes();
            var reader = new BitSerializer(bytes);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 1.795f) < 0.0001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 0.0275f) < 0.00001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() + 29.76f) < 0.001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 56971f) < 0.00001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 0.00000145f) < 0.00001f);
            Assert.IsTrue(Math.Abs(reader.ReadFloatLossyAuto() - 0) < 0.001f);
        }

        [Test]
        public void WriteReadFloatLossless_ArbitraryFloatValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteFloatLossless(0.0125f);
            serializer.WriteFloatLossless(1/461611f);
            serializer.WriteFloatLossless(-1586.123f);
            serializer.WriteFloatLossless(0);
            serializer.WriteFloatLossless(-0.0125f);
            serializer.WriteFloatLossless(float.MaxValue);
            serializer.WriteFloatLossless(float.MinValue);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(0.0125f, serializer.ReadFloatLossless());
            Assert.AreEqual(1/461611f, serializer.ReadFloatLossless());
            Assert.AreEqual(-1586.123f, serializer.ReadFloatLossless());
            Assert.AreEqual(0, serializer.ReadFloatLossless());
            Assert.AreEqual(-0.0125f, serializer.ReadFloatLossless());
            Assert.AreEqual(float.MaxValue, serializer.ReadFloatLossless());
            Assert.AreEqual(float.MinValue, serializer.ReadFloatLossless());
        }

        [Test]
        public void WriteReadInt_ArbitraryIntValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteInt(1891, 12);
            serializer.WriteInt(-18151826, 30);
            serializer.WriteInt(-15, 6);
            serializer.WriteInt(2, 3);
            serializer.WriteInt(0, 4);
            serializer.WriteInt(int.MaxValue, sizeof(int) * 8 + 1);
            serializer.WriteInt(int.MinValue, sizeof(int) * 8 + 1);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(1891, serializer.ReadInt(12));
            Assert.AreEqual(-18151826, serializer.ReadInt(30));
            Assert.AreEqual(-15, serializer.ReadInt(6));
            Assert.AreEqual(2, serializer.ReadInt(3));
            Assert.AreEqual(0, serializer.ReadInt(4));
            Assert.AreEqual(int.MaxValue, serializer.ReadInt(sizeof(int) * 8 + 1));
            Assert.AreEqual(int.MinValue, serializer.ReadInt(sizeof(int) * 8 + 1));
        }

        [Test]
        public void WriteReadVarInt_ArbitraryIntValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteVarInt(17);
            serializer.WriteVarInt(2896);
            serializer.WriteVarInt(-37899644);
            serializer.WriteVarInt(0);
            serializer.WriteVarInt(int.MaxValue);
            serializer.WriteVarInt(int.MinValue);
            var bytes = serializer.GetBytes();
            serializer = new BitSerializer(bytes);
            Assert.AreEqual(17, serializer.ReadVarInt());
            Assert.AreEqual(2896, serializer.ReadVarInt());
            Assert.AreEqual(-37899644, serializer.ReadVarInt());
            Assert.AreEqual(0, serializer.ReadVarInt());
            Assert.AreEqual(int.MaxValue, serializer.ReadVarInt());
            Assert.AreEqual(int.MinValue, serializer.ReadVarInt());
        }

        [Test]
        public void WriteReadVarLong_ArbitraryLongValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteVarLong(17);
            serializer.WriteVarLong(2896);
            serializer.WriteVarLong(-37899644);
            serializer.WriteVarLong(14165154651856);
            serializer.WriteVarLong(0);
            serializer.WriteVarLong(long.MaxValue);
            serializer.WriteVarLong(long.MinValue);
            var bytes = serializer.GetBytes();
            serializer = new BitSerializer(bytes);
            Assert.AreEqual(17, serializer.ReadVarLong());
            Assert.AreEqual(2896, serializer.ReadVarLong());
            Assert.AreEqual(-37899644, serializer.ReadVarLong());
            Assert.AreEqual(14165154651856, serializer.ReadVarLong());
            Assert.AreEqual(0, serializer.ReadVarLong());
            Assert.AreEqual(long.MaxValue, serializer.ReadVarLong());
            Assert.AreEqual(long.MinValue, serializer.ReadVarLong());
        }

        [Test]
        public void WriteReadString_ArbitraryStringValues_ValueWrittenEqualsValueRead()
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

        [Test]
        public void WriteReadEnum_ArbitraryEnumValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteEnumInt((uint) TestEnum.TestValueA);
            serializer.WriteEnumInt((uint) TestEnum.TestValueB);
            serializer.WriteEnumInt((uint) TestEnum.TestValueC);
            serializer.WriteEnumInt((uint) TestEnum.TestValueD);
            serializer.WriteEnumInt((uint) TestEnum.TestValueE);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(TestEnum.TestValueA, (TestEnum) serializer.ReadEnumInt());
            Assert.AreEqual(TestEnum.TestValueB, (TestEnum) serializer.ReadEnumInt());
            Assert.AreEqual(TestEnum.TestValueC, (TestEnum) serializer.ReadEnumInt());
            Assert.AreEqual(TestEnum.TestValueD, (TestEnum) serializer.ReadEnumInt());
            Assert.AreEqual(TestEnum.TestValueE, (TestEnum) serializer.ReadEnumInt());
        }

        [Test]
        public void WriteReadByte_ArbitraryByteValues_ValueWrittenEqualsValueRead()
        {
            var serializer = new BitSerializer();
            serializer.WriteByte(0x23);
            serializer.WriteByte(0x00);
            serializer.WriteByte(0xFF);
            serializer.WriteByte(0xEA);
            serializer.WriteByte(0x5F);
            serializer = new BitSerializer(serializer.GetBytes());
            Assert.AreEqual(0x23, serializer.ReadByte());
            Assert.AreEqual(0x00, serializer.ReadByte());
            Assert.AreEqual(0xFF, serializer.ReadByte());
            Assert.AreEqual(0xEA, serializer.ReadByte());
            Assert.AreEqual(0x5F, serializer.ReadByte());
        }

        [Test]
        public void WriteRead_ArbitraryValues_ValueWrittenEqualsValueRead()
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
            Assert.Throws<InvalidOperationException>(() => serializer.WriteString(builder.ToString()));
        }
    }
}
