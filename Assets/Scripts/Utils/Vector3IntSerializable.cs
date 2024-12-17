using System;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct Vector3IntSerializable : INetworkSerializable {
    public int x, y, z;

    public Vector3IntSerializable(Vector3Int vector) {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public Vector3Int ToVector3Int() => new(x, y, z);

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref x);
        serializer.SerializeValue(ref y);
        serializer.SerializeValue(ref z);
    }
}