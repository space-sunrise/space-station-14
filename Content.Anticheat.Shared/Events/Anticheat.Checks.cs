// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Anticheat.Shared.Events;

// Events are duplicated so that they'd have to be patched out individually

[Serializable, NetSerializable]
public sealed class TypeCheckOne : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed class TypeCheckTwo : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed class TypeCheckThree : EntityEventArgs
{

}

[Serializable, NetSerializable]
public sealed class TypeCheckFour : EntityEventArgs
{

}
