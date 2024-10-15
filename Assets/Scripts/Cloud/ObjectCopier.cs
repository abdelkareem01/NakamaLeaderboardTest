using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

public static class ObjectCopier
{
    public static T Clone<T>(T source)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", nameof(source));
        }

        //Don't serialize a null object, simply return the default for that object
        if (ReferenceEquals(source, null)) return default;

        using var stream = new MemoryStream();
        IFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, source);
        stream.Seek(0, SeekOrigin.Begin);
        return (T)formatter.Deserialize(stream);
    }

    public static bool IsDifferent<T>(T original, T cloned)
    {
        byte[] originalBytes = SerializeToByteArray(original);
        byte[] clonedBytes = SerializeToByteArray(cloned);

        if (originalBytes.Length != clonedBytes.Length)
        {
            return true;
        }

        for (int i = 0; i < originalBytes.Length; i++)
        {
            if (originalBytes[i] != clonedBytes[i])
            {
                return true;
            }
        }

        return false;
    }

    private static byte[] SerializeToByteArray<T>(T obj)
    {
        using var stream = new MemoryStream();
        IFormatter formatter = new BinaryFormatter();
        formatter.Serialize(stream, obj);
        return stream.ToArray();
    }
}