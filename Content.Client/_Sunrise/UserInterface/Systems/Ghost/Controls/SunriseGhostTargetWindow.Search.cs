using System.Numerics;
using Content.Client._Sunrise.UserInterface.Controls;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Sunrise.UserInterface.Systems.Ghost.Controls;

public sealed partial class SunriseGhostTargetWindow
{
    // Введенный в поисковую строку текст, закешированный для удобного доступа
    private string _searchText = string.Empty;

    private void OnSearchTextChanged(LineEdit.LineEditEventArgs args)
    {
        _searchText = args.Text;

        UpdateVisibleButtons();
        Scroll.SetScrollValue(Vector2.Zero); // Устанавливает ползунок в начало
    }

    /// <summary>
    /// Динамически скрывает лишние элементы меню, которые не содержат введенного в поиске текста.
    /// Проходится по контейнерам-родителям, в поисках кнопки
    /// </summary>
    private void UpdateVisibleButtons()
    {
        foreach (var bigGridCandidate in GhostTeleportContainer.Children)
        {
            if (bigGridCandidate is not GridContainer bigGrid)
                continue;

            var anyDepartmentVisible = false;

            foreach (var departmentCandidate in bigGrid.Children)
            {
                if (departmentCandidate is not GridContainer departmentGrid)
                    continue;

                var anyButtonVisible = UpdateButtonsVisibility(departmentGrid);
                departmentGrid.Visible = anyButtonVisible;

                if (anyButtonVisible)
                    anyDepartmentVisible = true;
            }

            bigGrid.Visible = anyDepartmentVisible;
        }
    }

    /// <summary>
    /// Динамически скрывает лишние кнопки, которые не содержат введенный в поиске текст
    /// Если в найденном контейнере нет ничего возвращает false и весь контейнер скрывается
    /// </summary>
    /// <param name="departmentGrid">Контейнер, непосредственно содержащий кнопки</param>
    /// <returns>Имеет ли переданный контейнер хоть одну кнопку с введенным текстом из поиска</returns>
    private bool UpdateButtonsVisibility(GridContainer departmentGrid)
    {
        var foundVisible = false;

        foreach (var child in departmentGrid.Children)
        {
            if (child is not RichTextButton button)
                continue;

            var isVisible = ButtonIsVisible(button);
            button.Visible = isVisible;

            if (isVisible)
                foundVisible = true;
        }

        return foundVisible;
    }

    /// <summary>
    /// Проверяет, содержит ли кнопка введенный в поиске текст
    /// </summary>
    /// <param name="button">Кнопка для проверки</param>
    /// <returns>Содержит ли кнопка введенный текст. Если нет -> кнопка не должна быть видна</returns>
    private bool ButtonIsVisible(RichTextButton button)
    {
        return string.IsNullOrEmpty(_searchText)
               || button.ToolTip == null
               || button.ToolTip.Contains(_searchText, StringComparison.OrdinalIgnoreCase);
    }
}
