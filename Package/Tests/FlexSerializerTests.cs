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
        public void WriteReadMemberId_GivenValues_ValueReadEqualsValuesWritten(int id)
        {
            var writeSerializer = new FlexSerializer();
            writeSerializer.WriteMemberId(id);
            var readSerializer = new FlexSerializer(writeSerializer.GetBytes());
            Assert.AreEqual(id, readSerializer.ReadMemberId());
        }
    }
}
