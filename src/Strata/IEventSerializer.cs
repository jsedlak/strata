namespace Strata;

public interface IEventSerializer
{
    BinaryData Serialize<TEvent>(TEvent data);
    
    TEvent Deserialize<TEvent>(byte[] data);
}