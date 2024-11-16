// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

namespace Content.Anticheat.Server.Data;

/// <summary>
///  Status of checks send by the client
/// </summary>
public struct ClientChecks
{
    /// <summary>
    /// Metadata classes commonly found in marseyloader patches
    /// "MarseyPatch", "SubverterPatch", "MarseyEntry", "Sedition"
    /// </summary>
    public ClientCheck Metadata;

    /// <summary>
    /// Extra modules that are not present for regular clients
    /// </summary>
    public ClientCheck Module;

    /// <summary>
    /// Extra types that are not in the allowed list of assembly modules
    /// </summary>
    public ClientCheck ExtraTypes;

    /// <summary>
    /// Cvars with suspicious names, like "aimbot"
    /// </summary>
    public ClientCheck SuspiciousCVars;

    /// <summary>
    /// Windows that are not from the game module
    /// </summary>
    public ClientCheck ExtraWindows;

    /// <summary>
    /// Timeouted on a check or didn't send a netmessage requested of the client
    /// </summary>
    public ClientCheck Timeouted;
}
