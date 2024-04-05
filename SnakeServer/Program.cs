using FreeNet;

namespace SnakeServer
{
    static class Program
    {
        private static readonly object UserListLock = new();
        private static readonly List<PeerUser> UserList = new(512);

        static void Main(string[] args)
        {
            CPacketBufferManager.initialize(1024);
            CNetworkService service = new CNetworkService(OnSessionCreated);
            service.Listen("0.0.0.0", 7979, 100);
            var heartbeatTimer = new Timer
                (CheckHeartbeat, null, Heartbeat.MAX_HEARTBEAT_SEC * 1000, Heartbeat.MAX_HEARTBEAT_SEC * 1000);
            Console.WriteLine("Started!");
            while (true)
            {
                Console.ReadLine();
                Thread.Sleep(1000);
            }
        }

        private static void CheckHeartbeat(object? state)
        {
            lock (UserListLock)
            {
                for (int i = UserList.Count - 1; i >= 0; i--)
                {
                    PeerUser user = UserList[i];
                    if (user.IsDisconnected)
                    {
                        CGameRoom? room = user.BattleRoom;
                        Console.WriteLine("Disconnected user by heartbeat");
                        if (room != null)
                        {
                            room.ForceGameOver(user.Player!);
                            CGameRoomManager.Instance.RemoveRoom(room);
                        }
                        GameServerManager.Instance.RemoveMatchWaiting(user);
                        ((IPeer)user).Disconnect();
                        UserList.RemoveAt(i);
                    }
                }
            }
        }

        private static void OnSessionCreated(CUserToken token)
        {
            PeerUser user = new PeerUser(token);
            lock (UserListLock)
            {
                UserList.Add(user);
            }
        }

        public static void RemoveUser(PeerUser user)
        {
            lock (UserListLock)
            {
                GameServerManager.Instance.RemoveMatchWaiting(user);
                CGameRoom? room = user.BattleRoom;
                if (room != null)
                {
                    CGameRoomManager.Instance.RemoveRoom(room);
                }

                UserList.Remove(user);
            }
        }
    }
}