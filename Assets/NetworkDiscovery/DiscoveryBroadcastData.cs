using Unity.Netcode;

public struct DiscoveryBroadcastData : INetworkSerializable
{
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter { }
}
