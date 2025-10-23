using System.Threading;
using Robust.Shared.GameStates;

namespace Content.Shared._Sunrise.Carrying
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class CarriableComponent : Component
    {
        /// <summary>
        ///     Необходимое количество свободных рук чтобы взять сущность
        /// </summary>
        [DataField]
        public int FreeHandsRequired = 2;

        [DataField]
        public float WalkSpeedModifier = 0.6f;

        [DataField]
        public float SprintSpeedModifier = 0.6f;

        /// <summary>
        ///     Если true, то переносить этот объект могут только мобы с компонентом NestingMobComponent
        /// </summary>
        [DataField]
        public bool RequiresNestingMob = false;
    }
}
