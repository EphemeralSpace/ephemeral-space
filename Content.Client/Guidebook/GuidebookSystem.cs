using System.Linq;
using Content.Client.Guidebook.Components;
using Content.Client.Light;
using Content.Client.Verbs;
using Content.Shared.Guidebook;
using Content.Shared.Interaction;
using Content.Shared.Light.Components;
using Content.Shared.Speech;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Guidebook;

/// <summary>
///     This system handles the help-verb and interactions with various client-side entities that are embedded into guidebooks.
/// </summary>
public sealed class GuidebookSystem : EntitySystem
{
    // ES START
    [Dependency] private readonly IPrototypeManager _proto = default!;
    // ES END
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly VerbSystem _verbSystem = default!;
    [Dependency] private readonly RgbLightControllerSystem _rgbLightControllerSystem = default!;
    [Dependency] private readonly SharedPointLightSystem _pointLightSystem = default!;
    [Dependency] private readonly TagSystem _tags = default!;

    public event Action<List<ProtoId<GuideEntryPrototype>>,
        List<ProtoId<GuideEntryPrototype>>?,
        ProtoId<GuideEntryPrototype>?,
        bool,
        ProtoId<GuideEntryPrototype>?>? OnGuidebookOpen;

    public const string GuideEmbedTag = "GuideEmbeded";

    private EntityUid _defaultUser;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GuideHelpComponent, GetVerbsEvent<ExamineVerb>>(OnGetVerbs);
        SubscribeLocalEvent<GuideHelpComponent, ActivateInWorldEvent>(OnInteract);

