using Content.Shared.Clothing.Dirt;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;

namespace Content.Client.Clothing.Dirt.UI;

// расширяет стандартный слот инвентаря - рисует полоску и процент грязи
public sealed class ClothingDirtInventorySlotControl : SlotControl
{
    private readonly PanelContainer _bar;
    private readonly PanelContainer _fill;
    private readonly Label _pct;
    private readonly PanelContainer _dot;

    private float _cachedLevel = -1f;
    private Color _cachedColor;

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IResourceCache _res = default!;

    public ClothingDirtInventorySlotControl() : base()
    {
        IoCManager.InjectDependencies(this);

        // тёмная подложка полоски
        _bar = new PanelContainer
        {
            VerticalAlignment = VAlignment.Bottom,
            HorizontalAlignment = HAlignment.Stretch,
            MinHeight = 4,
            Visible = false,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _bar.PanelOverride = new StyleBoxFlat { BackgroundColor = new Color(0f, 0f, 0f, 0.55f) };

        _fill = new PanelContainer
        {
            VerticalAlignment = VAlignment.Stretch,
            HorizontalAlignment = HAlignment.Left,
            MouseFilter = MouseFilterMode.Ignore,
        };
        _bar.AddChild(_fill);

        // процент в правом нижнем углу
        _pct = new Label
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Bottom,
            FontColorOverride = Color.White,
            ShadowColor = Color.Black,
            DrawShadow = true,
            Margin = new Thickness(0, 0, 2, 6),
            FontOverride = _res.GetFont("/Fonts/NotoSans/NotoSans-Bold.ttf", 7),
            Visible = false,
            MouseFilter = MouseFilterMode.Ignore,
        };

        // цветная точка сверху справа
        _dot = new PanelContainer
        {
            HorizontalAlignment = HAlignment.Right,
            VerticalAlignment = VAlignment.Top,
            MinWidth = 6,
            MinHeight = 6,
            Margin = new Thickness(0, 2, 2, 0),
            Visible = false,
            MouseFilter = MouseFilterMode.Ignore,
        };

        AddChild(_bar);
        AddChild(_pct);
        AddChild(_dot);

        TooltipDelay = 0.4f;
        OnShowTooltip += (_, tip) =>
        {
            if (HeldEntity.HasValue &&
                _ent.TryGetComponent<ClothingDirtComponent>(HeldEntity.Value, out var d) &&
                tip is ClothingDirtTooltipControl ctrl)
                ctrl.Fill(d);
        };
    }

    protected override Control MakeTooltip() => new ClothingDirtTooltipControl();

    public void Refresh()
    {
        var item = HeldEntity;

        if (item == null ||
            !_ent.TryGetComponent<ClothingDirtComponent>(item.Value, out var dirt) ||
            dirt.DirtLevel <= 0f)
        {
            Hide();
            return;
        }

        if (Math.Abs(dirt.DirtLevel - _cachedLevel) < 0.5f && dirt.DirtColor == _cachedColor)
            return;

        _cachedLevel = dirt.DirtLevel;
        _cachedColor = dirt.DirtColor;

        // полоска
        _bar.Visible = true;
        _fill.MinWidth = (int)(Width * dirt.DirtLevel / 100f);
        _fill.PanelOverride = new StyleBoxFlat { BackgroundColor = dirt.DirtColor.WithAlpha(0.85f) };

        // точка
        _dot.Visible = true;
        _dot.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = dirt.DirtColor,
            BorderColor = new Color(0f, 0f, 0f, 0.6f),
            BorderThickness = new Thickness(1),
        };

        // текст
        _pct.Visible = true;
        (_pct.Text, _pct.FontColorOverride) = dirt.DirtLevel switch
        {
            > 66f => ("100%", new Color(0.9f, 0.15f, 0.15f)),
            > 33f => ("66%",  new Color(0.95f, 0.6f, 0.1f)),
            _     => ("33%",  new Color(0.85f, 0.85f, 0.2f)),
        };
    }

    private void Hide()
    {
        _bar.Visible = false;
        _pct.Visible = false;
        _dot.Visible = false;
        _cachedLevel = -1f;
    }

    protected override void Resized()
    {
        base.Resized();
        if (_cachedLevel > 0f)
            _fill.MinWidth = (int)(Width * _cachedLevel / 100f);
    }
}
