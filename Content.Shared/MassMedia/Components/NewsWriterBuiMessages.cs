using Content.Shared._Sunrise.CartridgeLoader.Cartridges;
using Content.Shared.MassMedia.Systems;
using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Components;

[Serializable, NetSerializable]
public enum NewsWriterUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class NewsWriterBoundUserInterfaceState : BoundUserInterfaceState
{
    public readonly NewsArticle[] Articles;
    public readonly bool PublishEnabled;
    public readonly TimeSpan NextPublish;
    public readonly string DraftTitle;
    public readonly string DraftContent;
    public readonly List<string>? DraftPhotoPaths;

    public NewsWriterBoundUserInterfaceState(NewsArticle[] articles, bool publishEnabled, TimeSpan nextPublish, string draftTitle, string draftContent, List<string>? draftPhotoPaths = null)
    {
        Articles = articles;
        PublishEnabled = publishEnabled;
        NextPublish = nextPublish;
        DraftTitle = draftTitle;
        DraftContent = draftContent;
        DraftPhotoPaths = draftPhotoPaths;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterPublishMessage : BoundUserInterfaceMessage
{
    public readonly string Title;
    public readonly string Content;
    public readonly List<string>? PhotoPaths;


    public NewsWriterPublishMessage(string title, string content, List<string>? photoPaths = null)
    {
        Title = title;
        Content = content;
        PhotoPaths = photoPaths;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterDeleteMessage : BoundUserInterfaceMessage
{
    public readonly int ArticleNum;

    public NewsWriterDeleteMessage(int num)
    {
        ArticleNum = num;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterArticlesRequestMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NewsWriterSaveDraftMessage : BoundUserInterfaceMessage
{
    public readonly string DraftTitle;
    public readonly string DraftContent;
    public readonly List<string>? DraftPhotoPaths;

    public NewsWriterSaveDraftMessage(string draftTitle, string draftContent, List<string>? draftPhotoPaths = null)
    {
        DraftTitle = draftTitle;
        DraftContent = draftContent;
        DraftPhotoPaths = draftPhotoPaths;
    }
}

[Serializable, NetSerializable]
public sealed class NewsWriterRequestDraftMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NewsWriterRequestPhotosMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class NewsWriterPhotosMessage : BoundUserInterfaceMessage
{
    public readonly List<PhotoMetadata> Photos;

    public NewsWriterPhotosMessage(List<PhotoMetadata> photos)
    {
        Photos = photos;
    }
}
