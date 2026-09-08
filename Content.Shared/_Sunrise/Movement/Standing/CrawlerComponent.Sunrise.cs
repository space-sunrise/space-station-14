using System.Numerics;

#pragma warning disable IDE0130
namespace Content.Shared.Stunnable;

public sealed partial class CrawlerComponent
{
    /// <summary>
    /// Максимальная дистанция одного рывка при ползании.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(Content.Shared._Sunrise.Movement.Standing.ProneCrawlMovementController))]
    public float PullDistance = 0.7f;

    /// <summary>
    /// Сколько длится один рывок.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(Content.Shared._Sunrise.Movement.Standing.ProneCrawlMovementController))]
    public TimeSpan PullDuration = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// Пауза между рывками.
    /// </summary>
    [DataField, AutoNetworkedField, Access(typeof(Content.Shared._Sunrise.Movement.Standing.ProneCrawlMovementController))]
    public TimeSpan PullPause = TimeSpan.FromSeconds(0.3f);

    /// <summary>
    /// Смещение спрайта во время анимации рывка.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PullOffset = 0.08f;

    /// <summary>
    /// Масштаб спрайта во время анимации рывка.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 PullScale = new(1.05f, 0.95f);
}
