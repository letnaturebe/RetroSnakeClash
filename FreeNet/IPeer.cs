namespace FreeNet
{
    public interface IPeer
    {
        void OnMessage(byte[] buffer);
        void OnRemoved();
        void ProcessUserOperation(CPacket msg);
    }
}