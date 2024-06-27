using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.TapePlayer;
using Robust.Client.GameObjects;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.TapePlayer
{
    public sealed class TapePlayerSystem : SharedTapePlayerSystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly SharedUserInterfaceSystem _uiSystem = default!;
        [Dependency] private readonly IConfigurationManager _cfg = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<TapePlayerComponent, AppearanceChangeEvent>(OnAppearanceChange);
            SubscribeLocalEvent<TapePlayerComponent, AnimationCompletedEvent>(OnAnimationCompleted);
            SubscribeLocalEvent<TapePlayerComponent, AfterAutoHandleStateEvent>(OnTapePlayerAfterState);
            _cfg.OnValueChanged(SunriseCCVars.TapePlayerClientEnabled, OnTapePlayerClientOptionChanged, true);
        }

        public override void Shutdown()
        {
            base.Shutdown();
            _cfg.UnsubValueChanged(SunriseCCVars.TapePlayerClientEnabled, OnTapePlayerClientOptionChanged);
        }

        private void OnTapePlayerClientOptionChanged(bool option)
        {
            RaiseNetworkEvent(new ClientOptionTapePlayerEvent(option));
        }

        private void OnTapePlayerAfterState(Entity<TapePlayerComponent> ent, ref AfterAutoHandleStateEvent args)
        {
            if (!_uiSystem.TryGetOpenUi<TapePlayerBoundUserInterface>(ent.Owner, TapePlayerUiKey.Key, out var bui))
                return;

            bui.Reload();
        }

        private void OnAnimationCompleted(EntityUid uid, TapePlayerComponent component, AnimationCompletedEvent args)
        {
            if (!TryComp<SpriteComponent>(uid, out var sprite))
                return;

            if (!TryComp<AppearanceComponent>(uid, out var appearance) ||
                !_appearanceSystem.TryGetData<TapePlayerVisualState>(uid, TapePlayerVisuals.VisualState, out var visualState, appearance))
            {
                visualState = TapePlayerVisualState.On;
            }

            UpdateAppearance(uid, visualState, component, sprite);
        }

        private void OnAppearanceChange(EntityUid uid, TapePlayerComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (!args.AppearanceData.TryGetValue(TapePlayerVisuals.VisualState, out var visualStateObject) ||
                visualStateObject is not TapePlayerVisualState visualState)
            {
                visualState = TapePlayerVisualState.On;
            }

            UpdateAppearance(uid, visualState, component, args.Sprite);
        }

        private void UpdateAppearance(EntityUid uid, TapePlayerVisualState visualState, TapePlayerComponent component, SpriteComponent sprite)
        {
            SetLayerState(TapePlayerVisualLayers.Base, component.OffState, sprite);

            switch (visualState)
            {
                case TapePlayerVisualState.On:
                    SetLayerState(TapePlayerVisualLayers.Base, component.OnState, sprite);
                    break;

                case TapePlayerVisualState.Off:
                    SetLayerState(TapePlayerVisualLayers.Base, component.OffState, sprite);
                    break;
            }
        }

        private void SetLayerState(TapePlayerVisualLayers layer, string? state, SpriteComponent sprite)
        {
            if (string.IsNullOrEmpty(state))
                return;

            sprite.LayerSetVisible(layer, true);
            sprite.LayerSetAutoAnimated(layer, true);
            sprite.LayerSetState(layer, state);
        }
    }
}
