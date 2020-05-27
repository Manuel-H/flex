using System.Collections.Generic;
using System.Linq;
using com.Dunkingmachine.FlexSerialization;
using com.Dunkingmachine.Utility;
using Random = UnityEngine.Random;

namespace MyPackages.BitSerialization.Tests.TestClasses
{
    public enum FantasyEnum
    {
        Bremm,
        Kworb,
        Bloorb
    }

    public class FantasyClass : DataBaseClass
    {
        public bool Bool;
        [FlexIgnore]
        public short Short;
        [FlexFloatRange(10,-10, 2)]
        public float Float;
        public FantasyEnum Enum;
        public int[] IntArray;
        public List<Miniclass> MiniclassesList;
        public Dictionary<int, Miniclass> MiniclassesDictionary;

        public static FantasyClass CreateRandom()
        {
            var enumRan = Random.value;
            return new FantasyClass
            {
                Bool = Random.value > 0.5f,
                Short = (short) Random.Range(short.MinValue, short.MaxValue),
                Float = Random.Range(-10f, 10f),
                Enum = enumRan > 0.333f ? enumRan > 0.666f ? FantasyEnum.Bloorb : FantasyEnum.Bremm : FantasyEnum.Kworb,
                IntArray = new int[Random.Range(1, 10)].Initialize(i => Random.Range(0, 100)),
                MiniclassesList = Enumerable.Range(0, Random.Range(1,10)).Select(i => Miniclass.CreateRandom()).ToList(),
                MiniclassesDictionary = Enumerable.Range(0, Random.Range(1,10)).ToDictionary(i => i, i => Miniclass.CreateRandom())
            };
        }
    }

    public class Miniclass
    {
        public bool MiniBool;
        public FantasyEnum MiniEnum;

        public static Miniclass CreateRandom()
        {
            var enumRan = Random.value;
            return new Miniclass
            {
                MiniBool = Random.value > 0.5f,
                MiniEnum = enumRan > 0.333f ? enumRan > 0.666f ? FantasyEnum.Bloorb : FantasyEnum.Bremm : FantasyEnum.Kworb,
            };
        }
    }
}