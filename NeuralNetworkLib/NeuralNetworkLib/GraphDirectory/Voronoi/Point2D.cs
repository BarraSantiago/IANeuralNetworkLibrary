namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// A concrete 2D point type that implements IPoint2D.
/// </summary>
public struct Point2D : IPoint2D<Point2D>
{
    public double X { get; set; }
    public double Y { get; set; }

    public Point2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Point2D Add(Point2D other) => new Point2D(X + other.X, Y + other.Y);
    public Point2D Subtract(Point2D other) => new Point2D(X - other.X, Y - other.Y);
    public Point2D Multiply(double scalar) => new Point2D(X * scalar, Y * scalar);
    public Point2D Divide(double scalar) => new Point2D(X / scalar, Y / scalar);
    public double DistanceTo(Point2D other) =>
        Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));
    public double Dot(Point2D other) => X * other.X + Y * other.Y;

    public bool Equals(Point2D other) => Math.Abs(X - other.X) < 1e-9 && Math.Abs(Y - other.Y) < 1e-9;
    public override bool Equals(object obj) => obj is Point2D other && Equals(other);
    public override int GetHashCode() => X.GetHashCode() ^ Y.GetHashCode();
    public override string ToString() => $"({X:F2}, {Y:F2})";
}
