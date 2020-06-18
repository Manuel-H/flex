using System;
using System.Collections;
using System.Collections.Generic;

namespace com.Dunkingmachine.FlexSerialization
{
    public static class FlexConvert
    {
        public static class Initializer
        {
            public static void Initialize(Dictionary<Type, Action<object, FlexSerializer>> serializeMethods, Dictionary<Type, Func<object, FlexSerializer, object>> deserializeMethods)
            {
                _serializeMethods = serializeMethods;
                _deserializeMethods = deserializeMethods;
            }
        }

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
        
        //TODO
        public static byte[] SerializeList<T>(List<T> list)
        {
            var ser = new FlexSerializer();
            ser.WriteArrayLength(list.Count);
            foreach (var x1 in list)
            {
                if (!_serializeMethods.TryGetValue(x1.GetType(), out var action))
                {
                    throw new FlexException("No serializer created for type "+x1.GetType());
                }
        
                action(x1, ser);
            }
            return ser.GetBytes();
        }
        
        public static byte[] SerializeArray<T>(T[] list)
        {
            var ser = new FlexSerializer();
            ser.WriteArrayLength(list.Length);
            foreach (var x1 in list)
            {
                if (!_serializeMethods.TryGetValue(x1.GetType(), out var action))
                {
                    throw new FlexException("No serializer created for type " + x1.GetType());
                }

                action(x1, ser);
            }

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

        //TODO
        public static List<T> DeserializeList<T>(byte[] bytes)
        {
            if (!_deserializeMethods.TryGetValue(typeof(T), out var func))
            {
                if(typeof(IList).IsAssignableFrom(typeof(T)))
                    throw new FlexException("Use DeserializeList for lists!");
                throw new FlexException("No serializer created for type "+nameof(T));
            }
            var ser = new FlexSerializer(bytes);
            var length = ser.ReadArrayLength();
            var list = new List<T>(length);
            for (var i = 0; i < length; i++)
            {
                list.Add((T) func(null, ser));
            }
            return list;
        }
        
        public static T[] DeserializeArray<T>(byte[] bytes)
        {
            if (!_deserializeMethods.TryGetValue(typeof(T), out var func))
            {
                if(typeof(IList).IsAssignableFrom(typeof(T)))
                    throw new FlexException("Use DeserializeList for lists!");
                throw new FlexException("No serializer created for type "+nameof(T));
            }
            var ser = new FlexSerializer(bytes);
            var list = new T[ser.ReadArrayLength()];
            for (var i = 0; i < list.Length; i++)
            {
                list[i] = (T) func(null, ser);
            }
            return list;
        }
    }
}