using System;
using System.Collections.Generic;
using System.Linq;
using com.Dunkingmachine.BitSerialization;

namespace com.Dunkingmachine.Utility
{
    public class StringEncoder
    {
        private readonly EncodingLevel[] _levelsA;
        private readonly EncodingLevel[] _levelsB;

        public StringEncoder()
        {
            int ind = 1;
            _levelsA = new[]
            {
                new EncodingLevel
                {
                    Sets = new[]
                    {
                        new EncodingSet(ind++, 4, "etaionsrhlcdumfp"), //16 common letters
                    }
                },
                new EncodingLevel
                {
                    Sets = new[]
                    {
                        new EncodingSet(ind++, 5, "qwertzuiopasdfghjklyxcvbnm ,.'!?"), //simple lowercase sentence
                    }
                },
                new EncodingLevel
                {
                    Sets = new[]
                    {
                        new EncodingSet(ind++, 6, "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM ,.-'!?\":)(+"), //mixed case sentence
                    }
                }
            };
            _levelsB = new[]
                {
                    new EncodingLevel
                    {
                        Sets = new []
                        {
                            new EncodingSet(ind++,4,"0123456789abcdef"), //hexadecimal set
                        }
                    },
                    new EncodingLevel
                    {
                        Sets = new []
                        {
                            new EncodingSet(ind++, 5, "ertuiopasdfghjklycbnm0123456789_"), //lowercase keys
                        }
                    },
                    new EncodingLevel
                    {
                        Sets = new []
                        {
                            new EncodingSet(ind++, 6, "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM0123456789_."), //mixed case keys
                        }
                    },
                    
                    new EncodingLevel
                    {
                        Sets = new[]
                        {
                            new EncodingSet(ind++, 7,
                                "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM0123456789,.-;:_/*-+#~'!?`´<>\"°$@&%$()[]|{}°^\\ßíóéáñúüöäàèêçôùâûîòìóœïëæ =¿¡"),
                        }
                    },
                };
            _seenCacheSize = _levelsA.Concat(_levelsB).Max(l => l.Sets.Max(s => s.MaxChar))+1;
            _seenCharCache = new bool[_seenCacheSize];
        }

        private void ClearSeenCache()
        {
            // for (var i = 0; i < _maxSeenChar+1; i++)
            // {
            //     _seenCharCache[i] = false;
            // }

            Array.Clear(_seenCharCache, 0, _maxSeenChar+1);
            _maxSeenChar = 50;
        }

        private readonly bool[] _seenCharCache;
        private readonly int _seenCacheSize;
        private int _maxSeenChar = 50;
        public EncodingSet GetEncodingSet(char[] chars)
        {
            int levelAIndex = 0;
            var levelBIndex = 0;
            var levelA = _levelsA[levelAIndex];
            var levelB = _levelsB[levelBIndex];
            levelA.Reset();
            levelB.Reset();
            ClearSeenCache();
            foreach (var c in chars)
            {
                if (c >= _seenCacheSize)
                    return null;
                if (_seenCharCache[c])
                    continue;
                if (c > _maxSeenChar)
                    _maxSeenChar = c;
                _seenCharCache[c] = true;
                while (levelA != null && !levelA.Matches(c))
                {
                    if (levelAIndex >= _levelsA.Length)
                    {
                        levelA = null;
                        break;
                    }
                    levelA = _levelsA[levelAIndex];
                    levelA.Reset();
                    levelAIndex++;
                }
                while (levelB != null && !levelB.Matches(c))
                {
                    if (levelBIndex >= _levelsB.Length)
                    {
                        levelB = null;
                        break;
                    }
                    levelB = _levelsB[levelBIndex];
                    levelB.Reset();
                    levelBIndex++;
                }
            }

            if (levelA != null)
            {
                foreach (var encodingSet in levelA.Sets)
                {
                    if (encodingSet.Valid)
                        return encodingSet;
                }
            }
            
            if (levelB != null)
            {
                foreach (var encodingSet in levelB.Sets)
                {
                    if (encodingSet.Valid)
                        return encodingSet;
                }
            }
            

            return null;
        }

        public EncodingSet GetEncodingSet(int index)
        {
            foreach (var encodingLevel in _levelsA)
            {
                foreach (var encodingSet in encodingLevel.Sets)
                {
                    if (encodingSet.Index == index)
                        return encodingSet;
                }
            }
            foreach (var encodingLevel in _levelsB)
            {
                foreach (var encodingSet in encodingLevel.Sets)
                {
                    if (encodingSet.Index == index)
                        return encodingSet;
                }
            }

            return null;
        }
    }

    class CharEqualityComparer : IEqualityComparer<char>
    {
        public static CharEqualityComparer Instance => _instance ?? (_instance = new CharEqualityComparer());
        private static CharEqualityComparer _instance;
        public bool Equals(char x, char y)
        {
            return x == y;
        }

        public int GetHashCode(char obj)
        {
            return obj;
        }
    }

    public class EncodingLevel
    {
        public void Reset()
        {
            foreach (var encodingSet in Sets)
            {
                encodingSet.Valid = true;
            }
        }

        public bool Matches(char c)
        {
            var valid = false;
            foreach (var set in Sets)
            {
                if (!set.Valid)
                    continue;
                if (c <= set.MaxChar && set.CharContains[c])
                    valid = true;
                else set.Valid = false;
            }

            return valid;
        }

        public EncodingSet[] Sets;
    }

    public class EncodingSet
    {
        public EncodingSet(int index, int bits, string setString)
        {
            if(setString.Length != (1 << bits))
                throw new Exception("string has "+setString.Length+" chars but should have "+(1<<bits));
            Index = index;
            _bits = bits;
            _chars = setString.ToCharArray();
            MaxChar = _chars.Max();
            CharContains = new bool[MaxChar+1];
            _bytes = new byte[MaxChar+1];
            for (byte i = 0; i < _chars.Length; i++)
            {
                var c = _chars[i];
                CharContains[c] = true;
                _bytes[c] = i;
            }
        }
        
        public bool Valid;
        public readonly int Index;
        private readonly int _bits;
        private readonly char[] _chars;
        private readonly byte[] _bytes;

        public readonly bool[] CharContains;
        public readonly int MaxChar;

        public void Encode(BitBuffer ser, char[] chars)
        {
            foreach (var c in chars)
            {
                ser.Write(_bytes[c], _bits);
            }
        }

        public string Decode(BitBuffer ser, int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = _chars[(byte) ser.Read(_bits)];
            }

            return new string(chars);
        }
    }
}