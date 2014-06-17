namespace EventStore.Demo
{
    using System;
    using System.Net;

    using Common;

    using EventStore.ClientAPI;
    using EventStore.Demo.CommandHandlers;
    using EventStore.Demo.Commands;

    class Program
    {
        private static readonly IPEndPoint IntegrationTestTcpEndPoint = new IPEndPoint(IPAddress.Loopback, 1113);

        static void Main(string[] args)
        {
            // Run from an administrator command prompt EventStore.SingleNode --db=../db --logs=../logs

            using (IEventStoreConnection connection = EventStoreConnection.Create(IntegrationTestTcpEndPoint))
            {
                connection.Connect();
                GetEventStoreRepository repository = new GetEventStoreRepository(connection);

                CreateBookingCommandHandler handler = new CreateBookingCommandHandler(repository);
                handler.Execute(new CreateBookingCommand());
            }
            Console.ReadLine();

            // Check the result at http://localhost:2113
        }
    }
}
