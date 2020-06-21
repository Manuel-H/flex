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
                },
                // new EncodingLevel
                // {
                //     Sets = new[]
                //     {
                //         new EncodingSet(ind++, 7,
                //             "qwertzuiopasdfghjklyxcvbnmQWERTZUIOPASDFGHJKLYXCVBNM0123456789,.-;:_/*-+#~'!?`´<>\"°$@&%$()[]|{}€°^\\ßíóéáñúüöäàèêçôùâûîòìóœïëæ π="),
                //     }
                // },
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
                };
        }
        
        private HashSet<char> _seenCharCache = new HashSet<char>(CharEqualityComparer.Instance);
        public EncodingSet GetEncodingSet(char[] chars)
        {
            int levelAIndex = 0;
            var levelBIndex = 0;
            var levelA = _levelsA[levelAIndex];
            var levelB = _levelsB[levelBIndex];
            levelA.Reset();
            levelB.Reset();
            _seenCharCache.Clear();
            foreach (var c in chars)
            {
                if(_seenCharCache.Contains(c))
                    continue;
                _seenCharCache.Add(c);
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
                if (set.SetString.IndexOf(c) > -1)
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
            Bits = bits;
            SetString = setString;
            BytesByChar = new Dictionary<char, byte>(setString.Length, CharEqualityComparer.Instance);
            CharsByByte = new Dictionary<byte, char>(setString.Length);
            var array = setString.ToCharArray();
            for (byte i = 0; i < array.Length; i++)
            {
                BytesByChar[array[i]] = i;
                CharsByByte[i] = array[i];
            }
        }

        public bool NeedReset;
        public bool Valid;
        public int Index;
        public int Bits;
        public Dictionary<char, byte> BytesByChar;
        public Dictionary<byte, char> CharsByByte;
        public string SetString;

        public void Encode(BitBuffer ser, char[] chars)
        {
            foreach (var c in chars)
            {
                ser.Write(BytesByChar[c], Bits);
            }
        }

        public string Decode(BitBuffer ser, int length)
        {
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = CharsByByte[(byte) ser.Read(Bits)];
            }

            return new string(chars);
        }
    }
}