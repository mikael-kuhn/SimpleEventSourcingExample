namespace Common
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public abstract class AggregateBase : IAggregate, IEquatable<IAggregate>
    {
        private readonly ICollection<object> uncommittedEvents = new LinkedList<object>();
        private IRouteEvents registeredRoutes;

        protected AggregateBase()
            : this(null)
        {
        }

        protected AggregateBase(IRouteEvents handler)
        {
            if (handler == null) return;

            RegisteredRoutes = handler;
            RegisteredRoutes.Register(this);
        }

        public Guid Id { get; protected set; }

        public int Version { get; protected set; }

        protected IRouteEvents RegisteredRoutes
        {
            get
            {
                return registeredRoutes ?? (registeredRoutes = new ConventionEventRouter(true, this));
            }
            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException("AggregateBase must have an event router to function");
                }

                registeredRoutes = value;
            }
        }

        protected void Register<T>(Action<T> route)
        {
            RegisteredRoutes.Register(route);
        }

        protected void RaiseEvent(object @event)
        {
            ((IAggregate)this).ApplyEvent(@event);
            uncommittedEvents.Add(@event);
        }

        void IAggregate.ApplyEvent(object @event)
        {
            RegisteredRoutes.Dispatch(@event);
            Version++;
        }
        ICollection IAggregate.GetUncommittedEvents()
        {
            return (ICollection)uncommittedEvents;
        }
        void IAggregate.ClearUncommittedEvents()
        {
            uncommittedEvents.Clear();
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            return Equals(obj as IAggregate);
        }
        public virtual bool Equals(IAggregate other)
        {
            return null != other && other.Id == Id;
        }
    }
}
