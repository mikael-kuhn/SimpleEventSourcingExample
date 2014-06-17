namespace Common
{
    using System;

    /// <summary>
    /// Event registry
    /// </summary>
    public interface IRouteEvents
    {
        void Register<T>(Action<T> handler);
        void Register(IAggregate aggregate);

        void Dispatch(object eventMessage);
    }
}
