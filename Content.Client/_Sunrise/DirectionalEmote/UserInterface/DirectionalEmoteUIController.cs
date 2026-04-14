using Content.Shared._Sunrise.DirectionalEmote;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.DirectionalEmote.UserInterface;

[UsedImplicitly]
public sealed class DirectionalEmoteUIController : UIController
{
    [UISystemDependency] private readonly DirectionalEmoteSystem _directionalEmoteSystem = default!;
    [Dependency] private readonly EntityManager _entityManager = default!;

    private DirectionalEmoteWindow _emoteWindow = default!;

    public void OpenWindow(NetEntity source, NetEntity target)
    {
        EnsureWindow();

        _emoteWindow.Source = source;
        _emoteWindow.Target = target;
        _emoteWindow.Text = string.Empty;

        var sourceEntity = _entityManager.GetEntity(source);
        var targetName = Identity.Name(_entityManager.GetEntity(target),
                                       _entityManager,
                                       sourceEntity);

        _emoteWindow.Title = Loc.GetString("directional-emote-title", ("target", targetName));

        if (!_entityManager.TryGetComponent<DirectionalEmoteComponent>(sourceEntity, out var emoteComp))
            return;

        _emoteWindow.OpenCentered();
        _emoteWindow.MoveToFront();

        _emoteWindow.UpdateHideNameVisibility(emoteComp.CanHideName);

        _emoteWindow.AcceptPressed = null;
        _emoteWindow.LastEmotePressed = null;

        _emoteWindow.AcceptPressed += () =>
        {
            _directionalEmoteSystem.TrySendEmote(_emoteWindow.Source, _emoteWindow.Target, _emoteWindow.Text, _emoteWindow.HideName);

            _emoteWindow.Close();
            _emoteWindow.SetText(string.Empty);
        };

        _emoteWindow.LastEmotePressed += () =>
        {
            _emoteWindow.SetText(emoteComp.LastEmote);
        };
    }

    private void EnsureWindow()
    {
        if (_emoteWindow is { Disposed: false })
            return;

        _emoteWindow = UIManager.CreateWindow<DirectionalEmoteWindow>();
    }
}
