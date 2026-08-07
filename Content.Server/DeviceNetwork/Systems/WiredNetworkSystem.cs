using Content.Server.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;
using JetBrains.Annotations;

namespace Content.Server.DeviceNetwork.Systems
{
    [UsedImplicitly]
    public sealed class WiredNetworkSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WiredNetworkComponent, BeforePacketSentEvent>(OnBeforePacketSent);
        }

        /// <summary>
        /// Checks if both devices are on the same grid
        /// </summary>
        private void OnBeforePacketSent(EntityUid uid, WiredNetworkComponent component, BeforePacketSentEvent args)
        {
            // Sunrise edit start - пакеты от сущностей в контейнерах (надетые бодикамеры) не блокируются:
            // при смене грида игроком камера меняет GridUid вместе с ним, но должна оставаться
            // доступной для роутеров/мониторов на станции.
            if (args.Sender.IsValid() && (MetaData(args.Sender).Flags & MetaDataFlags.InContainer) != 0)
                return;
            // Sunrise edit end

            if (Transform(uid).GridUid != args.SenderTransform.GridUid)
            {
                args.Cancel();
            }
        }

        //Things to do in a future PR:
        //Abstract out the connection between the apcExtensionCable and the apcPowerReceiver
        //Traverse the power cables using path traversal
        //Cache an optimized representation of the traversed path (Probably just cache Devices)
    }
}
