using FreeNet;
using System.Diagnostics;

namespace SnakeServer;

public class CGameRoom
{
    private enum PLAYER_STATE : byte
    {
        ENTERED_ROOM,
        LOADING_COMPLETE,
    }

    public readonly Stopwatch Stopwatch = new();
    public bool IsGameOver => _collidedPlayers.Count > 0;

    private const float _updateTime = 0.15f;
    private readonly object _roomLock = new();
    private readonly List<Player> _players = new(2);
    private IThinkable? _thinkable;
    private readonly List<ISendable> _sendables = new(2);
    private readonly List<Food> _foods = new(3);
    private readonly List<Wall> _walls = new(4);
    private readonly List<Player> _collidedPlayers = new(2);
    private readonly List<RectObject> _invalidFoodRects = new(300);
    private float _elapsedTime;
    private readonly Vector2 _rectSize = new(0.5f, 0.5f);
    private ArenaBounds _arenaBounds = new([]);
    private readonly Dictionary<byte, PLAYER_STATE> _playerState = [];

    private void Broadcast(CPacket msg)
    {
        lock (_roomLock)
        {
            foreach (ISendable sendable in _sendables)
            {
                sendable.Send(msg, false);
            }
        }

        CPacket.Destroy(msg);
    }

    public void Update(float deltaTime)
    {
        _elapsedTime += deltaTime;

        if (_elapsedTime < _updateTime)
        {
            return;
        }

        Debug.Assert(_foods.Count != 0, "Foods length is 0");
        Debug.Assert(Stopwatch.IsRunning, "Stopwatch is not running");

        lock (_roomLock)
        {
            _thinkable?.Think(_walls, _players, _foods);

            foreach (var player in _players)
            {
                player.Move();

                if (player.CheckCollisions(_walls) || player.CheckCollisions(_players))
                {
                    _collidedPlayers.Add(player);
                    continue;
                }

                if (player.TryCheckCollision(_foods, out Food outFood))
                {
                    _invalidFoodRects.Clear();
                    foreach (var p in _players)
                    {
                        p.AddRectsTo(_invalidFoodRects);
                    }

                    _foods.Remove(outFood);
                    _foods.Add(new Food(_arenaBounds.GetRandomPoint(_invalidFoodRects, _foods), _rectSize));
                    player.AddBody();
                }
            }

            if (IsGameOver)
            {
                return;
            }

            CPacket msg = CPacket.create((short)Protocol.SYNC_GAME_STATUS);
            msg.push(Stopwatch.ElapsedMilliseconds / 1000f);
            msg.push((byte)_players.Count);
            foreach (var player in _players)
            {
                player.AddSnakeInfo(msg);
            }

            msg.push((byte)_foods.Count);
            foreach (var food in _foods)
            {
                msg.push(food.Center.X);
                msg.push(food.Center.Y);
            }

            Broadcast(msg);
            _elapsedTime = 0;
        }
    }
    
    public void ForceGameOver(Player disconnectedPlayer)
    {
        lock (_roomLock)
        {
            _collidedPlayers.Add(disconnectedPlayer);
            GameOver();
        }
    }

    public void GameOver()
    {
        Debug.Assert(IsGameOver, "IsGameOver must be true");
        Debug.Assert(_collidedPlayers.Count > 0, "_collidedPlayers length is 0");

        byte winnerIndex;
        if (_collidedPlayers.Count == 2)
        {
            winnerIndex = 255;
        }
        else if (_collidedPlayers[0].PlayerIndex == 1)
        {
            winnerIndex = 0;
        }
        else if (_collidedPlayers[0].PlayerIndex == 0)
        {
            winnerIndex = 1;
        }
        else
        {
            Debug.Assert(false, "Invalid winnerIndex");
            return;
        }

        CPacket msg = CPacket.create((short)Protocol.GAME_OVER);
        msg.push(winnerIndex);
        Broadcast(msg);
    }

