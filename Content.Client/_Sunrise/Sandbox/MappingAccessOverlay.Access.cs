using System.Text;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Sandbox;

public sealed partial class MappingAccessOverlay
{
    /*
     * Access-reader lookup and access text formatting helpers.
     */
    private readonly List<string> _groupAccessNames = new(8);
    private readonly StringBuilder _accessBuffer = new();
    private readonly Dictionary<EntityUid, AccessReaderComponent> _accessReaderLookup = new();
    private readonly Dictionary<string, AccessReaderComponent?> _prototypeAccessReaderLookup = new();
    private bool _accessReaderLookupDirty = true;

    public void MarkAccessReaderLookupDirty()
    {
        _accessReaderLookupDirty = true;
    }

    public void SyncAccessReaderLookup(EntityUid uid, AccessReaderComponent accessReader)
    {
        if (!accessReader.Enabled || accessReader.AccessLists.Count == 0)
        {
            _accessReaderLookup.Remove(uid);
            return;
        }

        _accessReaderLookup[uid] = accessReader;
    }

    public void RemoveAccessReaderLookup(EntityUid uid)
    {
        _accessReaderLookup.Remove(uid);
    }

    private bool TryGetDisplayedAccessReader(
        EntityUid uid,
        AccessReaderComponent accessReader,
        out AccessReaderComponent displayedReader)
    {
        if (ElectronicsOnly)
            return TryGetElectronicsAccessReader(uid, accessReader, out displayedReader);

        displayedReader = accessReader;
        return accessReader.ContainerAccessProvider == null;
    }

    private bool TryGetElectronicsAccessReader(
        EntityUid uid,
        AccessReaderComponent accessReader,
        out AccessReaderComponent electronicsReader)
    {
        electronicsReader = default!;

        if (accessReader.ContainerAccessProvider is not { } containerId)
            return false;

        if (TryGetContainedAccessReader(uid, containerId, out electronicsReader))
            return true;

        return TryGetPrototypeElectronicsAccessReader(uid, containerId, out electronicsReader);
    }

    private bool TryGetContainedAccessReader(
        EntityUid uid,
        string containerId,
        out AccessReaderComponent electronicsReader)
    {
        electronicsReader = default!;

        if (!_containerSystem.TryGetContainer(uid, containerId, out var container))
        {
            return false;
        }

        var foundReader = false;
        var selectedUid = EntityUid.Invalid;

        foreach (var containedUid in container.ContainedEntities)
        {
            if (!_accessReaderLookup.TryGetValue(containedUid, out var containedReader))
                continue;

            if (foundReader && containedUid.Id >= selectedUid.Id)
                continue;

            foundReader = true;
            selectedUid = containedUid;
            electronicsReader = containedReader;
        }

        return foundReader;
    }

    private bool TryGetPrototypeElectronicsAccessReader(
        EntityUid uid,
        string containerId,
        out AccessReaderComponent electronicsReader)
    {
        electronicsReader = default!;

        if (!_containerFillQuery.TryComp(uid, out var containerFill) ||
            !containerFill.Containers.TryGetValue(containerId, out var prototypes))
        {
            return false;
        }

        foreach (var prototypeId in prototypes)
        {
            if (!TryGetPrototypeAccessReader(prototypeId, out electronicsReader))
                continue;

            return true;
        }

        return false;
    }

    private bool TryGetPrototypeAccessReader(string prototypeId, out AccessReaderComponent accessReader)
    {
        accessReader = default!;

        if (_prototypeAccessReaderLookup.TryGetValue(prototypeId, out var cachedReader))
        {
            if (cachedReader == null)
                return false;

            accessReader = cachedReader;
            return true;
        }

        if (!_prototypeManager.TryIndex<EntityPrototype>(prototypeId, out var prototype) ||
            !prototype.TryGetComponent<AccessReaderComponent>(out var prototypeReader, _ent.ComponentFactory) ||
            !prototypeReader.Enabled ||
            prototypeReader.AccessLists.Count == 0)
        {
            _prototypeAccessReaderLookup[prototypeId] = null;
            return false;
        }

        _prototypeAccessReaderLookup[prototypeId] = prototypeReader;
        accessReader = prototypeReader;
        return true;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _prototypeAccessReaderLookup.Clear();
    }

    private void RebuildAccessReaderLookup()
    {
        _accessReaderLookup.Clear();

        var query = _ent.AllEntityQueryEnumerator<AccessReaderComponent>();
        while (query.MoveNext(out var uid, out var accessReader))
        {
            if (!accessReader.Enabled || accessReader.AccessLists.Count == 0)
                continue;

            _accessReaderLookup[uid] = accessReader;
        }

        _accessReaderLookupDirty = false;
    }

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
        return string.Compare(left, right, StringComparison.InvariantCultureIgnoreCase);
    }
}
