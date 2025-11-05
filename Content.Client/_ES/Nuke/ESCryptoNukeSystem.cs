using Content.Shared._ES.Nuke;
using Content.Shared._ES.Nuke.Components;

namespace Content.Client._ES.Nuke;

/// <inheritdoc/>
public sealed class ESCryptoNukeSystem : ESSharedCryptoNukeSystem
{
    protected override void UpdateUiState(Entity<ESCryptoNukeConsoleComponent, UserInterfaceComponent> ent)
    {
        if (UserInterface.TryGetOpenUi((ent, ent.Comp2), ESCryptoNukeConsoleUiKey.Key, out var bui))
            bui.Update();
    }
}
