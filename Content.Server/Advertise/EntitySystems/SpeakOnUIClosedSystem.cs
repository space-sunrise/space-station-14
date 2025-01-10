using Content.Server.Advertise.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Dataset;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using ActivatableUIComponent = Content.Shared.UserInterface.ActivatableUIComponent;

namespace Content.Server.Advertise
{
    public sealed partial class SpeakOnUIClosedSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly ChatSystem _chat = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SpeakOnUIClosedComponent, BoundUIClosedEvent>(OnBoundUIClosed);
        }

        private void OnBoundUIClosed(EntityUid uid, SpeakOnUIClosedComponent component, BoundUIClosedEvent args)
        {
            if (!TryComp<ActivatableUIComponent>(uid, out var activatable) || !args.UiKey.Equals(activatable.Key))
                return;

            if (component.RequireFlag && !component.Flag)
                return;

            TrySpeak(uid, component);
        }

        public bool TrySpeak(EntityUid uid, SpeakOnUIClosedComponent? component)
        {
            if (component == null || !component.Enabled)
                return false;

            if (!_prototypeManager.TryIndex(component.Pack, out LocalizedDatasetPrototype? messagePack))
            {
                Logger.Warning($"Failed to find localized dataset prototype with ID: {component.Pack}");
                return false;
            }

            var messageKey = _random.Pick(messagePack.Values);
            var message = Loc.GetString(messageKey, ("name", Name(uid)));

            //Logger.Info($"Localized message: {message}, messageKey: {messageKey}");
            _chat.TrySendInGameICMessage(uid, message, InGameICChatType.Speak, true);
            component.Flag = false;
            return true;
        }

        public bool TrySetFlag(EntityUid uid, SpeakOnUIClosedComponent? component, bool value = true)
        {
            if (component == null)
                return false;
            
            component.Flag = value;
            return true;
        }
    }
}