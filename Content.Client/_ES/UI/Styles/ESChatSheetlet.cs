using Content.Client.Stylesheets;
using Content.Client.Stylesheets.Fonts;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client._ES.UI.Styles;

[CommonSheetlet]
public sealed class ESChatSheetlet : Sheetlet<PalettedStylesheet>
{
    public override StyleRule[] GetRules(PalettedStylesheet sheet, object config)
    {
        var small = sheet.Fonts.GetFont(StandardFontType.Chat, 10);
        var medium = sheet.Fonts.GetFont(StandardFontType.Chat, 12);
        var large = sheet.Fonts.GetFont(StandardFontType.Chat, 14);

        return
        [
            E()
                .Class(StyleClass.FontChat)
                .Font(medium)
                .Prop(Label.StylePropertyFontOutlineThickness, 2f)
                .Prop(Label.StylePropertyFontOutlineColor, Color.Black),


            E<PanelContainer>()
                .Class("speechBox")
                .Margin(new Thickness(2f)),
            E<PanelContainer>()
                .Class("speechBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleHeader"))
                .Font(small),

            E<PanelContainer>()
                .Class("speechBox", "whisperBox")
                .ParentOf(E<RichTextLabel>().Class("bubbleContent"))
                .Font(small),
        ];
    }
}
