using Robust.Shared.GameObjects;

namespace Content.Shared.Clothing.Dirt;

// вешается на мобов в yaml - отмечает кто вообще может пачкать одежду
// без этого компонента система просто игнорирует сущность
[RegisterComponent]
public sealed partial class ClothingDirtReceiverComponent : Component { }
