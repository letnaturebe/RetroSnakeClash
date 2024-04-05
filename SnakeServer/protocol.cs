namespace SnakeServer;

public enum Protocol : short
{
    # region Game Room
    START_LOADING = 0,
    LOADING_COMPLETED = 1,
    GAME_START = 2,
    GAME_OVER = 3,
    ENTER_GAME_ROOM_REQ = 4,
    SOLO_PLAY = 5,
    PLAY_WITH_AI = 6,
    MATCHING_CANCEL = 7,
    HEARTBEAT_SEND = 8,
    HEARTBEAT_ACK = 9,
    # endregion
    
    # region Game Logic
    CHANGE_SNAKE_DIRECTION = 10,
    SYNC_GAME_STATUS = 11,
    # endregion
}