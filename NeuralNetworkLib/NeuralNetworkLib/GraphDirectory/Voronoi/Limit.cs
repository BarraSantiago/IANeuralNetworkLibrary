using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory.Voronoi;

public enum Direction
{
    Up,
    Right,
    Down,
    Left
}

public class Limit<TCoordinate, TCoordinateType> 
    where TCoordinate : ICoordinate<TCoordinateType>, new()
    where TCoordinateType : IEquatable<TCoordinateType>
{
    private TCoordinate origin;
    private readonly Direction direction;

    public Limit(TCoordinate origin, Direction direction)
    {
        this.origin = origin;
        this.direction = direction;
    }

    public TCoordinate GetMapLimitPosition(TCoordinate position)
    {
        // Create a copy of the origin to avoid modifying the original object
        TCoordinate originCopy = new TCoordinate();
        originCopy.SetX(origin.GetX());
        originCopy.SetY(origin.GetY());
    
        // Calculate the distance to the limit
        TCoordinate distance = new TCoordinate();
        distance.SetCoordinate(Math.Abs(position.GetX() - originCopy.GetX()) * 2f, Math.Abs(position.GetY() - originCopy.GetY()) * 2f);
        TCoordinate limit = new TCoordinate();
        limit.SetX(position.GetX());
        limit.SetY(position.GetY());

        switch (direction)
        {
            case Direction.Left:
                limit.SetX(position.GetX() - distance.GetX());
                break;
            case Direction.Up:
                limit.SetY(position.GetY() + distance.GetY());
                break;
            case Direction.Right:
                limit.SetX(position.GetX() + distance.GetX());
                break;
            case Direction.Down:
                limit.SetY(position.GetY() - distance.GetY());
                break;
        }

        return limit;
    }
}