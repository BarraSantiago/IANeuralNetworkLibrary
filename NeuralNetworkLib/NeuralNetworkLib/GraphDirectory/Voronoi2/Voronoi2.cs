using System;
using System.Collections.Generic;
using System.Linq;

namespace GenericVoronoi
{
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

    /// <summary>
    /// A generic Voronoi diagram. The cells are computed by iteratively “clipping” the bounding polygon
    /// with the half–planes determined by the perpendicular bisectors between sites.
    /// This class also provides weight balancing and a query method to get the site that “owns” a given point.
    /// </summary>
    public class VoronoiDiagram<TPoint2D>
        where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
    {
        public List<Site<TPoint2D>> Sites { get; set; }
        public List<Node<TPoint2D>> Nodes { get; set; }
        public List<TPoint2D> BoundingPolygon { get; set; }

        public VoronoiDiagram(List<Site<TPoint2D>> sites, List<Node<TPoint2D>> nodes, List<TPoint2D> boundingPolygon)
        {
            Sites = sites;
            Nodes = nodes;
            BoundingPolygon = boundingPolygon;
        }

        /// <summary>
        /// Computes the Voronoi cell for each site by clipping the bounding polygon with the half–planes
        /// determined by every other site.
        /// </summary>
        public void ComputeCells()
        {
            foreach (var site in Sites)
            {
                // Start with the entire bounding polygon.
                List<TPoint2D> cell = new List<TPoint2D>(BoundingPolygon);
                foreach (var other in Sites)
                {
                    if (other == site) continue;

                    // The perpendicular bisector between site and other is defined by:
                    //   (p - midpoint) · (site.Position - other.Position) >= 0
                    TPoint2D mid = site.Position.Add(other.Position).Divide(2.0);
                    TPoint2D normal = site.Position.Subtract(other.Position);
                    cell = ClipPolygonWithLine(cell, mid, normal);

                    if (cell.Count == 0)
                        break;
                }
                site.CellPolygon = cell;
            }
        }

        /// <summary>
        /// Clips a polygon with a half–plane defined by a boundary point and a boundary normal.
        /// Points p satisfying (p - boundaryPoint) · boundaryNormal >= 0 are kept.
        /// </summary>
        private List<TPoint2D> ClipPolygonWithLine(List<TPoint2D> polygon, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
        {
            List<TPoint2D> output = new List<TPoint2D>();
            if (polygon.Count == 0)
                return output;

            TPoint2D prev = polygon[polygon.Count - 1];
            bool prevInside = (prev.Subtract(boundaryPoint).Dot(boundaryNormal) >= 0);
            foreach (var curr in polygon)
            {
                bool currInside = (curr.Subtract(boundaryPoint).Dot(boundaryNormal) >= 0);
                if (currInside)
                {
                    if (!prevInside)
                    {
                        TPoint2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
                        output.Add(intersection);
                    }
                    output.Add(curr);
                }
                else if (prevInside)
                {
                    TPoint2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
                    output.Add(intersection);
                }
                prev = curr;
                prevInside = currInside;
            }
            return output;
        }

        /// <summary>
        /// Computes the intersection between a segment (from A to B) and the line defined by (p - boundaryPoint) · boundaryNormal = 0.
        /// </summary>
        private TPoint2D LineIntersection(TPoint2D A, TPoint2D B, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
        {
            TPoint2D AB = B.Subtract(A);
            double t = (boundaryPoint.Subtract(A)).Dot(boundaryNormal) / AB.Dot(boundaryNormal);
            return A.Add(AB.Multiply(t));
        }

        /// <summary>
        /// Determines whether point p is inside the polygon (using a ray–casting algorithm).
        /// </summary>
        public bool PointInPolygon(TPoint2D p, List<TPoint2D> polygon)
        {
            bool inside = false;
            int n = polygon.Count;
            if (n == 0) return false;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                TPoint2D pi = polygon[i];
                TPoint2D pj = polygon[j];
                if (((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                    (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Computes the total weight (sum of node weights) for each site’s cell.
        /// </summary>
        public void ComputeCellWeights()
        {
            foreach (var site in Sites)
            {
                double total = 0.0;
                foreach (var node in Nodes)
                {
                    if (PointInPolygon(node.Position, site.CellPolygon))
                        total += node.Weight;
                }
                site.CellWeight = total;
            }
        }

        /// <summary>
        /// Iteratively re–computes the cells and then moves each site toward the weighted centroid
        /// of the nodes within its cell. The parameter “step” determines how far (fractionally)
        /// each site moves in one iteration.
        /// </summary>
        public void BalanceWeights(int iterations, double step = 0.1)
        {
            for (int iter = 0; iter < iterations; iter++)
            {
                ComputeCells();
                ComputeCellWeights();

                foreach (var site in Sites)
                {
                    var nodesInCell = Nodes.Where(n => PointInPolygon(n.Position, site.CellPolygon)).ToList();
                    if (nodesInCell.Count == 0)
                        continue;

                    double sumWeights = nodesInCell.Sum(n => n.Weight);

                    // Start with a new (zero) instance for the weighted centroid.
                    TPoint2D centroid = new TPoint2D();
                    centroid.X = 0;
                    centroid.Y = 0;

                    foreach (var node in nodesInCell)
                    {
                        // Multiply the node’s position by its weight and add it.
                        centroid = centroid.Add(node.Position.Multiply(node.Weight));
                    }
                    centroid = centroid.Divide(sumWeights);

                    // Move the site’s position a fraction “step” toward the weighted centroid.
                    site.Position = site.Position.Multiply(1 - step).Add(centroid.Multiply(step));
                }
            }
            ComputeCells();
            ComputeCellWeights();
        }

        /// <summary>
        /// Returns the site (point of interest) whose cell contains the given point.
        /// If none of the cells contain the point, the site with the closest position is returned.
        /// </summary>
        public Site<TPoint2D> GetClosestPointOfInterest(TPoint2D agentPosition)
        {
            foreach (var site in Sites)
            {
                if (PointInPolygon(agentPosition, site.CellPolygon))
                    return site;
            }
            return Sites.OrderBy(site => site.Position.DistanceTo(agentPosition)).FirstOrDefault();
        }
    }
}
