namespace Common
{
    using System;
    using System.Collections;

    public interface IAggregate
    {
        /// <summary>
        /// Unique Id of the aggregate
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Version of the aggregate
        /// </summary>
        int Version { get; }

        /// <summary>
        /// Apply an event
        /// </summary>
        /// <param name="event"></param>
        void ApplyEvent(object @event);

        /// <summary>
        /// Get uncomitted events
        /// </summary>
        /// <returns></returns>
        ICollection GetUncommittedEvents();

        /// <summary>
        /// Clear uncomitted events
        /// </summary>
        void ClearUncommittedEvents();
    }
}
