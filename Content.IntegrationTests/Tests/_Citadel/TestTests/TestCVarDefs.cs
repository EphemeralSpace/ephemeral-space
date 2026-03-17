using Robust.Shared.Configuration;

namespace Content.IntegrationTests.Tests._Citadel.TestTests;

[CVarDefs]
public sealed class TestCVarDefs
{
    public static readonly CVarDef<string> TestCVar =
    CVarDef.Create("testing.testing.testcvar", "foo", CVar.ARCHIVE | CVar.SERVER);
}
