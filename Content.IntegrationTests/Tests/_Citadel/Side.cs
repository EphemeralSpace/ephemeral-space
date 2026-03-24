#nullable enable
namespace Content.IntegrationTests.Tests._Citadel;

[Flags]
public enum Side
{
    Client = 1,
    Server = 2,

    // A special value meant as a default for attributes, and NOTHING ELSE.
    Neither = 0,
    // A special value representing both sides.
    Both = Client | Server,
}
