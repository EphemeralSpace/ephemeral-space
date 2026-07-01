using Content.Server._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Fixtures;

public sealed partial class TestPlayer
{
    /// <summary>
    ///     Sets the player's secret identity on the server.
    /// </summary>
    public void SSetSecretIdentity(ProtoId<ESSecretIdentityPrototype> secretIdentity)
    {
        AssertServer();

        var secretIdentitySys = _test.Server.System<ESSecretIdentitySystem>();

        secretIdentitySys.ApplySecretIdentity(SMindEntity, secretIdentity);
    }

    /// <summary>
    ///     Gets the player's secret identity on the server.
    /// </summary>
    /// <returns></returns>
    public ProtoId<ESSecretIdentityPrototype>? SGetSecretIdentity()
    {
        AssertServer();

        var secretIdentitySys = _test.Server.System<ESSecretIdentitySystem>();

        return secretIdentitySys.GetSecretIdentityOrNull(SMindEntity);
    }
}
