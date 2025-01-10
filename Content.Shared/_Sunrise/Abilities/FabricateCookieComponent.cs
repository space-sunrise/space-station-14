using Content.Shared.Actions;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared._Sunrise.Abilities;

[RegisterComponent]
public sealed partial class FabricateCookieComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("cookieList")]
    public List<string> CookieList = new()
    {
        "FoodBakedCookieOatmeal"
    };

    [ViewVariables(VVAccess.ReadWrite),
     DataField("actionFabricateCookie", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionFabricateCookie = "FabricateCookie";
}


public sealed partial class FabricateCookieActionEvent : InstantActionEvent {}
