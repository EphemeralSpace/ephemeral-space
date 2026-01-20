using Content.Shared.Actions;

namespace Content.Shared._ES.Changeling;

public sealed partial class ESChemicalStingEvent : EntityTargetActionEvent
{
    /// <summary>
    /// Name of the solution to draw the injection chems from (what will be injected)
    /// </summary>
    [DataField]
    public string SolutionName = "Injector";
}
