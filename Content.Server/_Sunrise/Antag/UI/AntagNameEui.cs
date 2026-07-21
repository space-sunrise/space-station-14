using Content.Server.Antag;
using Content.Server.EUI;
using Content.Shared._Sunrise.Antag.UI;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Eui;

namespace Content.Server._Sunrise.Antag.UI;

public sealed class AntagNameEui : BaseEui
{
    [Dependency] private readonly IEntityManager _entity = default!;

    private readonly EntityUid _rule;
    private readonly EntityUid _antag;
    private readonly string _nameFormat;
    private readonly string _currentName;
    private readonly string _roleTitle;
    private readonly int _maxNameLength;

    public AntagNameEui(
        EntityUid rule,
        EntityUid antag,
        string nameFormat,
        string currentName,
        string roleTitle,
        int maxNameLength)
    {
        IoCManager.InjectDependencies(this);

        _rule = rule;
        _antag = antag;
        _nameFormat = nameFormat;
        _currentName = currentName;
        _roleTitle = roleTitle;
        _maxNameLength = maxNameLength;
    }

    public override void Opened()
    {
        base.Opened();

        StateDirty();
    }

    public override AntagNameEuiState GetNewState()
    {
        return new AntagNameEuiState(_currentName, _roleTitle, _maxNameLength);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (IsShutDown)
            return;

        if (msg is not AntagNameSelectedMessage selected)
            return;

        if (!selected.KeepRandom)
            TryApplyName(selected.Name);

        Close();
    }

    private bool TryApplyName(string? requestedName)
    {
        if (Player.AttachedEntity != _antag ||
            _entity.Deleted(_rule) ||
            _entity.Deleted(_antag))
            return false;

        var sanitizedName = requestedName.SanitizeInput(_maxNameLength);
        if (string.IsNullOrWhiteSpace(sanitizedName))
            return false;

        var name = Loc.GetString(_nameFormat, ("name", sanitizedName));
        var meta = _entity.GetComponent<MetaDataComponent>(_antag);
        _entity.System<MetaDataSystem>().SetEntityName(_antag, name, meta);
        _entity.System<AntagSelectionSystem>().TrySetAssignedMindNameByEntity(_rule, _antag, name);
        return true;
    }
}
