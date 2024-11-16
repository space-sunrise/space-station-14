// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

namespace Content.Anticheat.Server.Data;

/// <summary>
/// Holds data about the client
/// </summary>
public struct ClientInfo
{
    /// <summary>
    /// Checks passed or failed
    /// </summary>
    public ClientChecks Checks;

    /// <summary>
    /// Timestamp of last message given
    /// </summary>
    public TimeSpan Last;
}

