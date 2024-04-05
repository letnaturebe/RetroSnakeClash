using System.Diagnostics;
using FreeNet;

namespace SnakeServer
{
    public class UserPlayer : Player, ISendable
    {
        private readonly PeerUser _owner;

        public UserPlayer(PeerUser user, byte playerIndex) : base(playerIndex)
        {
            _owner = user;
        }

        public void Send(CPacket msg, bool dispose = true)
        {
            _owner.Send(msg, dispose);
        }

        public override void Move()
        {
            Snake.Move();
        }

        public void UpdateDirection(int inputX, int inputY)
        {
            if (inputX == 0)
            {
                Debug.Assert(inputY != 0, "inputY != 0");
            }

            if (inputY == 0)
            {
                Debug.Assert(inputX != 0, "inputX != 0");
            }

            if (Snake.Header.DirectionX != 0 && inputY != 0)
            {
                Snake.Header.DirectionX = 0;
                Snake.Header.DirectionY = inputY;
            }
            else if (Snake.Header.DirectionY != 0 && inputX != 0)
            {
                Snake.Header.DirectionX = inputX;
                Snake.Header.DirectionY = 0;
            }
        }
    }
}