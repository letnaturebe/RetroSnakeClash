using FreeNet;

namespace SnakeServer;

public interface ISendable
{
    public void Send(CPacket msg, bool dispose = true);
}