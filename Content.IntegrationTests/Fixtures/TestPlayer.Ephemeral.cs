using Content.Server._ES.Masks;
using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Fixtures;

public sealed partial class TestPlayer
{
    /// <summary>
    ///     Sets the player's mask on the server.
    /// </summary>
    public void SSetMask(ProtoId<ESMaskPrototype> mask)
    {
        AssertServer();

        var maskSys = _test.Server.System<ESMaskSystem>();

        maskSys.ApplyMask(SMindEntity, mask);
    }

    /// <summary>
    ///     Gets the player's mask on the server.
    /// </summary>
    /// <returns></returns>
    public ProtoId<ESMaskPrototype>? SGetMask()
    {
        AssertServer();

        var maskSys = _test.Server.System<ESMaskSystem>();

        return maskSys.GetMaskOrNull(SMindEntity);
    }
}
