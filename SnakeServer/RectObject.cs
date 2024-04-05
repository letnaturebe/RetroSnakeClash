namespace SnakeServer;

public abstract class RectObject
{
    public Vector2 Center { get; set; }
    public Vector2 Size { get; set; }

    protected Vector2 TopLeft => new(Center.X - Size.X / 2, Center.Y + Size.Y / 2);
    protected Vector2 BottomRight => new(Center.X + Size.X / 2, Center.Y - Size.Y / 2);

    protected RectObject(Vector2 center, Vector2 size)
    {
        Center = center;
        Size = size;
    }
    
    public bool AabbIntersectsWith(RectObject other)
    {
        bool xOverlap = TopLeft.X < other.BottomRight.X && BottomRight.X > other.TopLeft.X;
        bool yOverlap = TopLeft.Y > other.BottomRight.Y && BottomRight.Y < other.TopLeft.Y;
        return xOverlap && yOverlap;
    }
    
    public bool Contains(Vector2 point)
    {
        Vector2 diff = Center - point;
        return Math.Abs(diff.X) <= Size.X / 2 && Math.Abs(diff.Y) <= Size.Y / 2;
    }
}