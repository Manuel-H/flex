using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using com.Dunkingmachine.Utility;

namespace com.Dunkingmachine.FlexSerialization
{
    public static class FlexConvert
    {
        public static class Initializer
        {
            public static void Initialize(Dictionary<Type, Action<object, FlexSerializer>> serializeMethods, Dictionary<Type, Func<object, FlexSerializer, object>> deserializeMethods, Dictionary<int, Type> ids)
            {
                _serializeMethods = serializeMethods;
                _deserializeMethods = deserializeMethods;
                _typesById = ids;
                _idsByType = ids.ToDictionary(i => i.Value, i => i.Key);
            }
        }

        private static Dictionary<Type, int> _idsByType;
        private static Dictionary<int, Type> _typesById;
        private static Dictionary<Type, Func<object, FlexSerializer, object>> _deserializeMethods;
        private static Dictionary<Type, Action<object, FlexSerializer>> _serializeMethods;

        public static byte[] Serialize<T>(T item)
        {
            if (!_serializeMethods.TryGetValue(typeof(T), out var action))
            {
                if(item is IList)
                    throw new FlexException("Use SerializeList for lists!");
                throw new FlexException("No serializer created for type "+nameof(T));
            }
            var ser = new FlexSerializer();
            action(item, ser);
            return ser.GetBytes();
        }
        
        public static T Deserialize<T>(byte[] bytes)
        {
            if (!_deserializeMethods.TryGetValue(typeof(T), out var func))
            {
                if(typeof(IList).IsAssignableFrom(typeof(T)))
                    throw new FlexException("Use DeserializeList for lists!");
                throw new FlexException("No serializer created for type "+nameof(T));
            }
            var ser = new FlexSerializer(bytes);
            return (T) func(bytes, ser);
        }

        private struct ElementInfo
        {
            public object Object;
            public Type Type;
            public byte Ind;
        }
        public static byte[] SerializeCollection<T>(ICollection<T> list)
        {
            var elements = new ElementInfo[list.Count];
            var indexMap = new Dictionary<Type, byte>();
            byte count = 0;
            var i = 0;
            //first collect all necessary information for encoding
            foreach (var x1 in list)
            {
                var type = x1.GetType();
                //all types are mapped to an index for reduced memory imprint
                if (!indexMap.TryGetValue(type, out var ind))
                {
                    ind = count++;
                    indexMap[type] = ind;
                }
                elements[i] = new ElementInfo
                {
                    Ind = ind,
                    Type = type,
                    Object = x1
                };
                i++;
            }
            
            var ser = new FlexSerializer();
            var mapCount = (byte)indexMap.Count;
            ser.WriteArrayLength(mapCount);
            var bits = ((byte)(mapCount-1)).GetSignificantBits();
            var writeIndex = mapCount > 1;
            //all indices are mapped to their types unique type id and serialized
            foreach (var mapEntry in indexMap)
            {
                ser.Write(mapEntry.Value, bits);
                ser.WriteTypeId(_idsByType[mapEntry.Key]);
            }
            ser.WriteArrayLength(list.Count);
            foreach (var element in elements)
            {
                if (!_serializeMethods.TryGetValue(element.Type, out var action))
                {
                    throw new FlexException("No serializer created for type " + element.Type.FullName);
                }
                if(writeIndex)
                    ser.Write(element.Ind, bits);
                action(element.Object, ser);
            }
            return ser.GetBytes();
        }

        public static T[] DeserializeArray<T>(byte[] bytes)
        {
            var ser = new FlexSerializer(bytes);
            var mapSize = (byte)ser.ReadArrayLength();
            var bits = ((byte)(mapSize-1)).GetSignificantBits();
            var indexMap = new Dictionary<byte, Type>();
            for (int i = 0; i < mapSize; i++)
            {
                var ind = (byte)ser.Read(bits);
                var id = ser.ReadTypeId();
                indexMap[ind] = _typesById[id];
            }

            var arraySize = ser.ReadArrayLength();
            var array = new T[arraySize];
            if (mapSize == 1) //skips type / deserialize method lookup for every type
            {
                var type = indexMap.First().Value;
                if (!_deserializeMethods.TryGetValue(type, out var func))
                {
                    if(typeof(IList).IsAssignableFrom(type))
                        throw new FlexException("Use DeserializeList for lists!");
                    throw new FlexException("No serializer created for type "+type.FullName);
                }

                for (var i = 0; i < arraySize; i++)
                {
                    array[i] = (T) func(null, ser);
                }
            }
            else
            {
                for (int i = 0; i < arraySize; i++)
                {
                    var type = indexMap[(byte) ser.Read(bits)];
                    if (!_deserializeMethods.TryGetValue(type, out var func))
                    {
                        if(typeof(IList).IsAssignableFrom(type))
                            throw new FlexException("Use DeserializeList for lists!");
                        throw new FlexException("No serializer created for type "+type.FullName);
                    }

                    array[i] = (T) func(null, ser);
                }
            }
            return array;
        }
        
        //TODO
        // public static List<T> DeserializeList<T>(byte[] bytes)
        // {
        //     if (!_deserializeMethods.TryGetValue(typeof(T), out var func))
        //     {
        //         if(typeof(IList).IsAssignableFrom(typeof(T)))
        //             throw new FlexException("Use DeserializeList for lists!");
        //         throw new FlexException("No serializer created for type "+nameof(T));
        //     }
        //     var ser = new FlexSerializer(bytes);
        //     var length = ser.ReadArrayLength();
        //     var list = new List<T>(length);
        //     for (var i = 0; i < length; i++)
        //     {
        //         list.Add((T) func(null, ser));
        //     }
        //     return list;
        // }
    }
}