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
                if (departmentCandidate is Collapsible collapsible &&
                    collapsible.Body is CollapsibleBody body)
                {
                    var departmentGrid = GetDepartmentGrid(body);
                    var anyButtonVisible = departmentGrid != null && UpdateButtonsVisibility(departmentGrid);

                    collapsible.Visible = anyButtonVisible;

                    if (!string.IsNullOrEmpty(_searchText))
                        collapsible.BodyVisible = anyButtonVisible;
                    else if (_departmentCollapsibles.TryGetValue(collapsible, out var departmentKey))
                        collapsible.BodyVisible = !_collapsedDepartments.Contains(departmentKey);

                    if (anyButtonVisible)
                        anyDepartmentVisible = true;

                    continue;
                }

                if (departmentCandidate is not GridContainer directGrid)
                    continue;

                var anyDirectButtonVisible = UpdateButtonsVisibility(directGrid);
                directGrid.Visible = anyDirectButtonVisible;

                if (anyDirectButtonVisible)
                    anyDepartmentVisible = true;
            }

            bigGrid.Visible = anyDepartmentVisible;
        }
    }

    private static GridContainer? GetDepartmentGrid(CollapsibleBody body)
    {
        foreach (var child in body.Children)
        {
            if (child is GridContainer directGrid)
                return directGrid;

            if (child is not PanelContainer panel)
                continue;

            foreach (var panelChild in panel.Children)
            {
                if (panelChild is GridContainer panelGrid)
                    return panelGrid;
            }
        }

        return null;
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
