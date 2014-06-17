namespace Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using EventStore.ClientAPI;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Repository implementation for geteventstore
    /// </summary>
    public sealed class GetEventStoreRepository : IRepository
    {
        #region Private

        private const string EventClrTypeHeader = "EventClrTypeName";
        private const string AggregateClrTypeHeader = "AggregateClrTypeName";
        private const string CommitIdHeader = "CommitId";
        private const int WritePageSize = 500;
        private const int ReadPageSize = 500;
        private readonly Func<Type, Guid, string> aggregateIdToStreamName;
        private readonly IEventStoreConnection eventStoreConnection;
        private static readonly JsonSerializerSettings SerializerSettings;

        private static EventData ToEventData(Guid eventId, object evnt, IDictionary<string, object> headers)
        {
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evnt, SerializerSettings));

            var eventHeaders = new Dictionary<string, object>(headers)
            {
                {
                    EventClrTypeHeader, evnt.GetType().AssemblyQualifiedName
                }
            };
            var metadata = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(eventHeaders, SerializerSettings));
            var typeName = evnt.GetType().Name;

            return new EventData(eventId, typeName, true, data, metadata);
        }

        private static TAggregate ConstructAggregate<TAggregate>()
        {
            return (TAggregate)Activator.CreateInstance(typeof(TAggregate), true);
        }

        private static object DeserializeEvent(byte[] metadata, byte[] data)
        {
            var eventClrTypeName = JObject.Parse(Encoding.UTF8.GetString(metadata)).Property(EventClrTypeHeader).Value;
            return JsonConvert.DeserializeObject(Encoding.UTF8.GetString(data), Type.GetType((string)eventClrTypeName));
        }

        #endregion
        #region Constructors

        /// <summary>
        /// Constructor
        /// </summary>
        static GetEventStoreRepository()
        {
            SerializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GetEventStoreRepository(IEventStoreConnection eventStoreConnection)
            : this(
                eventStoreConnection,
                (t, g) => string.Format("{0}-{1}", char.ToLower(t.Name[0]) + t.Name.Substring(1), g.ToString("N")))
        {
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public GetEventStoreRepository(IEventStoreConnection eventStoreConnection, Func<Type, Guid, string> aggregateIdToStreamName)
        {
            this.eventStoreConnection = eventStoreConnection;
            this.aggregateIdToStreamName = aggregateIdToStreamName;
        }

        #endregion

        /// <summary>
        /// Get aggregate
        /// </summary>
        public TAggregate GetById<TAggregate>(Guid id) where TAggregate : class, IAggregate
        {
            return GetById<TAggregate>(id, int.MaxValue);
        }

        /// <summary>
        /// Get aggregate
        /// </summary>
        public TAggregate GetById<TAggregate>(Guid id, int version) where TAggregate : class, IAggregate
        {
            if (version <= 0)
            {
                throw new InvalidOperationException("Cannot get version <= 0");
            }

            var streamName = aggregateIdToStreamName(typeof(TAggregate), id);
            var aggregate = ConstructAggregate<TAggregate>();

            var sliceStart = 0;
            StreamEventsSlice currentSlice;
            do
            {
                var sliceCount = sliceStart + ReadPageSize <= version
                                    ? ReadPageSize
                                    : version - sliceStart + 1;

                currentSlice = eventStoreConnection.ReadStreamEventsForward(streamName, sliceStart, sliceCount, false);

                if (currentSlice.Status == SliceReadStatus.StreamNotFound)
                    throw new AggregateNotFoundException(id, typeof(TAggregate));

                if (currentSlice.Status == SliceReadStatus.StreamDeleted)
                    throw new AggregateDeletedException(id, typeof(TAggregate));

                sliceStart = currentSlice.NextEventNumber;

                foreach (var evnt in currentSlice.Events)
                {
                    aggregate.ApplyEvent(DeserializeEvent(evnt.OriginalEvent.Metadata, evnt.OriginalEvent.Data));
                }

            } while (version >= currentSlice.NextEventNumber && !currentSlice.IsEndOfStream);

            if (aggregate.Version != version && version < Int32.MaxValue)
            {
                throw new AggregateVersionException(id, typeof(TAggregate), aggregate.Version, version);
            }

            return aggregate;
        }

        /// <summary>
        /// Save aggregate
        /// </summary>
        public void Save(IAggregate aggregate)
        {
            Save(aggregate, Guid.NewGuid(), headers => { });
        }

        /// <summary>
        /// Save aggregate
        /// </summary>
        public void Save(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
        {
            var commitHeaders = new Dictionary<string, object>
            {
                {CommitIdHeader, commitId},
                {AggregateClrTypeHeader, aggregate.GetType().AssemblyQualifiedName}
            };
            updateHeaders(commitHeaders);

            var streamName = aggregateIdToStreamName(aggregate.GetType(), aggregate.Id);
            var newEvents = aggregate.GetUncommittedEvents().Cast<object>().ToList();
            var originalVersion = aggregate.Version - newEvents.Count;
            var expectedVersion = originalVersion == 0 ? ExpectedVersion.NoStream : originalVersion;
            var eventsToSave = newEvents.Select(e => ToEventData(Guid.NewGuid(), e, commitHeaders)).ToList();

            if (eventsToSave.Count < WritePageSize)
            {
                eventStoreConnection.AppendToStream(streamName, expectedVersion, eventsToSave);
            }
            else
            {
                var transaction = eventStoreConnection.StartTransaction(streamName, expectedVersion);

                var position = 0;
                while (position < eventsToSave.Count)
                {
                    var pageEvents = eventsToSave.Skip(position).Take(WritePageSize);
                    transaction.Write(pageEvents);
                    position += WritePageSize;
                }

                transaction.Commit();
            }

            aggregate.ClearUncommittedEvents();
        }

    }
}
