using Content.Client._ES.Screens;
using Content.Client.UserInterface.Screens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets.Hud;

[CommonSheetlet]
public sealed class ChatGameScreenSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        return
        [
            E()
                .Class(PerformerGameScreen.StyleClassChatContainer)
                .Panel(new StyleBoxFlat(sheet.SecondaryPalette.Background)),
            E<OutputPanel>()
                .Class(PerformerGameScreen.StyleClassChatOutput)
                .Panel(new StyleBoxFlat(sheet.SecondaryPalette.BackgroundDark)),
        ];
    }
}
