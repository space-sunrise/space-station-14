using Content.Client.Nuke;
using Content.Shared._Sunrise.EncodedAirlock;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Nuke;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Serialization;

namespace Content.Client._Sunrise.EncodedAirlock
{
    [UsedImplicitly]
    public sealed class CodeConsoleBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private CodeConsoleMenu? _menu;

        public CodeConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _menu = this.CreateWindow<CodeConsoleMenu>();

            _menu.OnKeypadButtonPressed += i =>
            {
                SendMessage(new CodeConsoleKeypadMessage(i));
            };
            _menu.OnEnterButtonPressed += () =>
            {
                SendMessage(new CodeConsoleKeypadEnterMessage());
            };
            _menu.OnClearButtonPressed += () =>
            {
                SendMessage(new CodeConsoleKeypadClearMessage());
            };
            _menu.ArmButton.OnPressed += _ =>
            {
                SendMessage(new NukeArmedMessage());
            };
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_menu == null)
                return;

            switch (state)
            {
                case CodeConsoleUiState msg:
                    _menu.UpdateState(msg);
                    break;
            }
        }
    }
}
