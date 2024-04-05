namespace SnakeServer;

public class Snake
{
    public SnakeHeader Header { get; }
    public List<SnakeBody> Bodies { get; }

    public Snake(Vector2 center, Vector2 size)
    {
        Header = new SnakeHeader(center, size);
        Bodies = new List<SnakeBody>(150);
    }

    public bool CheckCollision(List<Wall> walls)
    {
        foreach (var wall in walls)
        {
            if (Header.AabbIntersectsWith(wall))
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckCollision(Snake other)
    {
        foreach (var body in other.Bodies)
        {
            if (Header.AabbIntersectsWith(body))
            {
                return true;
            }
        }

        return false;
    }


    public bool TryCheckCollision(List<Food> foods, out Food outFood)
    {
        foreach (var food in foods)
        {
            if (Header.AabbIntersectsWith(food))
            {
                outFood = food;
                return true;
            }
        }

        outFood = null!;
        return false;
    }

    public void Move()
    {
        for (int i = Bodies.Count - 1; i > 0; i--)
        {
            Bodies[i].Center = Bodies[i - 1].Center;
        }

        if (Bodies.Count > 0)
        {
            Bodies[0].Center = Header.Center;
        }

        Header.Move();
    }

    public void AddBody()
    {
        RectObject lastBody = Header;
        if (Bodies.Count > 0)
        {
            lastBody = Bodies[^1];
        }

        Vector2 newBodyCenter;
        if (Header.DirectionX == 1) // Moving to right
        {
            newBodyCenter = lastBody.Center + new Vector2(-lastBody.Size.X, 0);
        }
        else if (Header.DirectionX == -1) // Moving to left
        {
            newBodyCenter = lastBody.Center + new Vector2(lastBody.Size.X, 0);
        }
        else if (Header.DirectionY == 1) // Moving to up
        {
            newBodyCenter = lastBody.Center + new Vector2(0, -lastBody.Size.Y);
        }
        else if (Header.DirectionY == -1) // Moving to down
        {
            newBodyCenter = lastBody.Center + new Vector2(0, lastBody.Size.Y);
        }
        else
        {
            throw new Exception("Invalid direction");
        }

        Bodies.Add(new SnakeBody(newBodyCenter, lastBody.Size));
    }

    public void AddRectsTo(List<RectObject> invalidFoodRects)
    {
        invalidFoodRects.Add(Header);
        invalidFoodRects.AddRange(Bodies);
    }
}