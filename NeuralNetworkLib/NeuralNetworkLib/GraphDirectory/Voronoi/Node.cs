namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// Represents a weighted node (for example, a population or cost unit) at a given 2D position.
/// </summary>
public class Node<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public TPoint2D Position { get; set; }
    public double Weight { get; set; }

    public Node(TPoint2D position, double weight)
    {
        Position = position;
        Weight = weight;
    }
}
