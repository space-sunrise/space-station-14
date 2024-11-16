// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

namespace Content.Anticheat.Server.Data;

public sealed class ClientCheck
{
    public bool Flag;
    public string Description;

    public ClientCheck(bool flag, string description)
    {
        Flag = flag;
        Description = description;
    }
}
