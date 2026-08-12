using Robust.Shared.Serialization;

namespace Content.Shared._ES.Mapping;

public static class ESMappingMessageRegistrationExt
{
    public delegate void MappingEventSubscriber<TComp>(Subscriber<TComp> subscriber) where TComp : IComponent;

    extension(EntitySystem.Subscriptions subs)
    {
        public void MappingEvents<TComp>(MappingEventSubscriber<TComp> subscriber) where TComp : IComponent
        {
            subscriber(new Subscriber<TComp>(subs));
        }
    }

    public sealed class Subscriber<TComp> where TComp : IComponent
    {
        private readonly EntitySystem.Subscriptions _subs;
        private readonly EntityQuery<TComp> _query;

        internal Subscriber(EntitySystem.Subscriptions subs)
        {
            _subs = subs;
            _query = IoCManager.Resolve<IEntityManager>().GetEntityQuery<TComp>();
        }

        public void Event<TEvent>(EntityEventRefHandler<TComp, TEvent> handler)
            where TEvent : ESMappingMessage
        {
            _subs.SubSessionEvent<TEvent>(EventSource.All,
                (args, msg) =>
                {
                    if (msg.SenderSession.AttachedEntity is not { } entity)
                        return;

                    if (!_query.TryComp(entity, out var comp))
                        return;

                    handler((entity, comp), ref args);
                });
        }
    }
}

[Serializable, NetSerializable]
public abstract partial class ESMappingMessage : EntityEventArgs;
