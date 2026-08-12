using Content.Shared._ES.Mapping;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._ES.Mapping;

[UsedImplicitly]
public sealed partial class ESMappingUIController : UIController, IOnSystemChanged<ESSelectionSystem>
{
    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IInputManager _inputMan = default!;

    [UISystemDependency] private InputSystem _inputSys = default!;

    private ESMappingControls? Controls => UIManager.GetActiveUIWidgetOrNull<ESMappingControls>();

    public void OnSystemLoaded(ESSelectionSystem system)
    {
        system.OnMapperUpdate += Update;
        system.OnMapperDetach += Detach;
    }

    public void OnSystemUnloaded(ESSelectionSystem system)
    {
        system.OnMapperUpdate -= Update;
        system.OnMapperDetach -= Detach;
    }

    private void Update(Entity<ESMapperComponent> obj)
    {
        Controls?.SetDisabled(false);
        Controls?.SetState(obj.Comp.SelectionState);

        if (obj.Comp.SelectionState is not ESSelectionState.Disabled)
        {
            _inputMan.Contexts.SetActiveContext("editor");
        }
        else
        {
            _inputSys.SetEntityContextActive();
        }
    }

    private void Detach()
    {
        Controls?.SetDisabled(true);
    }

    public void OnMoveModePressed()
    {
        _entities.RaisePredictiveEvent(new ESMappingDisableSelectionMessage());
    }

    public void OnSelectModePressed()
    {
        _entities.RaisePredictiveEvent(new ESMappingEnableSelectionMessage());
    }
}
