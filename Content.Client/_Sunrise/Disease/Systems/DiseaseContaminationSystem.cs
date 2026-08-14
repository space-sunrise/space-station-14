using System.Linq;
using Content.Shared._Sunrise.Disease;
using Content.Shared._Sunrise.Disease.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using static Robust.Client.GameObjects.SpriteComponent;

namespace Content.Client._Sunrise.Disease.Systems;

public sealed class DiseaseContaminationSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> ContaminationShader = "DiseaseContamination";
    private const string ContaminationLayerKeyPrefix = "disease-contamination-";

    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    private readonly Dictionary<EntityUid, ContaminationVisuals> _visuals = new();

    private sealed class ContaminationVisuals(ShaderInstance shader)
    {
        public ShaderInstance Shader = shader;
        public List<string> LayerKeys { get; } = new();
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentHandleState>(OnContaminationState);
        SubscribeLocalEvent<DiseaseContaminationComponent, AppearanceChangeEvent>(OnContaminationAppearanceChange);

        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentStartup>(OnContaminationStartup);
        SubscribeLocalEvent<DiseaseContaminationComponent, ComponentShutdown>(OnContaminationShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentStartup>(OnDetectorUserStartup);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, ComponentShutdown>(OnDetectorUserShutdown);

        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<DiseaseInfectionDetectorUserComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnContaminationStartup(Entity<DiseaseContaminationComponent> ent, ref ComponentStartup args)
    {
        UpdateShader(ent.Owner, ent.Comp);
    }

    private void OnContaminationShutdown(Entity<DiseaseContaminationComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<SpriteComponent>(ent.Owner, out var sprite))
            return;

        ClearShader((ent.Owner, sprite));
    }

    private void OnPlayerAttached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateAllContaminationShaders();
    }

    private void OnPlayerDetached(Entity<DiseaseInfectionDetectorUserComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        ClearAllContaminationShaders();
    }

    private void OnDetectorUserStartup(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        UpdateAllContaminationShaders();
    }

    private void OnDetectorUserShutdown(Entity<DiseaseInfectionDetectorUserComponent> ent, ref ComponentShutdown args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        ClearAllContaminationShaders();
    }

    private void OnContaminationState(Entity<DiseaseContaminationComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not DiseaseContaminationComponentState state)
            return;

        ent.Comp.Contamination = state.Contamination;
        ent.Comp.Color = state.Color;

        UpdateShader(ent.Owner, ent.Comp);
    }

    private void OnContaminationAppearanceChange(Entity<DiseaseContaminationComponent> ent, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateShader(ent.Owner, ent.Comp, args.Sprite);
    }

    private void UpdateAllContaminationShaders()
    {
        var query = EntityQueryEnumerator<DiseaseContaminationComponent>();
        while (query.MoveNext(out var uid, out var contamination))
        {
            UpdateShader(uid, contamination);
        }
    }

    private void ClearAllContaminationShaders()
    {
        var query = EntityQueryEnumerator<DiseaseContaminationComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            ClearShader((uid, sprite));
        }
    }

    private void UpdateShader(EntityUid uid, DiseaseContaminationComponent contamination)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        UpdateShader(uid, contamination, sprite);
    }

    private void UpdateShader(EntityUid uid, DiseaseContaminationComponent contamination, SpriteComponent sprite)
    {
        var shouldShow = CanSeeContamination() && contamination.Contamination > 0f;
        if (!shouldShow)
        {
            ClearShader((uid, sprite));
            return;
        }

        if (!_visuals.TryGetValue(uid, out var visuals))
        {
            visuals = new ContaminationVisuals(_prototype.Index(ContaminationShader).InstanceUnique());
            _visuals[uid] = visuals;
        }

        visuals.Shader.SetParameter("contaminationAmount", Math.Clamp(contamination.Contamination, 0f, 1f));

        // Если цвет прозрачный/чёрный (напр. данные ещё не пришли с сервера),
        // используем fallback, чтобы блоки заражения не были невидимыми.
        var color = contamination.Color;
        if (color.R + color.G + color.B < 0.01f)
            color = Color.FromHex("#7FBF3F");

        visuals.Shader.SetParameter("contaminationColor", color);
        RebuildContaminationLayers((uid, sprite), visuals);
    }

    private void ClearShader(Entity<SpriteComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!_visuals.TryGetValue(ent.Owner, out var visuals))
            return;

        ClearContaminationLayers((ent.Owner, ent.Comp), visuals);
        _visuals.Remove(ent.Owner);
    }

    private void RebuildContaminationLayers(Entity<SpriteComponent> ent, ContaminationVisuals visuals)
    {
        ClearContaminationLayers(ent, visuals);

        var sourceLayers = ent.Comp.AllLayers.ToArray();
        for (var i = 0; i < sourceLayers.Length; i++)
        {
            if (sourceLayers[i] is not Layer layer || !_sprite.IsVisible(layer))
                continue;

            var layerKey = $"{ContaminationLayerKeyPrefix}{i}";
            var contaminationLayer = _sprite.LayerMapReserve(ent.AsNullable(), layerKey);

            visuals.LayerKeys.Add(layerKey);

            if (layer.Texture != null)
                _sprite.LayerSetTexture(ent.AsNullable(), contaminationLayer, layer.Texture);
            else
                _sprite.LayerSetRsi(ent.AsNullable(), contaminationLayer, layer.RSI, layer.State);

            _sprite.LayerSetScale(ent.AsNullable(), contaminationLayer, layer.Scale);
            _sprite.LayerSetRotation(ent.AsNullable(), contaminationLayer, layer.Rotation);
            _sprite.LayerSetOffset(ent.AsNullable(), contaminationLayer, layer.Offset);
            _sprite.LayerSetDirOffset(ent.AsNullable(), contaminationLayer, layer.DirOffset);
            _sprite.LayerSetVisible(ent.AsNullable(), contaminationLayer, layer.Visible);
            _sprite.LayerSetColor(ent.AsNullable(), contaminationLayer, layer.Color);
            _sprite.LayerSetAutoAnimated(ent.AsNullable(), contaminationLayer, layer.AutoAnimated);
            _sprite.LayerSetRenderingStrategy(ent.AsNullable(), contaminationLayer, layer.RenderingStrategy);

            if (_sprite.TryGetLayer(ent.AsNullable(), contaminationLayer, out var contaminationLayerData, false))
            {
                contaminationLayerData.Cycle = layer.Cycle;
                contaminationLayerData.Loop = layer.Loop;
                contaminationLayerData.Shader = visuals.Shader;
                contaminationLayerData.ShaderPrototype = ContaminationShader;
            }
        }
    }

    private void ClearContaminationLayers(Entity<SpriteComponent> ent, ContaminationVisuals visuals)
    {
        var indices = new List<int>(visuals.LayerKeys.Count);

        foreach (var layerKey in visuals.LayerKeys)
        {
            if (_sprite.LayerMapTryGet(ent.AsNullable(), layerKey, out var index, false))
                indices.Add(index);
        }

        indices.Sort();

        for (var i = indices.Count - 1; i >= 0; i--)
        {
            _sprite.RemoveLayer(ent.AsNullable(), indices[i], false);
        }

        visuals.LayerKeys.Clear();
    }

    private bool CanSeeContamination()
    {
        if (_player.LocalEntity is not { } localEntity)
            return false;

        if (!TryComp<EyeComponent>(localEntity, out var eye))
            return false;

        return (eye.VisibilityMask & BaseDiseaseSettings.DiseaseInfectionVisibilityFlag) != 0;
    }
}