        SubscribeLocalEvent<GuidebookControlsTestComponent, InteractHandEvent>(OnGuidebookControlsTestInteractHand);
        SubscribeLocalEvent<GuidebookControlsTestComponent, ActivateInWorldEvent>(OnGuidebookControlsTestActivateInWorld);
        SubscribeLocalEvent<GuidebookControlsTestComponent, GetVerbsEvent<AlternativeVerb>>(
            OnGuidebookControlsTestGetAlternateVerbs);
    }

    // ES START
    /// <summary>
    ///     Takes a list of guide entries, and tries to translate their IDs to the ES equivalent with a prefix,
    ///     if one exists.
    /// </summary>
    private List<ProtoId<GuideEntryPrototype>> TryInterpretGuidesWithESPrefix(List<ProtoId<GuideEntryPrototype>> guides)
    {
        List<ProtoId<GuideEntryPrototype>> newGuides = new(guides.Count);

        foreach (var guide in guides)
        {
            // non-hidden prototypes shouldn't get translated
            // this includes any upstream prototypes that aren't hidden (of which there are none i think, but)
            // and also our own ES prototypes, which don't need to get translated
            if (_proto.Index(guide) is { Hidden: false })
            {
                newGuides.Add(guide);
                continue;
            }

            var translatedId = $"ES{guide.Id}";
            if (_proto.HasIndex<GuideEntryPrototype>(translatedId))
                newGuides.Add(translatedId);
            else
            {
                // if neither are true, then we have a guide that
                // isn't an ES guide, but also has no ES equivalent, so just add it anyway and let it be blank
                newGuides.Add(guide);
            }
        }

        return newGuides;
    }

    /// <summary>
    /// Gets a user entity to use for verbs and examinations. If the player has no attached entity, this will use a
    /// dummy client-side entity so that users can still use the guidebook when not attached to anything (e.g., in the
    /// lobby)
    /// </summary>
    public EntityUid GetGuidebookUser()
    {
        var user = _playerManager.LocalEntity;
        if (user != null)
            return user.Value;

        if (!Exists(_defaultUser))
            _defaultUser = Spawn(null, MapCoordinates.Nullspace);

        return _defaultUser;
    }

    private void OnGetVerbs(EntityUid uid, GuideHelpComponent component, GetVerbsEvent<ExamineVerb> args)
    {
        if (component.Guides.Count == 0 || _tags.HasTag(uid, GuideEmbedTag))
            return;

        // ES START
        var guides = TryInterpretGuidesWithESPrefix(component.Guides);
        // ES END

        args.Verbs.Add(new()
        {
            Text = Loc.GetString("guide-help-verb"),
            Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
            // ES START
            // component.Guides -> guides
            Act = () => OnGuidebookOpen?.Invoke(guides, null, null, component.IncludeChildren, guides[0]),
            // ES END
            ClientExclusive = true,
            CloseMenu = true
        });
    }

    public void OpenHelp(List<ProtoId<GuideEntryPrototype>> guides)
    {
        // ES START
        // translate guide entry IDs to ES guide entry IDs
        guides = TryInterpretGuidesWithESPrefix(guides);
        // ES END

        OnGuidebookOpen?.Invoke(guides, null, null, true, guides[0]);
    }

    private void OnInteract(EntityUid uid, GuideHelpComponent component, ActivateInWorldEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!component.OpenOnActivation || component.Guides.Count == 0 || _tags.HasTag(uid, GuideEmbedTag))
            return;

        // ES START
        var guides = TryInterpretGuidesWithESPrefix(component.Guides);

        // ES component.Guides -> guides
        OnGuidebookOpen?.Invoke(guides, null, null, component.IncludeChildren, guides[0]);
        // ES END
        args.Handled = true;
    }

    private void OnGuidebookControlsTestGetAlternateVerbs(EntityUid uid, GuidebookControlsTestComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                if (Transform(uid).LocalRotation != Angle.Zero)
                    Transform(uid).LocalRotation -= Angle.FromDegrees(90);
            },
            Text = Loc.GetString("guidebook-monkey-unspin"),
            Priority = -9999,
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Act = () =>
            {
                EnsureComp<PointLightComponent>(uid); // RGB demands this.
                _pointLightSystem.SetEnabled(uid, false);
                var rgb = EnsureComp<RgbLightControllerComponent>(uid);

                var sprite = EnsureComp<SpriteComponent>(uid);
                var layers = new List<int>();

                for (var i = 0; i < sprite.AllLayers.Count(); i++)
                {
                    layers.Add(i);
                }

                _rgbLightControllerSystem.SetLayers(uid, layers, rgb);
            },
            Text = Loc.GetString("guidebook-monkey-disco"),
            Priority = -9998,
        });
    }

    private void OnGuidebookControlsTestActivateInWorld(EntityUid uid, GuidebookControlsTestComponent component, ActivateInWorldEvent args)
    {
        Transform(uid).LocalRotation += Angle.FromDegrees(90);
    }

    private void OnGuidebookControlsTestInteractHand(EntityUid uid, GuidebookControlsTestComponent component, InteractHandEvent args)
    {
        if (!TryComp<SpeechComponent>(uid, out var speech) || speech.SpeechSounds is null)
            return;

        // This code is broken because SpeechSounds isn't a file name or sound specifier directly.
        // Commenting out to avoid compile failure with https://github.com/space-wizards/RobustToolbox/pull/5540
        // _audioSystem.PlayGlobal(speech.SpeechSounds, Filter.Local(), false, speech.AudioParams);
    }

    public void FakeClientActivateInWorld(EntityUid activated)
    {
        var activateMsg = new ActivateInWorldEvent(GetGuidebookUser(), activated, true);
        RaiseLocalEvent(activated, activateMsg);
    }

    public void FakeClientAltActivateInWorld(EntityUid activated)
    {
        // Get list of alt-interact verbs
        var verbs = _verbSystem.GetLocalVerbs(activated, GetGuidebookUser(), typeof(AlternativeVerb), force: true);

        if (!verbs.Any())
            return;

        _verbSystem.ExecuteVerb(verbs.First(), GetGuidebookUser(), activated);
    }

    public void FakeClientUse(EntityUid activated)
    {
        var activateMsg = new InteractHandEvent(GetGuidebookUser(), activated);
        RaiseLocalEvent(activated, activateMsg);
    }
}
