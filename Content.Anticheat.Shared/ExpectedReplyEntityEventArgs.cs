// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Anticheat.Shared;

/// <summary>
/// Events that have an expected reply event attached to them
/// </summary>
[Serializable, NetSerializable]
public abstract class ExpectedReplyEntityEventArgs : EntityEventArgs
{
    [field: NonSerialized]
    public abstract Type ExpectedReplyType { get; }
}
