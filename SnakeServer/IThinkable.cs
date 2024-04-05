namespace SnakeServer;

public interface IThinkable
{
    public void Think(List<Wall> walls, List<Player> players, List<Food> foods);
}