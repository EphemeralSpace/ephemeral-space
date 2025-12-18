using Content.Shared._ES.Mind.Components;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Shared._ES.Mind;

public abstract class ESSharedMindVisualsSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMindVisualsComponent, PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<ESMindVisualsComponent, PlayerDetachedEvent>(OnDetached);

        _player.PlayerStatusChanged += OnStatusChanged;
    }

    private void OnAttached(Entity<ESMindVisualsComponent> ent, ref PlayerAttachedEvent args)
    {
        ent.Comp.AssociatedUser = args.Player.UserId;
        Appearance.SetData(ent, ESMindVisuals.HasMind, true);
    }

    private void OnDetached(Entity<ESMindVisualsComponent> ent, ref PlayerDetachedEvent args)
    {
        Appearance.SetData(ent, ESMindVisuals.HasMind, false);
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        var newInGame = args.NewStatus == SessionStatus.InGame;

        var query = EntityQueryEnumerator<ESMindVisualsComponent, AppearanceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var appearance))
        {
            if (comp.AssociatedUser != args.Session.UserId)
                continue;
            Appearance.SetData(uid, ESMindVisuals.InGame, newInGame, appearance);
        }
    }
}
