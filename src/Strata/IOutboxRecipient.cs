namespace Strata;

public interface IOutboxRecipient<TEvent>
{
    Task Handle(int version, TEvent @event);
}