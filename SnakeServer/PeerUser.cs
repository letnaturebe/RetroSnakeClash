﻿using System.Diagnostics;
using FreeNet;

namespace SnakeServer;

public class PeerUser : IPeer, ISendable
{
    private readonly CUserToken _token;
    public CGameRoom? BattleRoom { get; private set; }

    public UserPlayer? Player { get; private set; }

    public bool IsDisconnected
    {
        get { return _latestHeartbeatTime + (Heartbeat.MAX_HEARTBEAT_SEC + 2) * 10000000 < DateTime.Now.Ticks; }
    }

    private long _latestHeartbeatTime = DateTime.Now.Ticks;

    public PeerUser(CUserToken token)
    {
        _token = token;
        _token.SetPeer(this);
        var heartbeat = new Heartbeat(0, OnHeartbeatSend);
    }

    private void OnHeartbeatSend()
    {
        CPacket msg = CPacket.create((short)Protocol.HEARTBEAT_SEND);
        Send(msg);
    }

    void IPeer.OnMessage(byte[] buffer)
    {
        byte[] clone = new byte[1024];
        Array.Copy(buffer, clone, buffer.Length);
        CPacket msg = new CPacket(clone, this);
        GameServerManager.Instance.EnqueuePacket(msg);
    }

    void IPeer.OnRemoved()
    {
        Console.WriteLine("The client disconnected.");
        Program.RemoveUser(this);
    }

    public void Send(CPacket msg, bool dispose = true)
    {
        _token.Send(msg);
        if (dispose)
        {
            CPacket.Destroy(msg);
        }
    }

    void IPeer.Disconnect()
    {
        _token.Socket!.Disconnect(false);
    }

    void IPeer.ProcessUserOperation(CPacket msg)
    {
        Protocol protocol = (Protocol)msg.pop_protocol_id();
        switch (protocol)
        {
            case Protocol.SOLO_PLAY:
                GameServerManager.Instance.StartSoloPlay(this);
                break;
            case Protocol.PLAY_WITH_AI:
                GameServerManager.Instance.StartWithAi(this);
                break;
            case Protocol.ENTER_GAME_ROOM_REQ:
                GameServerManager.Instance.MatchingRequest(this);
                break;
            case Protocol.MATCHING_CANCEL:
                GameServerManager.Instance.MatchingCancel(this);
                break;
            case Protocol.HEARTBEAT_ACK:
                _latestHeartbeatTime = DateTime.Now.Ticks;
                break;
            case Protocol.LOADING_COMPLETED:
                if (Player == null || BattleRoom == null) return;
                BattleRoom.LoadingComplete(Player, msg);
                break;
            case Protocol.CHANGE_SNAKE_DIRECTION:
                if (Player == null || BattleRoom == null) return;
                Player.UpdateDirection(msg.pop_int32(), msg.pop_int32());
                break;
        }
    }

    public void EnterRoom(UserPlayer player, CGameRoom room)
    {
        Player = player;
        BattleRoom = room;
    }
}