using Content.Shared.Actions;

namespace Content.Shared._ES.Detective.Components;

[RegisterComponent]
[Access(typeof(ESHunchSystem))]
public sealed partial class ESHunchComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, string> BodyClue = new ();
}

public sealed partial class ESHunchActionEvent : EntityTargetActionEvent;
