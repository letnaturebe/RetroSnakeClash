namespace SnakeServer;

public class Food : RectObject
{
    public bool IsEnabled { get; set; }
    
    public Food(Vector2 center, Vector2 size) : base(center, size)
    {
        IsEnabled = true;
    }
}