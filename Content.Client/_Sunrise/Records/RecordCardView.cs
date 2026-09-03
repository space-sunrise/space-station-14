using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Records;

/// <summary>
/// Read-only карточка досье, используемая консолями станции (медицинская/охранная/трудовая).
/// </summary>
public sealed class RecordCardView : BoxContainer
{
    private static readonly Color AutomaticColor = Color.FromHex("#E2E2E6");
    private static readonly Color AuthorColor = Color.FromHex("#B8B8C0");
    private static readonly Color PlaceholderColor = Color.FromHex("#777780");
    private static readonly Color WarningColor = Color.FromHex("#D5A064");

    private readonly PanelContainer _servicePanel;
    private readonly Label _name;
    private readonly BoxContainer _serviceRows;
    private readonly BoxContainer _sections;

    public RecordCardView()
    {
        Orientation = LayoutOrientation.Vertical;
        HorizontalExpand = true;

        _name = new Label
        {
            StyleClasses = { "LabelBig" },
            Margin = new Thickness(0, 0, 0, 4),
        };
        _serviceRows = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 2,
        };
        _servicePanel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 8),
            PanelOverride = PanelStyle("#202025", "#3C3C44"),
            Children =
            {
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Vertical,
                    Margin = new Thickness(10, 8),
                    Children = { _name, _serviceRows },
                },
            },
        };
        AddChild(_servicePanel);

        // Без собственного скролла — карточка растёт по контенту, а прокручивает
        // её единый ScrollContainer консоли (вместе с заметками и остальными полями).
        _sections = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 10,
            Margin = new Thickness(2, 0, 6, 8),
        };
        AddChild(_sections);
    }

    public void SetData(RecordViewData data, bool showService = true)
    {
        var accent = Accent(data.Kind);
        _servicePanel.Visible = showService;
        _servicePanel.PanelOverride = PanelStyle("#202025", accent.ToHex());
        _name.Text = data.Identity.Name;
        _name.FontColorOverride = accent;
        _serviceRows.RemoveAllChildren();

        AddServiceRow("records-view-job", data.Identity.Job);
        AddServiceRow("records-view-fingerprint", data.Identity.Fingerprint, true);
        AddServiceRow("records-view-dna", data.Identity.Dna, true);

        _sections.RemoveAllChildren();
        foreach (var section in data.Sections)
            _sections.AddChild(BuildSection(section, accent));
    }

    private Control BuildSection(RecordViewSection section, Color accent)
    {
        var fields = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            SeparationOverride = 6,
            Margin = new Thickness(8, 6, 8, 8),
        };
        foreach (var field in section.Fields)
        {
            var sectionAlreadyLabelsField = section.Fields.Count == 1 && field.Label == section.Title;
            fields.AddChild(BuildField(field, !sectionAlreadyLabelsField));
        }

        var heading = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            Children =
            {
                new PanelContainer
                {
                    MinWidth = 3,
                    MaxWidth = 3,
                    Margin = new Thickness(0, 0, 7, 0),
                    PanelOverride = new StyleBoxFlat { BackgroundColor = accent },
                },
                new Label
                {
                    Text = section.Title,
                    StyleClasses = { "LabelBig" },
                    FontColorOverride = accent,
                },
            },
        };

        return new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children = { heading, fields },
        };
    }

    private Control BuildField(RecordViewField field, bool showLabel)
    {
        var valueColor = field.Warning
            ? WarningColor
            : field.Source switch
            {
                RecordValueSource.Automatic => AutomaticColor,
                RecordValueSource.Author => AuthorColor,
                _ => PlaceholderColor,
            };

        var value = new RichTextLabel
        {
            HorizontalExpand = true,
            LineHeightScale = 1.05f,
        };
        value.SetMessage(FormattedMessage.FromUnformatted(field.Value), valueColor);

        var longValue = field.LongValue || field.Value.Contains('\n');
        if (!longValue && showLabel)
        {
            return new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                HorizontalExpand = true,
                SeparationOverride = 8,
                Children =
                {
                    new Label
                    {
                        Text = field.Label,
                        FontColorOverride = Color.FromHex("#D4D4D8"),
                        MinWidth = 205,
                    },
                    value,
                },
            };
        }

        var content = field.Source == RecordValueSource.Placeholder
            ? (Control) value
            : new PanelContainer
            {
                HorizontalExpand = true,
                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#17171B") },
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = LayoutOrientation.Vertical,
                        Margin = new Thickness(8, 6),
                        Children = { value },
                    },
                },
            };

        if (!showLabel)
            return content;

        return new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Children =
            {
                new Label
                {
                    Text = field.Label,
                    FontColorOverride = Color.FromHex("#D4D4D8"),
                    Margin = new Thickness(0, 0, 0, 3),
                },
                content,
            },
        };
    }

    private void AddServiceRow(string labelKey, string value, bool code = false)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            SeparationOverride = 6,
            Children =
            {
                new Label
                {
                    Text = Loc.GetString(labelKey),
                    FontColorOverride = Color.FromHex("#92929A"),
                    MinWidth = 145,
                },
                new Control { HorizontalExpand = true },
                new Label
                {
                    Text = value,
                    FontColorOverride = code ? Color.FromHex("#D7D7DE") : AutomaticColor,
                    HorizontalAlignment = HAlignment.Right,
                },
            },
        };
        _serviceRows.AddChild(row);
    }

    public static Color Accent(RecordViewKind kind) => kind switch
    {
        RecordViewKind.Medical => Color.FromHex("#62A6B8"),
        RecordViewKind.Security => Color.FromHex("#B85F62"),
        _ => Color.FromHex("#66A37B"),
    };

    private static StyleBoxFlat PanelStyle(string background, string border) => new()
    {
        BackgroundColor = Color.FromHex(background),
        BorderColor = Color.FromHex(border),
        BorderThickness = new Thickness(1),
    };
}
