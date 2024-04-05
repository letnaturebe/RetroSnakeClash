using FreeNet;
using System.Numerics;

namespace SnakeServer;

public abstract class Player
{
    public byte PlayerIndex { get; private set; }
    protected Snake Snake { get; private set; }

    protected Player(byte playerIndex)
    {
        PlayerIndex = playerIndex;
        Snake = new Snake(Vector2.Zero, new Vector2(0, 0));
        ResetPlayer(new Vector2(0, 0));
    }

    public void ResetPlayer(Vector2 snakeSize)
    {
        switch (PlayerIndex)
        {
            case 0:
                Snake.Header.Center = new Vector2(-5, 0);
                Snake.Header.DirectionX = 1;
                Snake.Header.DirectionY = 0;
                Snake.Header.Size = snakeSize;
                break;
            case 1:
                Snake.Header.Center = new Vector2(5, 0);
                Snake.Header.DirectionX = -1;
                Snake.Header.DirectionY = 0;
                Snake.Header.Size = snakeSize;
                break;
            case 2:
                Snake.Header.Center = new Vector2(0, -5);
                Snake.Header.DirectionX = 1;
                Snake.Header.DirectionY = 0;
                Snake.Header.Size = snakeSize;
                break;
            case 3:
                Snake.Header.Center = new Vector2(0, 5);
                Snake.Header.DirectionX = -1;
                Snake.Header.DirectionY = 0;
                Snake.Header.Size = snakeSize;
                break;
        }
    }

    public abstract void Move();

    public bool CheckCollisions(List<Wall> walls)
    {
        return Snake.CheckCollision(walls);
    }

    public bool CheckCollisions(List<Player> players)
    {
        foreach (var opponent in players)
        {
            if (Snake.CheckCollision(opponent.Snake))
            {
                return true;
            }
        }

        return false;
    }

    public bool TryCheckCollision(List<Food> foods, out Food outFood)
    {
        return Snake.TryCheckCollision(foods, out outFood);
    }

    public void AddBody()
    {
        Snake.AddBody();
    }

    public void AddRectsTo(List<RectObject> invalidFoodRects)
    {
        Snake.AddRectsTo(invalidFoodRects);
    }

    public void AddSnakeInfo(CPacket msg)
    {
        msg.push(PlayerIndex);
        msg.push(Snake.Header.Center.X);
        msg.push(Snake.Header.Center.Y);
        msg.push((byte)Snake.Bodies.Count);
    }
}