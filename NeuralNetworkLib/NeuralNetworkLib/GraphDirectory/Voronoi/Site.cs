namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// A site (point of interest) that “owns” a cell.
/// Its Position is fixed.
/// The cell polygon is computed via a power diagram (using PowerWeight),
/// and CellWeight is the sum of the node weights inside the cell.
/// </summary>
public class Site<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public TPoint2D Position { get; set; }
    public List<TPoint2D> CellPolygon { get; set; } = new List<TPoint2D>();
    public double CellWeight { get; set; }
    /// <summary>
    /// The balancing parameter used in the power diagram.
    /// Adjusted during balancing to modify the cell polygon.
    /// </summary>
    public double PowerWeight { get; set; } = 0.0;

    public Site(TPoint2D position)
    {
        Position = position;
    }
}