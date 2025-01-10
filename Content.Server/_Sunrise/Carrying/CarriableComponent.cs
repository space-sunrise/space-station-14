using System.Threading;

namespace Content.Server.Carrying
{
    [RegisterComponent]
    public sealed partial class CarriableComponent : Component
    {
        /// <summary>
        ///     необходимое количество свободных рук
        ///     что-бы взять сущность
        /// </summary>
        [DataField("freeHandsRequired")]
        public int FreeHandsRequired = 2;

        public CancellationTokenSource? CancelToken;
    }
}
