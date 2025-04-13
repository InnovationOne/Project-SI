using Unity.Netcode;
using Unity.Collections;
using System;

public struct LobbyPlayerData : INetworkSerializable, IEquatable<LobbyPlayerData> {
    public ulong ClientId;
    public FixedString64Bytes PlayerName;
    public bool IsReady;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
    }

    public bool Equals(LobbyPlayerData other) {
        return ClientId == other.ClientId &&
               PlayerName.Equals(other.PlayerName) &&
               IsReady == other.IsReady;
    }
}
