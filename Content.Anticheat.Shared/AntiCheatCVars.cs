// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Robust.Shared.Configuration;

namespace Content.Anticheat.Shared;

[CVarDefs]
public sealed class AntiCheatCVars
{
    /// <summary>
    /// How much time is given for the client to reply to a request, in seconds
    /// </summary>
    public static readonly CVarDef<float> AntiCheatResponseTimeout =
        CVarDef.Create("ac.ResponseTimeout", 30f, CVar.SERVERONLY);

    public static readonly CVarDef<bool> AntiCheatKickResponseTimeout =
        CVarDef.Create("ac.KickTimeout", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> AntiCheatKickResonseTimeoutReason =
        CVarDef.Create("ac.KickTimeoutReason", "Exceeded allotted time for a response.", CVar.SERVERONLY);
}
