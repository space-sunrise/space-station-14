// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Robust.Shared.GameObjects;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Anticheat.Shared.Events;

[Serializable, NetSerializable]
public sealed class AnticheatJoinReqEvent : ExpectedReplyEntityEventArgs
{
    [field: NonSerialized]
    public override Type ExpectedReplyType { get; } = typeof(AnticheatJoinRespEvent);
}

[Serializable, NetSerializable]
public sealed class AnticheatJoinRespEvent(NetUserId user) : EntityEventArgs
{
    public NetUserId User = user;
}
