// © SUNRISE, An EULA/CLA with a hosting restriction, full text: https://github.com/space-sunrise/space-station-14/blob/master/CLA.txt

using Content.Client._Sunrise.UserInterface.CustomControls;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.IoC;

namespace Content.Client._Sunrise.UserInterface.CustomControls;

/// <summary>
/// Общий хелпер для инициализации кнопки эмодзи и связывания её с окном выбора эмодзи.
/// </summary>
public static class EmojiButtonHelper
{
    private static EmojiPickerWindow? _emojiPicker;

    /// <summary>
    /// Закрывает активное окно выбора эмодзи.
    /// </summary>
    public static void ClosePicker()
    {
        _emojiPicker?.Close();
    }

    /// <summary>
    /// Инициализирует кнопку эмодзи (поддерживает как Button, так и TextureButton),
    /// устанавливает иконку и настраивает открытие EmojiPickerWindow при клике.
    /// </summary>
    public static void SetupEmojiButton(
        BaseButton emojiButton,
        LineEdit lineEdit,
        IResourceCache resourceCache)
    {
        var texture = resourceCache.GetTexture("/Textures/_Sunrise/Interface/Smile.png");

        if (emojiButton is TextureButton texBtn)
        {
            texBtn.TextureNormal = texture;
        }
        else
        {
            emojiButton.DisposeAllChildren();
            var textureRect = new TextureRect
            {
                Texture = texture,
                SetWidth = 24,
                SetHeight = 24,
                HorizontalAlignment = Control.HAlignment.Center,
                VerticalAlignment = Control.VAlignment.Center,
                Stretch = TextureRect.StretchMode.KeepAspectCentered,
            };
            emojiButton.AddChild(textureRect);
        }

        emojiButton.OnPressed += _ =>
        {
            if (_emojiPicker != null && _emojiPicker.IsOpen)
            {
                _emojiPicker.Close();
                return;
            }

            _emojiPicker = new EmojiPickerWindow();
            _emojiPicker.OnEmojiSelected += emojiCode =>
            {
                lineEdit.Text += emojiCode;
                lineEdit.CursorPosition = lineEdit.Text.Length;
                lineEdit.GrabKeyboardFocus();
            };

            _emojiPicker.OnClose += () => _emojiPicker = null;
            _emojiPicker.OpenCentered();
        };
    }
}
