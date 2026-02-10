using Content.Client._Sunrise.Messenger;
using Content.Shared._Sunrise.Messenger;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Robust.Shared.Input;

namespace Content.Client._Sunrise.UserInterface.CustomControls;

public sealed class EmojiPickerWindow : DefaultWindow
{
    [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;

    private readonly BoxContainer? _emojiPickerContentContainer;
    private readonly List<string> _recentEmojis = new();
    private readonly HashSet<string> _favoriteEmojis = new();

    public event Action<string>? OnEmojiSelected;

    private ClientEmojiSystem EmojiSystem => _entitySystemManager.GetEntitySystem<ClientEmojiSystem>();
    private SpriteSystem SpriteSystem => _entitySystemManager.GetEntitySystem<SpriteSystem>();

    public EmojiPickerWindow()
    {
        IoCManager.InjectDependencies(this);

        Title = Loc.GetString("messenger-emoji-picker-title");
        MinSize = new Vector2(445, 400);
        Resizable = false;

        var container = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            Margin = new Thickness(8)
        };

        var scrollContainer = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = false,
            ReserveScrollbarSpace = true
        };

        _emojiPickerContentContainer = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 5)
        };

        scrollContainer.AddChild(_emojiPickerContentContainer);
        container.AddChild(scrollContainer);
        Contents.AddChild(container);

        LoadSavedEmojis();
        UpdateEmojiPickerContent();
    }

    public void UpdateEmojiPickerContent()
    {
        if (_emojiPickerContentContainer == null)
            return;

        _emojiPickerContentContainer.RemoveAllChildren();
        var allEmojis = EmojiSystem.GetAllEmojis().ToList();
        var emojiDict = allEmojis.ToDictionary(e => e.Code, e => e);

        BuildRecentEmojisSection(_emojiPickerContentContainer, emojiDict);
        BuildFavoriteEmojisSection(_emojiPickerContentContainer, allEmojis);
        BuildAllEmojisSection(_emojiPickerContentContainer, allEmojis);
    }

    private StyleBoxTexture CreateEmojiPanelStyle()
    {
        var panelTex = _resourceCache.GetTexture("/Textures/Interface/Nano/rounded_button_bordered.svg.96dpi.png");
        var panelStyle = new StyleBoxTexture
        {
            Texture = panelTex
        };
        panelStyle.SetPatchMargin(StyleBox.Margin.All, 5);
        panelStyle.SetContentMarginOverride(StyleBox.Margin.All, 8);
        panelStyle.Modulate = Color.FromHex("#2F2F35");
        return panelStyle;
    }

    private void BuildRecentEmojisSection(BoxContainer contentContainer, Dictionary<string, EmojiPrototype> emojiDict)
    {
        var recentPanel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 10),
            PanelOverride = CreateEmojiPanelStyle()
        };

        var recentSection = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        recentSection.AddChild(new Label
        {
            Text = Loc.GetString("messenger-emoji-recent-title"),
            StyleClasses = { "Bold" },
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (_recentEmojis.Count > 0)
        {
            var recentContainer = new GridContainer
            {
                Columns = 5,
                HorizontalExpand = true
            };
            var recentToShow = _recentEmojis.TakeLast(5).Reverse().ToList();
            foreach (var emojiCode in recentToShow)
            {
                if (emojiDict.TryGetValue(emojiCode, out var emoji))
                {
                    recentContainer.AddChild(CreateEmojiButton(emoji, false));
                }
            }
            recentSection.AddChild(recentContainer);
        }
        else
        {
            recentSection.AddChild(new Label
            {
                Text = Loc.GetString("messenger-emoji-recent-empty-hint"),
                StyleClasses = { "LabelSubText" }
            });
        }
        recentPanel.AddChild(recentSection);
        contentContainer.AddChild(recentPanel);
    }

    private void BuildFavoriteEmojisSection(BoxContainer contentContainer, List<EmojiPrototype> allEmojis)
    {
        var favoritePanel = new PanelContainer
        {
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 10),
            PanelOverride = CreateEmojiPanelStyle()
        };

        var favoriteSection = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        favoriteSection.AddChild(new Label
        {
            Text = Loc.GetString("messenger-emoji-favorite-title"),
            StyleClasses = { "Bold" },
            Margin = new Thickness(0, 0, 0, 4)
        });

        if (_favoriteEmojis.Count > 0)
        {
            var favoriteContainer = new GridContainer
            {
                Columns = 5,
                HorizontalExpand = true
            };
            var favoriteEmojisList = allEmojis.Where(e => _favoriteEmojis.Contains(e.Code)).ToList();
            foreach (var emoji in favoriteEmojisList)
            {
                favoriteContainer.AddChild(CreateEmojiButton(emoji, true));
            }
            favoriteSection.AddChild(favoriteContainer);
        }
        else
        {
            favoriteSection.AddChild(new Label
            {
                Text = Loc.GetString("messenger-emoji-favorite-hint"),
                StyleClasses = { "LabelSubText" },
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
        favoritePanel.AddChild(favoriteSection);
        contentContainer.AddChild(favoritePanel);
    }

    private void BuildAllEmojisSection(BoxContainer contentContainer, List<EmojiPrototype> allEmojis)
    {
        var allPanel = new PanelContainer
        {
            HorizontalExpand = true,
            PanelOverride = CreateEmojiPanelStyle()
        };

        var allSection = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true
        };

        allSection.AddChild(new Label
        {
            Text = Loc.GetString("messenger-emoji-all-title"),
            StyleClasses = { "Bold" },
            Margin = new Thickness(0, 0, 0, 4)
        });

        var allEmojisContainer = new GridContainer
        {
            Columns = 5,
            HorizontalExpand = true
        };

        var nonFavoriteEmojis = allEmojis.Where(e => !_favoriteEmojis.Contains(e.Code)).ToList();
        foreach (var emoji in nonFavoriteEmojis)
        {
            allEmojisContainer.AddChild(CreateEmojiButton(emoji, false));
        }

        allSection.AddChild(allEmojisContainer);
        allPanel.AddChild(allSection);
        contentContainer.AddChild(allPanel);
    }

    private Button CreateEmojiButton(EmojiPrototype emoji, bool isFavorite)
    {
        var emojiButton = new Button
        {
            MinSize = new Vector2(75, 75),
            MaxSize = new Vector2(75, 75),
            ToolTip = emoji.Code
        };

        try
        {
            var spriteSpec = new SpriteSpecifier.Rsi(new ResPath(emoji.SpritePath), emoji.SpriteState);
            var state = SpriteSystem.RsiStateLike(spriteSpec);

            emojiButton.Label.Visible = false;

            if (state.IsAnimated)
            {
                var animatedRect = new AnimatedTextureRect
                {
                    SetWidth = 65,
                    SetHeight = 65,
                    HorizontalAlignment = Control.HAlignment.Center,
                    VerticalAlignment = Control.VAlignment.Center
                };
                animatedRect.SetFromSpriteSpecifier(spriteSpec);
                animatedRect.DisplayRect.HorizontalExpand = true;
                animatedRect.DisplayRect.VerticalExpand = true;
                animatedRect.DisplayRect.Stretch = TextureRect.StretchMode.KeepAspectCentered;
                emojiButton.AddChild(animatedRect);
            }
            else
            {
                var texture = SpriteSystem.Frame0(spriteSpec);
                var textureRect = new TextureRect
                {
                    Texture = texture,
                    SetWidth = 45,
                    SetHeight = 45,
                    HorizontalAlignment = Control.HAlignment.Center,
                    VerticalAlignment = Control.VAlignment.Center,
                    Stretch = TextureRect.StretchMode.KeepAspectCentered
                };
                emojiButton.AddChild(textureRect);
            }
        }
        catch
        {
            emojiButton.Text = emoji.Code;
        }

        var emojiCode = emoji.Code;

        emojiButton.OnPressed += _ =>
        {
            OnEmojiSelected?.Invoke(emojiCode);
            AddToRecentEmojis(emojiCode);
            UpdateEmojiPickerContent();
        };

        emojiButton.OnKeyBindDown += args =>
        {
            if (args.Function == EngineKeyFunctions.UIRightClick)
            {
                args.Handle();
                ToggleFavoriteEmoji(emojiCode);
                UpdateEmojiPickerContent();
            }
        };

        return emojiButton;
    }

    private void AddToRecentEmojis(string emojiCode)
    {
        _recentEmojis.Remove(emojiCode);
        _recentEmojis.Add(emojiCode);
        if (_recentEmojis.Count > 5)
        {
            _recentEmojis.RemoveAt(0);
        }
        SaveRecentEmojis();
    }

    private void ToggleFavoriteEmoji(string emojiCode)
    {
        if (!_favoriteEmojis.Add(emojiCode))
        {
            _favoriteEmojis.Remove(emojiCode);
        }
        SaveFavoriteEmojis();
    }

    private void LoadSavedEmojis()
    {
        HashSet<string>? allEmojis = null;
        try
        {
            allEmojis = EmojiSystem.GetAllEmojis().Select(e => e.Code).ToHashSet();
        }
        catch { }

        try
        {
            var recentEmojisStr = _configurationManager.GetCVar(SunriseCCVars.MessengerRecentEmojis);
            if (!string.IsNullOrWhiteSpace(recentEmojisStr))
            {
                var emojiCodes = recentEmojisStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _recentEmojis.Clear();
                foreach (var code in emojiCodes)
                {
                    if (string.IsNullOrWhiteSpace(code) || (allEmojis != null && !allEmojis.Contains(code)))
                        continue;
                    _recentEmojis.Add(code);
                    if (_recentEmojis.Count >= 5) break;
                }
            }
        }
        catch { _recentEmojis.Clear(); }

        try
        {
            var favoriteEmojisStr = _configurationManager.GetCVar(SunriseCCVars.MessengerFavoriteEmojis);
            if (!string.IsNullOrWhiteSpace(favoriteEmojisStr))
            {
                var emojiCodes = favoriteEmojisStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _favoriteEmojis.Clear();
                foreach (var code in emojiCodes)
                {
                    if (string.IsNullOrWhiteSpace(code) || (allEmojis != null && !allEmojis.Contains(code)))
                        continue;
                    _favoriteEmojis.Add(code);
                }
            }
        }
        catch { _favoriteEmojis.Clear(); }
    }

    private void SaveRecentEmojis()
    {
        try
        {
            var emojiCodesStr = string.Join(",", _recentEmojis);
            _configurationManager.SetCVar(SunriseCCVars.MessengerRecentEmojis, emojiCodesStr);
            _configurationManager.SaveToFile();
        }
        catch { }
    }

    private void SaveFavoriteEmojis()
    {
        try
        {
            var emojiCodesStr = string.Join(",", _favoriteEmojis);
            _configurationManager.SetCVar(SunriseCCVars.MessengerFavoriteEmojis, emojiCodesStr);
            _configurationManager.SaveToFile();
        }
        catch { }
    }
}
