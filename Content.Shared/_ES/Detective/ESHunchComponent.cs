using Content.Shared._ES.Auditions;
using Content.Shared.Actions;

namespace Content.Shared._ES.Detective;

[RegisterComponent]
public sealed partial class ESHunchComponent : Component
{
    public Dictionary<EntityUid, string> BodyClue = new ();
}

public sealed partial class ESHunchActionEvent : EntityTargetActionEvent;
