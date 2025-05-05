namespace Content.Shared._Sunrise.Item;

/// <summary>
/// Этим компонентом следует помечать предметы, которые должно быть можно поднять вновь только после какой-то задержки
/// Компонент изначально делался для фелинидов: чтоб если поставить фелинида на пол, то у фелинида было N секунд убежать
/// от атакующего
/// </summary>
[RegisterComponent]
public sealed partial class ItemRepickupCooldownComponent : Component
{
    /// <summary>
    /// Сколько времени ждать перед следующим поднятием
    /// </summary>
    [DataField("cooldown", required: true)]
    public TimeSpan CooldownDuration;

    /// <summary>
    /// Время предыдущего выкладывания. null если еще никогда не выкладывали сущность
    /// </summary>
    public TimeSpan? PrevDrop;
}
