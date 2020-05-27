using com.Dunkingmachine.FlexSerialization;
using NUnit.Framework;
using UnityEngine;

namespace MyPackages.BitSerialization.Tests
{
    public class FlexMetaTests
    {
        [Test]
        public void TestDebug()
        {
            var builder = new FlexBuilder();
            builder.Build(GetType().Assembly, Application.dataPath + "/Editor/FlexTestData", "");
        }
    }
}