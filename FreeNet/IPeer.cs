namespace FreeNet
{
    public interface IPeer
    {
        void OnMessage(byte[] buffer);
        void OnRemoved();

        void Disconnect();


        void ProcessUserOperation(CPacket msg);
    }
}