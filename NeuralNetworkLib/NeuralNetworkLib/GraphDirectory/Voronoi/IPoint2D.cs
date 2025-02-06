namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// An interface representing a 2D point. This uses the “curiously recurring template pattern”
/// so that the arithmetic methods can return the concrete type.
/// </summary>
/// <typeparam name="TPoint2D">The concrete type implementing this interface.</typeparam>
public interface IPoint2D<TPoint2D> : IEquatable<TPoint2D>
    where TPoint2D : IPoint2D<TPoint2D>
{
    double X { get; set; }
    double Y { get; set; }

    TPoint2D Add(TPoint2D other);
    TPoint2D Subtract(TPoint2D other);
    TPoint2D Multiply(double scalar);
    TPoint2D Divide(double scalar);
    double DistanceTo(TPoint2D other);
    double Dot(TPoint2D other);
}
