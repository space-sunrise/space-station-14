using JetBrains.Annotations;
using Content.Shared.MassMedia.Systems;
using Content.Shared.MassMedia.Components;
using Robust.Client.UserInterface;
using Robust.Shared.Utility;

namespace Content.Client.MassMedia.Ui;

[UsedImplicitly]
public sealed class NewsWriterBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private NewsWriterMenu? _menu;
    private PhotoSelectorWindow? _selector; // Sunrise-Edit

    public NewsWriterBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {

    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindow<NewsWriterMenu>();

        _menu.ArticleEditorPanel.PublishButtonPressed += OnPublishButtonPressed;
        _menu.DeleteButtonPressed += OnDeleteButtonPressed;

        _menu.CreateButtonPressed += OnCreateButtonPressed;
        _menu.ArticleEditorPanel.ArticleDraftUpdated += OnArticleDraftUpdated;
        _menu.ArticleEditorPanel.RequestPhotosPressed += OnRequestPhotosPressed; // Sunrise-Edit

        SendMessage(new NewsWriterArticlesRequestMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        // Sunrise-Start
        if (state is NewsWriterBoundUserInterfaceState cast)
        {
            _menu?.UpdateUI(cast.Articles, cast.PublishEnabled, cast.NextPublish, cast.DraftTitle, cast.DraftContent);
            if (_menu != null)
            {
                _menu.ArticleEditorPanel.PhotoPaths = cast.DraftPhotoPaths != null ? new List<string>(cast.DraftPhotoPaths) : new List<string>();
                _menu.ArticleEditorPanel.UpdatePhotosUI();
            }
        }
        // Sunrise-End
    }

    // Sunrise-Start
    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is NewsWriterPhotosMessage photosMsg)
        {
            if (_selector != null && _selector.IsOpen)
            {
                _selector.Populate(photosMsg.Photos);
                return;
            }

            _selector = new PhotoSelectorWindow();
            _selector.PhotoSelected += path =>
            {
                if (_menu == null) return;
                if (!_menu.ArticleEditorPanel.PhotoPaths.Contains(path))
                {
                    _menu.ArticleEditorPanel.PhotoPaths.Add(path);
                    _menu.ArticleEditorPanel.UpdatePhotosUI();
                    OnArticleDraftUpdated(_menu.ArticleEditorPanel.TitleField.Text, Rope.Collapse(_menu.ArticleEditorPanel.ContentField.TextRope), _menu.ArticleEditorPanel.PhotoPaths);
                }
            };
            _selector.OnClose += () => _selector = null;
            _selector.Populate(photosMsg.Photos);
            _selector.OpenCentered();
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _selector?.Close();
    }
    // Sunrise-End

    private void OnPublishButtonPressed()
    {
        var title = _menu?.ArticleEditorPanel.TitleField.Text.Trim() ?? "";
        if (_menu == null || title.Length == 0)
            return;

        var stringContent = Rope.Collapse(_menu.ArticleEditorPanel.ContentField.TextRope).Trim();

        if (stringContent.Length == 0)
            return;

        var name = title.Length <= SharedNewsSystem.MaxTitleLength
            ? title
            : $"{title[..(SharedNewsSystem.MaxTitleLength - 3)]}...";

        var content = stringContent.Length <= SharedNewsSystem.MaxContentLength
            ? stringContent
            : $"{stringContent[..(SharedNewsSystem.MaxContentLength - 3)]}...";


        SendMessage(new NewsWriterPublishMessage(name, content, _menu.ArticleEditorPanel.PhotoPaths)); // Sunrise-Edit
    }

    private void OnDeleteButtonPressed(int articleNum)
    {
        if (_menu == null)
            return;

        SendMessage(new NewsWriterDeleteMessage(articleNum));
    }

    private void OnCreateButtonPressed()
    {
        SendMessage(new NewsWriterRequestDraftMessage());
    }

    private void OnArticleDraftUpdated(string title, string content, List<string>? photoPaths) // Sunrise-Edit
    {
        SendMessage(new NewsWriterSaveDraftMessage(title, content, photoPaths)); // Sunrise-Edit
    }

    // Sunrise-Start
    private void OnRequestPhotosPressed()
    {
        SendMessage(new NewsWriterRequestPhotosMessage());
    }
    // Sunrise-End
}
