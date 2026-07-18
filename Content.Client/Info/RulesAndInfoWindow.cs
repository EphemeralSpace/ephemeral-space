using System.Numerics;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client.Info;

public sealed class RulesAndInfoWindow : DefaultWindow
{
    public RulesAndInfoWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("ui-info-tab-rules");

        var rulesList = new RulesControl
        {
            Margin = new Thickness(10)
        };

        ContentsContainer.AddChild(rulesList);

        SetSize = new Vector2(675, 750);
    }
}
