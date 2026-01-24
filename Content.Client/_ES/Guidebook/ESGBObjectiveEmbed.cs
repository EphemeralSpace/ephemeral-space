using System.Diagnostics.CodeAnalysis;
using Content.Client._ES.Objectives.Ui;
using Content.Client.Guidebook;
using Content.Client.Guidebook.Richtext;
using Content.Shared.Tag;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Guidebook;

[UsedImplicitly]
public sealed class ESGBObjectiveEmbed : Control, IDocumentTag
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IEntitySystemManager _sysMan = default!;
    [Dependency] private readonly ILogManager _log = default!;

    private readonly TagSystem _tagSys;
    private readonly ISawmill _sawmill;

    private readonly ESObjectiveControl _control = new();

    private EntProtoId? _objective;
    private EntityUid? _managedEnt;

    public ESGBObjectiveEmbed()
    {
        IoCManager.InjectDependencies(this);

        _sawmill = _log.GetSawmill("guidebook.objective");
        _tagSys = _sysMan.GetEntitySystem<TagSystem>();

        AddChild(_control);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        _managedEnt = _entMan.SpawnEntity(_objective, MapCoordinates.Nullspace);

        _tagSys.AddTag(_managedEnt.Value, GuidebookSystem.GuideEmbedTag);
        _control.SetObjective(_managedEnt.Value);
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        if (!_entMan.Deleted(_managedEnt))
            _entMan.DeleteEntity(_managedEnt);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        if (!args.TryGetValue("Objective", out var proto))
        {
            _sawmill.Error("Entity embed tag is missing entity prototype argument");
            control = null;
            return false;
        }

        _objective = proto;

        control = this;
        return true;
    }
}
