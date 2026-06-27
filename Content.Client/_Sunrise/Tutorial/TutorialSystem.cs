using Content.Client.UserInterface.Screens;
using Content.Client._Sunrise.Tutorial.Components;
using Content.Client._Sunrise.Tutorial.Overlays;
using Content.Shared._Sunrise.Tutorial.Components;
using Content.Shared._Sunrise.Tutorial.EntitySystems;
using Content.Shared._Sunrise.Tutorial.Events;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Client._Sunrise.Tutorial.TutorialBubbleControl;

namespace Content.Client._Sunrise.Tutorial;

/// <summary>
/// Client-side tutorial controller for bubbles, target highlighting, path overlay, and tutorial menu requests.
/// </summary>
public sealed class TutorialSystem : SharedTutorialSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private static readonly ProtoId<ShaderPrototype> TutorialShader = "TutorialTargetOutline";

    private ShaderInstance? _shaderInstance;
    private EntityUid? _highlightedTarget;
    private uint? _highlightedTargetRenderOrder;
    private EntityQuery<SpriteComponent> _spriteQuery;

    /// <summary>
    /// Raised after the server sends the list of completed tutorial sequences.
    /// </summary>
    public event Action? WindowDataReceived;

    /// <summary>
    /// Whether completion data has been received from the server during this client session.
    /// </summary>
    public bool CompletedTutorialsReceived { get; private set; }

    /// <summary>
    /// Prototype IDs of tutorial sequences completed by the local user.
    /// </summary>
    public readonly HashSet<string> CompletedTutorials = [];
    private EntityQuery<TutorialBubbleUiComponent> _bubbleUiQuery;
    private LayoutContainer? _tutorialBubbleRoot;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TutorialBubbleComponent, AfterAutoHandleStateEvent>(AfterAutoHandleState);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TutorialBubbleComponent, ComponentShutdown>(OnComponentShutdown);
        SubscribeLocalEvent<TutorialPlayerComponent, AfterAutoHandleStateEvent>(OnTutorialPlayerState);
        SubscribeLocalEvent<TutorialBubbleComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<TutorialBubbleComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
        SubscribeNetworkEvent<TutorialWindowDataResponseEvent>(OnWindowDataResponse);
        SubscribeNetworkEvent<TutorialStartDeniedEvent>(OnStartDenied);

        _tutorialBubbleRoot = new LayoutContainer();
        _bubbleUiQuery = GetEntityQuery<TutorialBubbleUiComponent>();
        _spriteQuery = GetEntityQuery<SpriteComponent>();
        _ui.OnScreenChanged += OnScreenChanged;

        _shaderInstance = _proto.Index(TutorialShader).InstanceUnique();
        _overlayManager.AddOverlay(new TutorialPathOverlay(EntityManager, _player, _timing, _transform, _proto));
    }

    private void OnScreenChanged((UIScreen? Old, UIScreen? New) ev)
    {
        if (ev.New is not InGameScreen)
        {
            SetHighlight(null);
            _tutorialBubbleRoot?.Orphan();
            return;
        }

        RefreshBubbles();
    }

    private void OnStartDenied(TutorialStartDeniedEvent msg, EntitySessionEventArgs args)
    {
        if (string.IsNullOrEmpty(msg.Reason))
            return;

        _ui.Popup(msg.Reason, null, false);
    }

    private void OnPlayerAttached(Entity<TutorialBubbleComponent> ent, ref LocalPlayerAttachedEvent ev)
    {
        RefreshBubbles();
    }

    private void OnPlayerDetached(Entity<TutorialBubbleComponent> ent, ref LocalPlayerDetachedEvent ev)
    {
        SetHighlight(null);
        _tutorialBubbleRoot?.Orphan();
    }

    private void OnTutorialPlayerState(Entity<TutorialPlayerComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        // Clear highlight if target changed or was removed.
        // The new highlight is applied in OnComponentInit/AfterAutoHandleState of TutorialBubbleComponent,
        // so path and highlight always appear together with the bubble.
        if (ent.Comp.CurrentBubbleTarget != _highlightedTarget)
            SetHighlight(null);

        RefreshBubbles();
    }

    private void SetHighlight(EntityUid? target)
    {
        if (_highlightedTarget == target)
            return;

        // Remove from old target
        if (_highlightedTarget is { } old && _spriteQuery.TryGetComponent(old, out var oldSprite))
        {
            if (oldSprite.PostShader == _shaderInstance)
            {
                oldSprite.PostShader = null;
                if (_highlightedTargetRenderOrder is { } renderOrder)
                    oldSprite.RenderOrder = renderOrder;
            }
        }

        _highlightedTarget = null;
        _highlightedTargetRenderOrder = null;

        // Apply to new target — never highlight the local player
        if (target is not { } newTarget || newTarget == _player.LocalEntity)
            return;

        if (!_spriteQuery.TryGetComponent(newTarget, out var sprite))
            return;

        // Don't overwrite an existing unrelated post-shader
        if (sprite.PostShader != null && sprite.PostShader != _shaderInstance)
            return;

        _highlightedTarget = target;
        _highlightedTargetRenderOrder = sprite.RenderOrder;
        sprite.PostShader = _shaderInstance;
        sprite.RenderOrder = EntityManager.CurrentTick.Value;
    }

    private void AfterAutoHandleState(Entity<TutorialBubbleComponent> ent, ref AfterAutoHandleStateEvent ev)
    {
        SetHighlight(ent);
        UpdateBubble(ent);
    }

    private void OnComponentInit(Entity<TutorialBubbleComponent> ent, ref ComponentInit ev)
    {
        SetHighlight(ent);
        UpdateBubble(ent);
    }

    private void OnComponentShutdown(Entity<TutorialBubbleComponent> ent, ref ComponentShutdown ev)
    {
        if (_highlightedTarget == ent.Owner)
            SetHighlight(null);
        RemoveBubble(ent);
    }

    private void RefreshBubbles()
    {
        var query = EntityQueryEnumerator<TutorialBubbleComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            UpdateBubble((uid, comp));
        }
    }

    private void RemoveBubble(EntityUid uid)
    {
        if (!_bubbleUiQuery.TryGetComponent(uid, out var bubbleUi) || bubbleUi.Bubble == null)
            return;

        var bubble = bubbleUi.Bubble;
        bubbleUi.Bubble = null;
        RemComp<TutorialBubbleUiComponent>(uid);

        bubble.DisposeAllChildren();
        bubble.Orphan();
    }

    private void UpdateBubble(Entity<TutorialBubbleComponent> ent)
    {
        if (string.IsNullOrEmpty(ent.Comp.Instruction))
        {
            RemoveBubble(ent.Owner);
            return;
        }

        if (_ui.ActiveScreen is not InGameScreen)
            return;

        var viewportContainer = _ui.ActiveScreen.FindControl<LayoutContainer>("ViewportContainer");

        if (_bubbleUiQuery.TryGetComponent(ent.Owner, out var uiComp) && uiComp.Bubble != null)
        {
            if (uiComp.LastInstruction == ent.Comp.Instruction)
            {
                // Text unchanged — just ensure parenting is correct.
                SetSpeechBubbleRoot(viewportContainer, uiComp.Bubble);
                return;
            }

            // Instruction changed — discard existing bubble and fall through to create a fresh one.
            // This gives consistent behaviour regardless of whether the network delivered the
            // change as a component update or as a remove + re-add.
            RemoveBubble(ent.Owner);
        }

        var bubble = TutorialBubble.Create(
            Loc.GetString(ent.Comp.Instruction),
            ent);

        SetSpeechBubbleRoot(viewportContainer, bubble);

        var bubbleUi = EnsureComp<TutorialBubbleUiComponent>(ent.Owner);
        bubbleUi.Bubble = bubble;
        bubbleUi.LastInstruction = ent.Comp.Instruction;
    }

    private void SetSpeechBubbleRoot(LayoutContainer root, TutorialBubble bubble)
    {
        if (_tutorialBubbleRoot == null)
            return;

        if (bubble.Parent != _tutorialBubbleRoot)
            _tutorialBubbleRoot.AddChild(bubble);

        // Only re-parent when necessary — orphaning every update causes a blank render tick.
        if (_tutorialBubbleRoot.Parent != root)
        {
            _tutorialBubbleRoot.Orphan();
            root.AddChild(_tutorialBubbleRoot);
            LayoutContainer.SetAnchorPreset(_tutorialBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        }

        _tutorialBubbleRoot.SetPositionLast();
    }

    /// <summary>
    /// Requests that the server ends the local player's active tutorial session.
    /// </summary>
    public void RequestQuitTutorial()
    {
        RaiseNetworkEvent(new TutorialQuitRequestEvent());
    }

    /// <summary>
    /// Requests the server-side list of tutorials completed by the local user.
    /// </summary>
    public void RequestWindowData()
    {
        RaiseNetworkEvent(new TutorialWindowDataRequestEvent());
    }

    /// <summary>
    /// Requests that the server starts the selected tutorial sequence for the local user.
    /// </summary>
    public void RequestStartTutorial(ProtoId<TutorialSequencePrototype> sequenceId)
    {
        RaiseNetworkEvent(new TutorialStartRequestEvent(sequenceId));
    }

    private void OnWindowDataResponse(TutorialWindowDataResponseEvent msg, EntitySessionEventArgs args)
    {
        CompletedTutorials.Clear();
        CompletedTutorials.UnionWith(msg.CompletedTutorials);
        CompletedTutorialsReceived = true;
        WindowDataReceived?.Invoke();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        SetHighlight(null);
        _ui.OnScreenChanged -= OnScreenChanged;
        _overlayManager.RemoveOverlay<TutorialPathOverlay>();

    }
}
