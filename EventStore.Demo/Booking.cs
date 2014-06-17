
namespace EventStore.Demo
{
    using System;

    using Common;

    using EventStore.Demo.Events;

    public sealed class Booking : AggregateBase
    {
        public Booking()
        {
            RaiseEvent(new BookingCreatedEvent(Guid.NewGuid()));
        }

        public static Booking CreateNew()
        {
            return new Booking();
        }

        public void Apply(BookingCreatedEvent @event)
        {
            Id = @event.Id;
        }
    }
}
