
namespace EventStore.Demo.Events
{
    using System;

    public sealed class BookingCreatedEvent : IEvent
    {
        public BookingCreatedEvent(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; private set; }
    }
}
