using Content.Shared.Actions;

namespace Content.Server._ES.Masks.Chemicalnjection;

public sealed partial class ESChemicalInjectorEvent : InstantActionEvent
{
    [DataField]
    public string SolutionName = "Injector";

    [DataField]
    public string NotCrit = "chemical-injector-not-crit";

    [DataField]
    public bool OnlyUsableWhileCrit = true;
}
