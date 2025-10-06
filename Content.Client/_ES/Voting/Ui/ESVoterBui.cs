using System.Numerics;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Voting.Ui;

[UsedImplicitly]
public sealed class ESVoterBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESVotingWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESVotingWindow>();
        _window.OpenCenteredAt(new Vector2(0.25f, 0.25f));
        _window.Update(Owner);
    }

    public override void Update()
    {
        base.Update();

        _window?.Update(Owner);
    }
}