    private void ChangePlayerState(Player player, PLAYER_STATE state)
    {
        _playerState[player.PlayerIndex] = state;
    }

    private bool IsReady(PLAYER_STATE state)
    {
        foreach (KeyValuePair<byte, PLAYER_STATE> kvp in _playerState)
        {
            if (kvp.Value != state)
            {
                return false;
            }
        }

        return true;
    }

    public void EnterGameRoom(PeerUser user1, PeerUser user2)
    {
        EnterPlayer(user1, 0, PLAYER_STATE.ENTERED_ROOM);
        EnterPlayer(user2, 1, PLAYER_STATE.ENTERED_ROOM);
    }

    public void EnterGameRoom(PeerUser user)
    {
        EnterPlayer(user, 0, PLAYER_STATE.ENTERED_ROOM);
    }

    public void EnterAiGameRoom(PeerUser user)
    {
        EnterPlayer(user, 0, PLAYER_STATE.ENTERED_ROOM);
        var aiPlayer = new AiPlayer(1);
        ChangePlayerState(aiPlayer, PLAYER_STATE.LOADING_COMPLETE);
        lock (_roomLock)
        {
            _players.Add(aiPlayer);
            _thinkable = aiPlayer;
        }
    }
    
    private void EnterPlayer(PeerUser user, byte playerIndex, PLAYER_STATE state)
    {
        UserPlayer player = new UserPlayer(user, playerIndex);
        ChangePlayerState(player, state);

        lock (_roomLock)
        {
            _players.Add(player);
            _sendables.Add(player);
        }

        CPacket msg = CPacket.create((short)Protocol.START_LOADING);
        msg.push(player.PlayerIndex);
        player.Send(msg);
        user.EnterRoom(player, this);
    }

    public void LoadingComplete(UserPlayer player, CPacket msg)
    {
        ChangePlayerState(player, PLAYER_STATE.LOADING_COMPLETE);
        if (!IsReady(PLAYER_STATE.LOADING_COMPLETE))
        {
            return;
        }

        if (_walls.Count != 0)
        {
            Debug.Assert(_arenaBounds.Center.X != 0, "_rectBound must be initialized before this point.");
            BattleStart();
            return;
        }

        byte wallCount = msg.pop_byte();
        for (int i = 0; i < wallCount; i++)
        {
            float x1 = msg.pop_float();
            float y1 = msg.pop_float();
            float x2 = msg.pop_float();
            float y2 = msg.pop_float();
            Wall wall = new Wall(new Vector2(x1, y1), new Vector2(x2, y2));
            _walls.Add(wall);
        }

        Debug.Assert(_walls.Count != 0, "Walls's length is 0");

        _arenaBounds = new ArenaBounds(_walls);
        BattleStart();
    }


    private void BattleStart()
    {
        ResetGameData();

        Debug.Assert(_foods.Count != 0, "Foods length is 0");

        lock (_players)
        {
            CPacket msg = CPacket.create((short)Protocol.GAME_START);
            msg.push((byte)_players.Count);
            foreach (var player in _players)
            {
                player.AddSnakeInfo(msg);
            }

            msg.push((byte)_foods.Count);
            foreach (Food food in _foods)
            {
                msg.push(food.Center.X);
                msg.push(food.Center.Y);
            }

            Broadcast(msg);
        }
    }

    private void ResetGameData()
    {
        _invalidFoodRects.Clear();
        lock (_players)
        {
            foreach (var player in _players)
            {
                player.ResetPlayer(_rectSize);
                player.AddRectsTo(_invalidFoodRects);
            }
        }

        for (int i = 0; i < 3; i++)
        {
            Food food = new Food(_arenaBounds.GetRandomPoint(_invalidFoodRects, _foods), _rectSize);
            _foods.Add(food);
            _invalidFoodRects.Add(food);
        }

        Stopwatch.Start();
    }
}