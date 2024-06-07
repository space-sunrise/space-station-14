using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using System;

namespace Content.Shared._Sunrise.TicketMachine
{
    public class Cooldown
    {
        private TimeSpan _start;
        private TimeSpan _duration;
        private bool _refresh;

        public Cooldown(TimeSpan duration, bool refresh = true)
        {
            _duration = duration;
            _refresh = refresh;
            _start = TimeSpan.Zero;
        }

        public void Start(TimeSpan currentTime)
        {
            _start = currentTime;
        }

        public bool IsCoolingDown(TimeSpan currentTime)
        {
            if (!_refresh)
            {
                return currentTime < _start + _duration;
            }
            else
            {
                return true;
            }
        }

        public void Reset()
        {
            _start = TimeSpan.Zero;
        }
    }

	[RegisterComponent]
    public partial class TicketComponent : Component
    {
        private int _number;
        private Cooldown _setNumberCooldown;
        private Cooldown _burnCooldown;

        public TicketComponent()
        {
            // Set up a default cooldown of 1 second for setting the number
            _setNumberCooldown = new Cooldown(TimeSpan.FromSeconds(1));

            // Set up a default cooldown of 2 seconds for burning tickets
            _burnCooldown = new Cooldown(TimeSpan.FromSeconds(2));
        }

        public void SetNumber(int number, TimeSpan currentTime)
        {
            // Check if the set number cooldown is still active
            if (_setNumberCooldown.IsCoolingDown(currentTime))
            {
                // If still cooling down, do nothing
                return;
            }

            // If not cooling down, set the number and start the cooldown
            _number = number;

            // Perform any additional actions related to setting the number here...

            // Start the cooldown for setting the number
            _setNumberCooldown.Start(currentTime);
        }

        public void Burn(TimeSpan currentTime)
        {
            // Check if the burning cooldown is still active
            if (_burnCooldown.IsCoolingDown(currentTime))
            {
                // If still cooling down, do nothing
                return;
            }

            // If not cooling down, start the burning process
            // Perform burning actions here...

            // Start the burning cooldown
            _burnCooldown.Start(currentTime);
        }
    }
}