using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Sunrise.Ghost
{
    public sealed class AcceptGhostWindow : DefaultWindow
    {
        public readonly Button DenyButton;
        public readonly Button AcceptButton;

        public AcceptGhostWindow()
        {

            Title = Loc.GetString("accept-ghost-window-title");

            Contents.AddChild(new BoxContainer
            {
                Orientation = LayoutOrientation.Vertical,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Children =
                        {
                            (new Label()
                            {
                                Text = Loc.GetString("accept-ghost-window-text")
                            }),
                            new BoxContainer
                            {
                                Orientation = LayoutOrientation.Horizontal,
                                Align = AlignMode.Center,
                                Children =
                                {
                                    (AcceptButton = new Button
                                    {
                                        Text = Loc.GetString("accept-ghost-window-accept-button"),
                                    }),

                                    (new Control()
                                    {
                                        MinSize = new Vector2(20, 0)
                                    }),

                                    (DenyButton = new Button
                                    {
                                        Text = Loc.GetString("accept-ghost-window-deny-button"),
                                    })
                                }
                            },
                        }
                    },
                }
            });
        }
    }
}
