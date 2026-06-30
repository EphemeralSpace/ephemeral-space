using Content.Server._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Fixtures;

public sealed partial class TestPlayer
{
    /// <summary>
    ///     Sets the player's mask on the server.
    /// </summary>
    public void SSetMask(ProtoId<ESSecretIdentityPrototype> mask)
    {
        AssertServer();

        var maskSys = _test.Server.System<ESSecretIdentitySystem>();

        maskSys.ApplyMask(SMindEntity, mask);
    }

    /// <summary>
    ///     Gets the player's mask on the server.
    /// </summary>
    /// <returns></returns>
    public ProtoId<ESSecretIdentityPrototype>? SGetMask()
    {
        AssertServer();

        var maskSys = _test.Server.System<ESSecretIdentitySystem>();

        return maskSys.GetMaskOrNull(SMindEntity);
    }
}
