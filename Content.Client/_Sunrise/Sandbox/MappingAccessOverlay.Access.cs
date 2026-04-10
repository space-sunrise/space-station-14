using System.Text;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

public sealed partial class MappingAccessOverlay
{
    private readonly List<string> _groupAccessNames = new(8);
    private readonly StringBuilder _accessBuffer = new();

    private void BuildAccessLines(AccessReaderComponent reader, string orText)
    {
        _accessLines.Clear();

        foreach (var accessGroup in reader.AccessLists)
        {
            if (accessGroup.Count == 0)
                continue;

            _groupAccessNames.Clear();
            foreach (var access in accessGroup)
            {
                _groupAccessNames.Add(GetAccessName(access));
            }

            _groupAccessNames.Sort(CompareAccessText);
            _accessBuffer.Clear();

            for (var i = 0; i < _groupAccessNames.Count; i++)
            {
                if (i > 0)
                    _accessBuffer.Append(" + ");

                _accessBuffer.Append(_groupAccessNames[i]);
            }

            _accessLines.Add(_accessBuffer.ToString());
        }

        _accessLines.Sort(CompareAccessText);

        if (_accessLines.Count <= 1)
            return;

        _accessBuffer.Clear();
        for (var i = 0; i < _accessLines.Count; i++)
        {
            if (i > 0)
                _accessBuffer.Append(' ').Append(orText).Append(' ');

            _accessBuffer.Append(_accessLines[i]);
        }

        _accessLines.Clear();
        _accessLines.Add(_accessBuffer.ToString());
    }

    private string GetAccessName(ProtoId<AccessLevelPrototype> access)
    {
        if (_prototypeManager.Resolve(access, out var accessPrototype) &&
            !string.IsNullOrWhiteSpace(accessPrototype.Name))
        {
            return _loc.GetString(accessPrototype.Name);
        }

        return access.Id;
    }

    private static int CompareAccessText(string? left, string? right)
    {
        return string.Compare(left, right, StringComparison.CurrentCultureIgnoreCase);
    }
}
