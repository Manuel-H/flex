using com.Dunkingmachine.FlexSerialization;
using NUnit.Framework;

namespace MyPackages.BitSerialization.Tests
{
    [TestFixture]
    public class FlexSerializerTests
    {
        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        [TestCase(511)]
        public void WriteReadMemberId_ValidValues_ValueReadEqualsValuesWritten(int id)
        {
            var writeSerializer = new FlexSerializer();
            writeSerializer.WriteMemberId(id);
            var readSerializer = new FlexSerializer(writeSerializer.GetBytes());
            Assert.AreEqual(id, readSerializer.ReadMemberId());
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        [TestCase(511)]
        public void WriteReadTypeId_ValidValues_ValueReadEqualsValuesWritten(int id)
        {
            var writeSerializer = new FlexSerializer();
            writeSerializer.WriteTypeId(id);
            var readSerializer = new FlexSerializer(writeSerializer.GetBytes());
            Assert.AreEqual(id, readSerializer.ReadTypeId());
        }

        [Test]
        [TestCase(-1)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        [TestCase(10_000)]
        [TestCase(32_768)]
        public void WriteReadArrayLength_ValidValues_ValueReadEqualsValuesWritten(int length)
        {
            var writeSerializer = new FlexSerializer();
            writeSerializer.WriteArrayLength(length);
            var readSerializer = new FlexSerializer(writeSerializer.GetBytes());
            Assert.AreEqual(length, readSerializer.ReadArrayLength());
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(100)]
        [TestCase(511)]
        public void WriteReadTypeIndex_ValidValues_ValueReadEqualsValuesWritten(int id)
        {
            var writeSerializer = new FlexSerializer();
            writeSerializer.WriteTypeIndex(id);
            var readSerializer = new FlexSerializer(writeSerializer.GetBytes());
            Assert.AreEqual(id, readSerializer.ReadTypeIndex());
        }

        // TODO: Tests for Write/Read Token
    }
}
