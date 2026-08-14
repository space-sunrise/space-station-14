using Content.Client.Eui;

namespace Content.Client._Sunrise.Antags.Vampires.UI.Thralls;

public sealed class VampireThrallEui : BaseEui
{
    private readonly VampireThrallMenu _menu = new();

    public override void Opened()
        => _menu.OpenCentered();

    public override void Closed()
    {
        base.Closed();

        _menu.Close();
    }
}
