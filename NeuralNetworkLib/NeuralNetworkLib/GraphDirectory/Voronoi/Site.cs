namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// Represents a “site” (point of interest) that owns a Voronoi cell.
/// The cell polygon and the total weight of nodes in the cell are stored.
/// </summary>
public class Site<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public TPoint2D Position { get; set; }
    public List<TPoint2D> CellPolygon { get; set; } = new List<TPoint2D>();
    public double CellWeight { get; set; }

    public Site(TPoint2D position)
    {
        Position = position;
    }
}