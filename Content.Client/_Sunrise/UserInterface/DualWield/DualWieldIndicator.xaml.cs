using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Sunrise.UserInterface.DualWield;

/// <summary>
/// Sunrise-Edit: Индикатор режима "стрельбы по македонски" в правом верхнем углу экрана.
/// Показывает иконку огня когда режим активен.
/// </summary>
public sealed partial class DualWieldIndicator : UIWidget
{
    public DualWieldIndicator()
    {
        RobustXamlLoader.Load(this);
        Visible = false;
    }

    public void SetActive(bool active)
    {
        Visible = active;
    }
}
