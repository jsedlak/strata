using Orleans.Storage;

namespace Strata;

public class OrleansStateSerializer : IStateSerializer
{
    private readonly IGrainStorageSerializer _storageSerializer;

    public OrleansStateSerializer(IGrainStorageSerializer storageSerializer)
    {
        _storageSerializer = storageSerializer;
    }
    
    public BinaryData Serialize<TState>(TState data)
    {
        return _storageSerializer.Serialize(data);
    }

    public TState Deserialize<TState>(byte[] data)
    {
        return _storageSerializer.Deserialize<TState>(data);
    }
}