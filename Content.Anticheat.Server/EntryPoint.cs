// ***
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at https://mozilla.org/MPL/2.0/.
// This Source Code Form is "Incompatible With Secondary Licenses", as defined by the Mozilla Public License, v. 2.0.
// ***

using Content.Anticheat.Server.Managers;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;

namespace Content.Anticheat.Server;

public sealed class EntryPoint : GameServer
{
    public override void PreInit()
    {
        base.PreInit();

        IoCManager.Register<IClientDataManager, ClientDataManager>();
    }

    public override void Init()
    {
        base.Init();

        IoCManager.BuildGraph();
    }
}
