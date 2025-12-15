using Content.Shared.Actions;

namespace Content.Shared._ES.Masks.ChemicalInjector;

public sealed partial class ESChemicalInjectorEvent : InstantActionEvent
{
    /// <summary>
    /// Name of the solution to draw the injection chems from (what will be injected)
    /// </summary>
    [DataField]
    public string SolutionName = "Injector";

    /// <summary>
    /// Failure popup string
    /// </summary>
    [DataField]
    public LocId FailMessage = "es-chemical-injector-not-crit";

    /// <summary>
    /// If true, fail when the user is not crit.
    /// </summary>
    [DataField]
    public bool OnlyUsableWhileCrit = true;
}
