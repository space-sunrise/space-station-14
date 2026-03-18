using System.Linq;
using Content.Client.Options.UI;
using Robust.Shared.Configuration;

namespace Content.Client._Sunrise.Options.UI;

/// <summary>
/// Sunrise-specific dropdown option that supports replacing the available values at runtime.
/// </summary>
public sealed class SunriseOptionDropDownCVar<T> : BaseOptionCVar<T> where T : notnull
{
    private readonly IConfigurationManager _cfg;
    private readonly CVarDef<T> _cVar;
    private readonly OptionDropDown _dropDown;
    private ItemEntry[] _entries;

    protected override T Value
    {
        get => (T) _dropDown.Button.SelectedMetadata!;
        set => _dropDown.Button.SelectId(FindValueId(value));
    }

    public SunriseOptionDropDownCVar(
        OptionsTabControlRow controller,
        IConfigurationManager cfg,
        CVarDef<T> cVar,
        OptionDropDown dropDown,
        IReadOnlyCollection<ValueOption> options) : base(controller, cfg, cVar)
    {
        _cfg = cfg;
        _cVar = cVar;
        _dropDown = dropDown;
        _entries = [];

        ReplaceOptions(options);

        dropDown.Button.OnItemSelected += args =>
        {
            dropDown.Button.SelectId(args.Id);
            ValueChanged();
        };
    }

    public override void LoadValue()
    {
        Value = ResolveAvailableValue(_cfg.GetCVar(_cVar));
    }

    public override void ResetToDefault()
    {
        Value = ResolveAvailableValue(_cVar.DefaultValue);
    }

    public override bool IsModified()
    {
        return !IsValueEqual(Value, ResolveAvailableValue(_cfg.GetCVar(_cVar)));
    }

    public override bool IsModifiedFromDefault()
    {
        return !IsValueEqual(Value, ResolveAvailableValue(_cVar.DefaultValue));
    }

    public void ReplaceOptions(IReadOnlyCollection<ValueOption> options)
    {
        if (options.Count == 0)
            throw new ArgumentException("Need at least one option!");

        var previousValue = TryGetSelectedValue(out var selectedValue)
            ? selectedValue
            : options.First().Key;

        _dropDown.Button.Clear();
        _entries = new ItemEntry[options.Count];

        var i = 0;
        foreach (var option in options)
        {
            _entries[i] = new ItemEntry
            {
                Key = option.Key,
            };

            _dropDown.Button.AddItem(option.Label, i);
            _dropDown.Button.SetItemMetadata(_dropDown.Button.GetIdx(i), option.Key);
            i += 1;
        }

        Value = previousValue;
    }

    private bool TryGetSelectedValue(out T value)
    {
        value = default!;

        if (_dropDown.Button.ItemCount == 0)
            return false;

        for (var i = 0; i < _dropDown.Button.ItemCount; i++)
        {
            if (_dropDown.Button.GetItemId(i) != _dropDown.Button.SelectedId)
                continue;

            if (_dropDown.Button.GetItemMetadata(i) is not T metadata)
                return false;

            value = metadata;
            return true;
        }

        return false;
    }

    private T ResolveAvailableValue(T value)
    {
        return TryFindValueId(value, out var id)
            ? _entries[id].Key
            : _entries[0].Key;
    }

    private int FindValueId(T value)
    {
        if (TryFindValueId(value, out var id))
            return id;

        return 0;
    }

    private bool TryFindValueId(T value, out int id)
    {
        for (var i = 0; i < _entries.Length; i++)
        {
            if (IsValueEqual(_entries[i].Key, value))
            {
                id = i;
                return true;
            }
        }

        id = 0;
        return false;
    }

    public sealed class ValueOption(T key, string label)
    {
        public readonly T Key = key;
        public readonly string Label = label;
    }

    private struct ItemEntry
    {
        public T Key;
    }
}
