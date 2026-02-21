using Content.Shared._ES.PainFlash;
using Content.Shared._ES.PainFlash.Components;
using Content.Shared.Damage.Systems;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._ES.PainFlash;

/// <inheritdoc/>
public sealed class ESPainFlashSystem : ESSharedPainFlashSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private ESPainFlashOverlay _overlay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPainFlashComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ESPainFlashComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<ESPainFlashComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESPainFlashComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeNetworkEvent<ESPainFlashMessage>(OnPainFlashMessage);

        _overlay = new();
    }

    private void OnPlayerAttached(Entity<ESPainFlashComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        _overlayManager.AddOverlay(_overlay);
        _overlay.ResetPainAccumulator();
    }

    private void OnPlayerDetached(Entity<ESPainFlashComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.ResetPainAccumulator();
    }

    private void OnInit(Entity<ESPainFlashComponent> ent, ref ComponentInit args)
    {
        if (_player.LocalEntity != ent)
            return;
        _overlayManager.AddOverlay(_overlay);
        _overlay.ResetPainAccumulator();
    }

    private void OnShutdown(Entity<ESPainFlashComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent)
            return;
        _overlayManager.RemoveOverlay(_overlay);
        _overlay.ResetPainAccumulator();
    }

    private void OnPainFlashMessage(ESPainFlashMessage ev)
    {
        _overlay.AddPain(ev.Damage);
    }

    protected override void OnDamageChanged(Entity<ESPainFlashComponent> ent, ref DamageChangedEvent args)
    {
        if (_player.LocalEntity != ent)
            return;

        if (_timing.ApplyingState || !_timing.IsFirstTimePredicted)
            return;

        if (!IsPainFlashTrigger(args, out var damage))
            return;

        _overlay.AddPain(damage);
    }
}
