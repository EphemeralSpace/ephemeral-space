using Content.Server._ES.Announcements;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared._ES.Chat.Radio;
using Content.Shared._ES.Degradation;
using Content.Shared.Gravity;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Gravity;

public sealed partial class GravityGeneratorSystem : SharedGravityGeneratorSystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private ESAnnouncementSystem _announcement = default!;
    [Dependency] private GravitySystem _gravitySystem = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;
    [Dependency] private PowerChargeSystem _powerCharge = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ESUndergoDegradationEvent>(OnUndergoDegradation);
    }

    private void OnUndergoDegradation(Entity<GravityGeneratorComponent> ent, ref ESUndergoDegradationEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.GravityActive)
        {
            var msg = Loc.GetString("es-grav-gen-sabotage-announcement");
            var distortedMsg = ESRadioSystem.DistortRadioMessage(msg, 0.15f, _prototype, _random, Loc);
            _announcement.DispatchRoundAnnouncement(
                distortedMsg,
                sender: Loc.GetString("es-station-event-announcer"),
                colorOverride: Color.FromHex("#00ff96"),
                important: true);
        }

        _powerCharge.SetCharge(ent.Owner, 0f);
        _powerCharge.SetActive(ent.Owner, false);

        args.Handled = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GravityGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
        }
    }

    private void OnActivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.GravityActive = true;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        }
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.GravityActive = false;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
        }
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
        }
    }
}
