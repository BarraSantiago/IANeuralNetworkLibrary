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
        // Calculo de la distancia al limite:
        // 1. Calculo la distancia entre "position" y el origen del límite
        // 2. Tomo el valor absoluto para asegurarme de tener una distancia positiva
        // 3. Multiplico esta distancia por 2 para extender el límite más allá de la distancia original
        TCoordinate distance = new TCoordinate();
        distance.SetCoordinate(Math.Abs(position.GetX() - origin.GetX()) * 2f, Math.Abs(position.GetY() - origin.GetY()) * 2f);
        TCoordinate limit = new TCoordinate();
        limit.SetCoordinate(position.GetCoordinate());

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