using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;

namespace Content.Shared.Clothing.Dirt;

// добавляется динамически когда одежда впервые пачкается
// не вешайте его в yaml на все предметы подряд, только через систему
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ClothingDirtComponent : Component
{
    [AutoNetworkedField, DataField]
    public float DirtLevel = 0f; // 0..100

    [AutoNetworkedField, DataField]
    public Color DirtColor = Color.Transparent;

    // отдельные слои грязи - у каждого свой цвет и интенсивность
    // нужно чтобы кровь + лужа с химикатом не сливались в одну кашу
    [AutoNetworkedField, DataField]
    public List<DirtLayer> Layers = new();

    // сколько грязи за 1 секунду при ползании/лежании
    [DataField]
    public float DirtRatePerSecond = 10f;
}

[DataDefinition, Serializable, NetSerializable]
public sealed partial class DirtLayer
{
    [DataField]
    public Color Color = Color.White;

    [DataField]
    public float Intensity = 0f; // 0..100
}
