namespace SnakeServer;

public class AiPlayer : Player, IThinkable
{
    private readonly List<(int, int)> _directions =
    [
        (1, 0), // right
        (-1, 0), // left
        (0, 1), // up
        (0, -1)  // down
    ];

    private readonly List<(int, int)> _candidateDirections = new(4);

    public AiPlayer(byte playerIndex) : base(playerIndex)
    {
    }

    public override void Move()
    {
        Snake.Move();
    }

    private Food FindClosestFood(List<Food> foods)
    {
        Food closestFood = foods[0];
        float minDistance = float.MaxValue;
        foreach (var food in foods)
        {
            float distance = (Snake.Header.Center - food.Center).LengthSquared();
            if (distance < minDistance)
            {
                minDistance = distance;
                closestFood = food;
            }
        }

        return closestFood;
    }

    public void Think(List<Wall> walls, List<Player> players, List<Food> foods)
    {
        _candidateDirections.Clear();

        foreach (var direction in _directions)
        {
            Snake.Header.DirectionX = direction.Item1;
            Snake.Header.DirectionY = direction.Item2;
            Snake.Header.Move();

            bool isCollided = Snake.CheckCollision(walls) || CheckCollisions(players);
            Snake.Header.DirectionX = -direction.Item1;
            Snake.Header.DirectionY = -direction.Item2;
            Snake.Header.Move();

            if (!isCollided)
            {
                _candidateDirections.Add(direction);
            }
        }

        if (_candidateDirections.Count == 0)
        {
            Snake.Header.DirectionX = _directions[0].Item1;
            Snake.Header.DirectionY = _directions[0].Item2;
            return;
        }

        Food closestFood = FindClosestFood(foods);
        Vector2 diff = closestFood.Center - Snake.Header.Center;
        bool found = false;
        foreach ((int, int) direction in _candidateDirections)
        {
            if (diff.X > 0 && direction.Item1 == 1)
            {
                Snake.Header.DirectionX = direction.Item1;
                Snake.Header.DirectionY = direction.Item2;
                found = true;
                break;
            }
            else if (diff.X < 0 && direction.Item1 == -1)
            {
                Snake.Header.DirectionX = direction.Item1;
                Snake.Header.DirectionY = direction.Item2;
                found = true;
                break;
            }
            else if (diff.Y > 0 && direction.Item2 == 1)
            {
                Snake.Header.DirectionX = direction.Item1;
                Snake.Header.DirectionY = direction.Item2;
                found = true;
                break;
            }
            else if (diff.Y < 0 && direction.Item2 == -1)
            {
                Snake.Header.DirectionX = direction.Item1;
                Snake.Header.DirectionY = direction.Item2;
                found = true;
                break;
            }
        }

        if (!found)
        {
            Snake.Header.DirectionX = _candidateDirections[0].Item1;
            Snake.Header.DirectionY = _candidateDirections[0].Item2;
        }
    }
}