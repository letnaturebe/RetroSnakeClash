namespace SnakeServer;

using FreeNet;
using System.Diagnostics;
using System.Threading;


class GameServerManager
{
    private static GameServerManager? _instance;
    public static GameServerManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameServerManager();
            }

            return _instance;
        }
    }

    private readonly Queue<CPacket> _userOperations;
    private readonly object _operationLock;
    private readonly List<PeerUser> _matchingWaitingUsers;
    private readonly object _matchingLock;
    private readonly AutoResetEvent _operationEvent;

    private GameServerManager()
    {
        _operationLock = new object();
        _matchingLock = new object();
        _operationEvent = new AutoResetEvent(false);
        _userOperations = new Queue<CPacket>();
        _matchingWaitingUsers = new List<PeerUser>();
        Thread gameLoop = new Thread(GameLoop);
        gameLoop.Start();
        Thread userOperation = new Thread(ProcessReceivedPackets);
        userOperation.Start();
    }

    private void GameLoop()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        long lastTick = stopwatch.ElapsedMilliseconds;

        float updateInterval = 1000f / 20f;

        while (true)
        {
            long currentTick = stopwatch.ElapsedMilliseconds;
            float deltaTime = currentTick - lastTick;

            if (deltaTime >= updateInterval)
            {
                lock (CGameRoomManager.Instance.RoomsLock)
                {
                    for (int i = CGameRoomManager.Instance.Rooms.Count - 1; i >= 0; i--)
                    {
                        CGameRoom room = CGameRoomManager.Instance.Rooms[i];
                        if (!room.Stopwatch.IsRunning)
                        {
                            continue;
                        }

                        room.Update(deltaTime / 1000f);

                        if (room.IsGameOver)
                        {
                            room.GameOver();
                            CGameRoomManager.Instance.RemoveRoom(room);
                        }
                    }
                }

                lastTick = currentTick;
            }

            Thread.Sleep(1);
        }
    }

    private void ProcessReceivedPackets()
    {
        while (true)
        {
            _operationEvent.WaitOne();
            lock (_operationLock)
            {
                while (_userOperations.Count > 0)
                {
                    CPacket packet = _userOperations.Dequeue();
                    packet.owner.ProcessUserOperation(packet);
                }
            }
        }
    }

    public void EnqueuePacket(CPacket packet)
    {
        lock (_operationLock)
        {
            _userOperations.Enqueue(packet);
            _operationEvent.Set();
        }
    }

    public void MatchingRequest(PeerUser user)
    {
        lock (_matchingLock)
        {
            if (_matchingWaitingUsers.Contains(user))
            {
                return;
            }

            _matchingWaitingUsers.Add(user);

            if (_matchingWaitingUsers.Count >= 2)
            {
                PeerUser user1 = _matchingWaitingUsers[0];
                PeerUser user2 = _matchingWaitingUsers[1];
                CGameRoomManager.Instance.CreateRoom(user1, user2);
                _matchingWaitingUsers.Remove(user1);
                _matchingWaitingUsers.Remove(user2);
            }
        }
    }
    
    public void MatchingCancel(PeerUser user)
    {
        lock (_matchingLock)
        {
            if (_matchingWaitingUsers.Contains(user))
            {
                _matchingWaitingUsers.Remove(user);
            }
        }
    }

    public void StartSoloPlay(PeerUser user)
    {
        CGameRoomManager.Instance.CreateRoom(user);
    }
    
    public void StartWithAi(PeerUser user)
    {
        CGameRoomManager.Instance.CreateAiRoom(user);
    }


    public void RemoveMatchWaiting(PeerUser user)
    {
        lock (_matchingLock)
        {
            if (_matchingWaitingUsers.Contains(user))
            {
                _matchingWaitingUsers.Remove(user);
            }
        }
    }
}