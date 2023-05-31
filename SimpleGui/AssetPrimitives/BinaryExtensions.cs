using System;
using System.IO;
using System.Runtime.InteropServices;

namespace AssetPrimitives
{
    internal static class BinaryExtensions
    {
        
        
        public static T ReadEnum<T>(this BinaryReader reader) where T : struct
        {
            return (T)Enum.ToObject(typeof(T), reader.ReadInt32());
        }

        public static void WriteEnum<T>(this BinaryWriter writer, T value)
        {
            int i32 = Convert.ToInt32(value);
            writer.Write(i32);
        }

        public static byte[] ReadByteArray(this BinaryReader reader)
        {
            int byteCount = reader.ReadInt32();
            return reader.ReadBytes(byteCount);
        }

        public static void WriteByteArray(this BinaryWriter writer, byte[] array)
        {
            writer.Write(array.Length);
            writer.Write(array);
        }

        public static void WriteObjectArray<T>(this BinaryWriter writer, T[] array, Action<BinaryWriter, T> writeFunc)
        {
            writer.Write(array.Length);
            foreach (T item in array)
            {
                writeFunc(writer, item);
            }
        }

        public static T[] ReadObjectArray<T>(this BinaryReader reader, Func<BinaryReader, T> readFunc)
        {
            int length = reader.ReadInt32();
            T[] ret = new T[length];
            for (int i = 0; i < length; i++)
            {
                ret[i] = readFunc(reader);
            }

            return ret;
        }

        public static void WriteBlittableArray<T>(this BinaryWriter writer, T[] array) where T : unmanaged
        {

            Span<byte> spanArray = MemoryMarshal.Cast<T, byte>(array.AsSpan());

            writer.Write(array.Length);
            foreach (byte t in spanArray)
            {
                writer.Write(t);
            }
        }

        public static T[] ReadBlittableArray<T>(this BinaryReader reader) where T : unmanaged
        {
            int length = reader.ReadInt32();
            T[] ret = new T[length];
            Span<byte> spanArray = MemoryMarshal.Cast<T, byte>(ret.AsSpan());
            for (int i = 0; i < spanArray.Length; i++)
            {
                spanArray[i] = reader.ReadByte();
            }
            return ret;
        }
    }
}
