using Content.Shared._Sunrise.ExtendedColor;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.UserInterface.Controls;

public sealed class ExtendedColorSelectorSliders : Control
{
    public ExtendedColor Color
    {
        get => _currentColor;
        set => _currentColor = value;
    }

    private ExtendedColor _currentColor;

    private ExtendedColor? _lastChangedColor;

    private readonly Dictionary<string, ColorSelectorSliders> _colorSelectors = new();

    private readonly OptionButton _typeSelector;
    private readonly List<ColorType> _types = new();

    private ColorType _currentType = ColorType.Color;

    private readonly BoxContainer _bodyBox;

    public Action<ExtendedColor>? OnColorChanged;

    public ColorType CurrentType
    {
        get => _currentType;
        set
        {
            if (_currentType == value)
                return;

            _currentType = value;
            UpdateType();
        }
    }

    public ExtendedColorSelectorSliders(ExtendedColor? defaultColor = null)
    {
        defaultColor ??= ExtendedColor.White;

        _typeSelector = new OptionButton();
        foreach (var type in Enum.GetValues<ColorType>())
        {
            // TODO: локализация
            _typeSelector.AddItem(type.ToString());
            _types.Add(type);
        }

        _typeSelector.OnItemSelected += args =>
        {
            CurrentType = _types[args.Id];
            _typeSelector.Select(args.Id);
        };

        var rootBox = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        AddChild(rootBox);

        var headerBox = new BoxContainer();
        rootBox.AddChild(headerBox);
        headerBox.AddChild(_typeSelector);

        _bodyBox = new BoxContainer
            { Orientation = BoxContainer.LayoutOrientation.Vertical };
        rootBox.AddChild(_bodyBox);

        _currentColor = defaultColor;
        _currentType = defaultColor.Type;
        _typeSelector.TrySelect(_types.IndexOf(_currentType));
        _typeSelector.OnItemSelected += _ => OnColorsChanged();
        UpdateType();
    }

    public void UpdateType()
    {
        PopulateSelectors();
    }

    private ColorSelectorSliders CreateSelector(string key = "base")
    {
        var colorSelector = new ColorSelectorSliders();

        if (Color.Colors.TryGetValue(key, out var defaultColor))
            colorSelector.Color = defaultColor;

        colorSelector.OnColorChanged += _ => OnColorsChanged();

        _colorSelectors.Add(key, colorSelector);
        _bodyBox.AddChild(colorSelector);

        return colorSelector;
    }

    private void OnColorsChanged()
    {
        foreach (var (key, selector) in _colorSelectors)
        {
            Color.Colors[key] = selector.Color;
        }

        Color.Type = CurrentType;

        if (_lastChangedColor?.Equals(Color) == true)
            return;


        _lastChangedColor = new ExtendedColor(Color.Type, new Dictionary<string, Color>(Color.Colors));
        OnColorChanged?.Invoke(Color);
    }

    private void PopulateSelectors()
    {
        _colorSelectors.Clear();
        _bodyBox.DisposeAllChildren();
        switch (CurrentType)
        {
            case ColorType.Color:
                CreateSelector();
                break;
            case ColorType.Gradient:
                CreateSelector();
                CreateSelector("gradient");
                break;
        }
    }
}

