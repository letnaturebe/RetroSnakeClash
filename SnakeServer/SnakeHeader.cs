using System.Diagnostics;

namespace SnakeServer;

public class SnakeHeader : RectObject
{
    public int DirectionX;
    public int DirectionY;

    public SnakeHeader(Vector2 center, Vector2 size) : base(center, size)
    {
    }

    public void Move()
    {
        Debug.Assert(!(DirectionX == 0 && DirectionY == 0));
        Center += new Vector2(DirectionX * 0.5f, DirectionY * 0.5f);
    }
}