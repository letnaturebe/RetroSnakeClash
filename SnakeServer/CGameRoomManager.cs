
namespace SnakeServer;

public class CGameRoomManager
{
    private static CGameRoomManager? _instance;
    public static CGameRoomManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new CGameRoomManager();
            }

            return _instance;
        }
    }

    private CGameRoomManager()
    {
    }

    public readonly List<CGameRoom> Rooms = new();
    public readonly object RoomsLock = new();

    public void CreateRoom(PeerUser user1, PeerUser user2)
    {
        lock (RoomsLock)
        {
            CGameRoom gameRoom = new CGameRoom();
            gameRoom.EnterGameRoom(user1, user2);
            Rooms.Add(gameRoom);
        }
    }

    public void CreateRoom(PeerUser user)
    {
        lock (RoomsLock)
        {
            CGameRoom gameRoom = new CGameRoom();
            gameRoom.EnterGameRoom(user);
            Rooms.Add(gameRoom);
        }
    }
    
    public void CreateAiRoom(PeerUser user)
    {
        lock (RoomsLock)
        {
            CGameRoom gameRoom = new CGameRoom();
            gameRoom.EnterAiGameRoom(user);
            Rooms.Add(gameRoom);
        }
    }
    
    public void RemoveRoom(CGameRoom gameRoom)
    {
        lock (RoomsLock)
        {
            Rooms.Remove(gameRoom);
        }
    }
}