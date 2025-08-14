using Content.Shared.CCVar;
using Content.Shared._Sunrise.Drugs;
using Content.Shared.StatusEffectNew;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Random;

namespace Content.Client._Sunrise.Drugs;

public sealed class RainbowSmoothOverlaySystem : EntitySystem
{
	[Dependency] private readonly IPlayerManager _player = default!;
	[Dependency] private readonly IOverlayManager _overlayMan = default!;
	[Dependency] private readonly IRobustRandom _random = default!;

	private RainbowSmoothOverlay _overlay = default!;

	public override void Initialize()
	{
		base.Initialize();
		SubscribeLocalEvent<SeeingRainbowsWeakStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
		SubscribeLocalEvent<SeeingRainbowsWeakStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
		SubscribeLocalEvent<SeeingRainbowsWeakStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerAttachedEvent>>(OnPlayerAttached);
		SubscribeLocalEvent<SeeingRainbowsWeakStatusEffectComponent, StatusEffectRelayedEvent<LocalPlayerDetachedEvent>>(OnPlayerDetached);
		_overlay = new();
	}

	private void OnRemoved(Entity<SeeingRainbowsWeakStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
	{
		if (_player.LocalEntity != args.Target)
			return;
		_overlay.Intoxication = 0;
		_overlay.TimeTicker = 0;
		_overlayMan.RemoveOverlay(_overlay);
	}

	private void OnApplied(Entity<SeeingRainbowsWeakStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
	{
		if (_player.LocalEntity != args.Target)
			return;
		_overlay.Phase = _random.NextFloat(MathF.Tau);
		_overlayMan.AddOverlay(_overlay);
	}

	private void OnPlayerAttached(Entity<SeeingRainbowsWeakStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerAttachedEvent> args)
	{
		_overlayMan.AddOverlay(_overlay);
	}

	private void OnPlayerDetached(Entity<SeeingRainbowsWeakStatusEffectComponent> ent, ref StatusEffectRelayedEvent<LocalPlayerDetachedEvent> args)
	{
		_overlay.Intoxication = 0;
		_overlay.TimeTicker = 0;
		_overlayMan.RemoveOverlay(_overlay);
	}
}

public sealed class RainbowSmoothOverlay : Overlay
{
	private static readonly ProtoId<ShaderPrototype> Shader = "Rainbow";

	[Dependency] private readonly IConfigurationManager _config = default!;
	[Dependency] private readonly IEntityManager _entityManager = default!;
	[Dependency] private readonly IPrototypeManager _prototypeManager = default!;
	[Dependency] private readonly IPlayerManager _playerManager = default!;
	[Dependency] private readonly IEntitySystemManager _sysMan = default!;
	[Dependency] private readonly IGameTiming _timing = default!;
	private readonly StatusEffectsSystem _statusEffects = default!;

	public override OverlaySpace Space => OverlaySpace.WorldSpace;
	public override bool RequestScreenTexture => true;
	private readonly ShaderInstance _rainbowShader;

	public float Intoxication = 0.0f;
	public float TimeTicker = 0.0f;
	public float Phase = 0.0f;

	private float _timeScale = 0.0f;
	private float _warpScale = 0.0f;

	private float EffectScale => Intoxication;

	public RainbowSmoothOverlay()
	{
		IoCManager.InjectDependencies(this);
		_statusEffects = _sysMan.GetEntitySystem<StatusEffectsSystem>();
		_rainbowShader = _prototypeManager.Index(Shader).InstanceUnique();
		_config.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, invokeImmediately: true);
	}

	private void OnReducedMotionChanged(bool reducedMotion)
	{
		_timeScale = reducedMotion ? 0.0f : 1.0f;
		_warpScale = reducedMotion ? 0.0f : 1.0f;
	}

	protected override void FrameUpdate(FrameEventArgs args)
	{
		var playerEntity = _playerManager.LocalEntity;
		if (playerEntity == null)
			return;

		if (!_statusEffects.TryGetEffectsEndTimeWithComp<SeeingRainbowsWeakStatusEffectComponent>(playerEntity, out var endTime))
			return;

		endTime ??= TimeSpan.MaxValue;
		var timeLeft = (float)(endTime - _timing.CurTime).Value.TotalSeconds;

		TimeTicker += args.DeltaSeconds;

		const float fadeSeconds = 10.0f;
		var fadeIn = Math.Clamp(TimeTicker / fadeSeconds, 0.0f, 1.0f);
		var fadeOut = Math.Clamp(timeLeft / fadeSeconds, 0.0f, 1.0f);
		var envelope = MathF.Min(fadeIn, fadeOut);

		const float baseStrength = 0.2f;
		Intoxication = baseStrength * envelope;
	}

	protected override bool BeforeDraw(in OverlayDrawArgs args)
	{
		if (!_entityManager.TryGetComponent(_playerManager.LocalEntity, out EyeComponent? eyeComp))
			return false;
		if (args.Viewport.Eye != eyeComp.Eye)
			return false;
		return EffectScale > 0;
	}

	protected override void Draw(in OverlayDrawArgs args)
	{
		if (ScreenTexture == null)
			return;
		var handle = args.WorldHandle;
		_rainbowShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
		_rainbowShader.SetParameter("colorScale", EffectScale);
		_rainbowShader.SetParameter("timeScale", _timeScale);
		_rainbowShader.SetParameter("warpScale", _warpScale * EffectScale);
		_rainbowShader.SetParameter("phase", Phase);
		handle.UseShader(_rainbowShader);
		handle.DrawRect(args.WorldBounds, Color.White);
		handle.UseShader(null);
	}
}
