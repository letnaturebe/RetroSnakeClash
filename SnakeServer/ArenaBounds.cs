namespace SnakeServer;

public class ArenaBounds : RectObject
{
    public ArenaBounds(List<Wall> walls) : base(new Vector2(0, 0), CalculateBound(walls))
    {
    }

    private static Vector2 CalculateBound(List<Wall> walls)
    {
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;

        foreach (var wall in walls)
        {
            if (wall.Center.X < minX)
            {
                minX = wall.Center.X;
            }

            if (wall.Center.Y > maxY)
            {
                maxY = wall.Center.Y;
            }

            if (wall.Center.X > maxX)
            {
                maxX = wall.Center.X;
            }

            if (wall.Center.Y < minY)
            {
                minY = wall.Center.Y;
            }
        }

        return new Vector2(maxX - minX, maxY - minY);
    }

    public Vector2 GetRandomPoint(List<RectObject> snakes, List<Food> foods)
    {
        Random random = new();
        while (true)
        {
            int leftX = (int)TopLeft.X + 1;
            int rightX = (int)BottomRight.X - 1;
            int bottomY = (int)BottomRight.Y;
            int topY = (int)TopLeft.Y - 2;
            float[] additional = new float[2];

            for (int i = 0; i < additional.Length; i++)
            {
                additional[i] = random.Next(0, 2) == 0 ? -0.5f : 0.5f;
            }

            float x = random.Next(leftX, rightX) + additional[0];
            float y = random.Next(bottomY, topY) + additional[1];

            Vector2 point = new(x, y);

            if (snakes.All(snake => !snake.Contains(point)) && foods.All(food => !food.Contains(point)))
            {
                return point;
            }
        }
    }
}