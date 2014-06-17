namespace EventStore.Demo.CommandHandlers
{
    using Common;

    using EventStore.Demo.Commands;

    public sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand>
    {
        private readonly IRepository repository;

        public CreateBookingCommandHandler(IRepository repository)
        {
            this.repository = repository;
        }

        public void Execute(CreateBookingCommand command)
        {
            Booking booking = Booking.CreateNew();
            repository.Save(booking);
        }
    }
}
