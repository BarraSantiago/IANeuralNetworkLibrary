namespace VoronoiWeightBalancing
{
    public interface IPoint2D : IEquatable<IPoint2D>
    {
        public double X { get; set; }
        public double Y { get; set; }
        
        public IPoint2D Sum(IPoint2D other);
        public IPoint2D Minuns(IPoint2D other);
        public IPoint2D Multiply(double scalar);
        public IPoint2D Divide(double scalar);
        public double DistanceTo(IPoint2D other);
        public static double Dot(IPoint2D a, IPoint2D b)
        {
            return a.X * b.X + a.Y * b.Y;
        }
        public double Dot(IPoint2D other)
        {
            return X * other.X + Y * other.Y;
        }
    }

    /// <summary>
    /// A simple 2D point with basic vector operations.
    /// </summary>
    public class Point2D : IPoint2D
    {

        public Point2D(double x, double y) { X = x; Y = y; }

        // Overloaded operators.
        public static Point2D operator +(Point2D a, Point2D b) =>
            new Point2D(a.X + b.X, a.Y + b.Y);
        public static Point2D operator -(Point2D a, Point2D b) =>
            new Point2D(a.X - b.X, a.Y - b.Y);
        public static Point2D operator *(Point2D a, double scalar) =>
            new Point2D(a.X * scalar, a.Y * scalar);
        public static Point2D operator /(Point2D a, double scalar) =>
            new Point2D(a.X / scalar, a.Y / scalar);

        // Euclidean distance.
        public double X { get; set; }
        public double Y { get; set; }
        public IPoint2D Sum(IPoint2D other)
        {
            return new Point2D(X+other.X, Y+other.Y);
        }

        public IPoint2D Minuns(IPoint2D other)
        {
            return new Point2D(X-other.X, Y-other.Y);
        }

        public IPoint2D Multiply(double scalar)
        {
            return new Point2D(X*scalar, Y*scalar);
        }

        public IPoint2D Divide(double scalar)
        {
            return new Point2D(X/scalar, Y/scalar);
        }

        public double DistanceTo(IPoint2D other) =>
            Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

        // Dot product.
        public static double Dot(Point2D a, Point2D b) => a.X * b.X + a.Y * b.Y;

        public override string ToString() => $"({X:F2}, {Y:F2})";

        protected bool Equals(Point2D other)
        {
            return Approximately(X,other.X) && Approximately(Y, other.Y);
        }
        private bool Approximately(double a, double b)
        {
            return Math.Abs(a - b) < 1e-6f;
        }

        public bool Equals(IPoint2D other)
        {
            return Approximately(X,other.X) && Approximately(Y, other.Y);
         }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Point2D)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }

    /// <summary>
    /// Represents a “node” (e.g. a population point, a cost unit, etc.) that has a position and an associated weight.
    /// </summary>
    public class Node
    {
        public Point2D Position { get; set; }
        public double Weight { get; set; }

        public Node(Point2D pos, double weight)
        {
            Position = pos;
            Weight = weight;
        }
    }

    /// <summary>
    /// Represents a site (point of interest) that will “own” a Voronoi cell.
    /// After computing the diagram, CellPolygon holds the list of vertices for the cell,
    /// and CellWeight holds the sum of the weights of nodes that fall inside the cell.
    /// </summary>
    public class Site<TPoint2D>
        where TPoint2D : IEquatable<TPoint2D>, IPoint2D
    {
        public TPoint2D Position { get; set; }
        public List<TPoint2D> CellPolygon { get; set; } = new List<TPoint2D>();
        public double CellWeight { get; set; }

        public Site(TPoint2D pos)
        {
            Position = pos;
        }
    }

    /// <summary>
    /// Builds the Voronoi diagram and supports weight balancing.
    /// The diagram is computed over a bounding convex polygon (here a rectangle).
    /// </summary>
    public class VoronoiDiagram <TPoint2D> 
    where TPoint2D : IPoint2D, IEquatable<TPoint2D>, new()
    {
        public List<Site<TPoint2D>> Sites { get; set; }
        public List<Node> Nodes { get; set; }
        public List<TPoint2D> BoundingPolygon { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sites">List of sites (points of interest).</param>
        /// <param name="nodes">List of nodes (with weights) that lie in the region.</param>
        /// <param name="boundingPolygon">
        /// A convex polygon (e.g. a rectangle) that defines the zone.
        /// The polygon vertices should be given in order.
        /// </param>
        public VoronoiDiagram(List<Site<TPoint2D>> sites, List<Node> nodes, List<TPoint2D> boundingPolygon)
        {
            Sites = sites;
            Nodes = nodes;
            BoundingPolygon = boundingPolygon;
        }

        /// <summary>
        /// Computes the Voronoi cell (as a polygon) for each site.
        /// Each cell is obtained by starting with the entire bounding polygon and clipping it
        /// by the half–plane associated with every other site.
        /// </summary>
        public void ComputeCells()
        {
            foreach (var site in Sites)
            {
                // Start with the whole bounding polygon.
                List<TPoint2D> cell = new List<TPoint2D>(BoundingPolygon);

                foreach (var other in Sites)
                {
                    if (other == site) continue;

                    // For sites "site" and "other", the perpendicular bisector
                    // divides the plane into two half–planes. We want to keep points p
                    // that are closer to "site" than "other." One way to express this is:
                    //   (p - midpoint) · (site - other) >= 0
                    // where midpoint = (site.Position + other.Position) / 2.
                    TPoint2D midpoint = (site.Position.Sum(other.Position)).Divide(2.0);
                    TPoint2D normal = site.Position - other.Position; // directs from "other" toward "site"

                    // Clip the current cell polygon with the half–plane defined by the line:
                    //   (p - midpoint) · normal = 0
                    // keeping points that satisfy (p - midpoint) · normal >= 0.
                    cell = ClipPolygonWithLine(cell, midpoint, normal);

                    // If the cell becomes empty, stop processing.
                    if (cell.Count == 0) break;
                }

                site.CellPolygon = cell;
            }
        }

        /// <summary>
        /// Clips a convex polygon with a half–plane.
        /// The half–plane is defined by a line passing through "boundaryPoint" with outward normal "boundaryNormal".
        /// Points p satisfying (p - boundaryPoint) · boundaryNormal >= 0 are kept.
        /// Uses the Sutherland–Hodgman algorithm.
        /// </summary>
        private List<TPoint2D> ClipPolygonWithLine(List<TPoint2D> polygon, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
        {
            List<TPoint2D> output = new List<TPoint2D>();

            if (polygon.Count == 0)
                return output;

            TPoint2D prev = polygon[polygon.Count - 1];
            bool prevInside = (boundaryNormal.Dot(prev - boundaryPoint) >= 0);

            foreach (var curr in polygon)
            {
                bool currInside = (boundaryNormal.Dot(curr - boundaryPoint) >= 0);

                if (currInside)
                {
                    if (!prevInside)
                    {
                        // Edge goes from outside to inside: add intersection.
                        TPoint2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
                        output.Add(intersection);
                    }
                    output.Add(curr);
                }
                else if (prevInside)
                {
                    // Edge goes from inside to outside: add intersection.
                    TPoint2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
                    output.Add(intersection);
                }

                prev = curr;
                prevInside = currInside;
            }
            return output;
        }

        /// <summary>
        /// Computes the intersection between a segment (from A to B) and a line defined by:
        ///   (p - boundaryPoint) · boundaryNormal = 0.
        /// Assumes that A and B are not both parallel to the boundary.
        /// </summary>
        private TPoint2D LineIntersection(TPoint2D A, TPoint2D B, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
        {
            TPoint2D AB = B - A;
            double t = TPoint2D.Dot(boundaryPoint - A, boundaryNormal) / TPoint2D.Dot(AB, boundaryNormal);
            return A + AB * t;
        }

        /// <summary>
        /// For each site, computes the total weight from the Nodes that lie inside its cell polygon.
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
        /// A standard ray–casting algorithm to decide whether point p is inside a polygon.
        /// </summary>
        public bool PointInPolygon(TPoint2D p, List<TPoint2D> polygon)
        {
            bool inside = false;
            int n = polygon.Count;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((polygon[i].Y > p.Y) != (polygon[j].Y > p.Y)) &&
                    (p.X < (polygon[j].X - polygon[i].X) * (p.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        /// <summary>
        /// Performs iterative weight balancing.
        /// In each iteration the Voronoi cells are recomputed and then each site is moved
        /// toward the weighted centroid (of the nodes in its cell) by a fraction "step".
        /// </summary>
        /// <param name="iterations">Number of iterations to run.</param>
        /// <param name="step">
        /// Step size (0 &lt; step &lt; 1). For example, step = 0.2 means the site moves 20% of the way toward the weighted centroid.
        /// </param>
        public void BalanceWeights(int iterations, double step = 0.1)
        {
            // (Optional) Compute the target weight per cell (if desired).
            double totalWeight = Nodes.Sum(n => n.Weight);
            double targetWeight = totalWeight / Sites.Count;

            for (int iter = 0; iter < iterations; iter++)
            {
                // Recompute the cells based on current site positions.
                ComputeCells();
                ComputeCellWeights();

                // Update each site’s position.
                foreach (var site in Sites)
                {
                    // Find all nodes that fall inside the cell.
                    var nodesInCell = Nodes.Where(n => PointInPolygon(n.Position, site.CellPolygon)).ToList();
                    if (nodesInCell.Count == 0)
                        continue; // No nodes in cell? Skip adjustment.

                    // Compute the weighted centroid.
                    double sumWeights = nodesInCell.Sum(n => n.Weight);
                    TPoint2D centroid = new TPoint2D(0, 0);
                    foreach (var node in nodesInCell)
                    {
                        centroid += node.Position * node.Weight;
                    }
                    centroid = centroid / sumWeights;

                    // Update site position: move a fraction "step" toward the weighted centroid.
                    site.Position = site.Position * (1 - step) + centroid * step;
                }
            }

            // Final update.
            ComputeCells();
            ComputeCellWeights();
        }
        
        /// <summary>
        /// Returns the site (point of interest) whose Voronoi cell contains the given point.
        /// If no cell contains the point (for example, if the point lies outside all computed cells),
        /// the function returns the site that is closest (by Euclidean distance) to the point.
        /// </summary>
        /// <param name="agentPosition">The 2D point for which to determine the corresponding site.</param>
        /// <returns>The Site whose cell contains the point, or the closest site if none contain it.</returns>
        public Site<TPoint2D> GetClosestPointOfInterest(TPoint2D agentPosition)
        {
            // First, check if the point is inside any site's cell polygon.
            foreach (var site in Sites)
            {
                // PointInPolygon is assumed to be a helper method that determines if agentPosition is inside the polygon.
                if (PointInPolygon(agentPosition, site.CellPolygon))
                {
                    return site;
                }
            }
    
            // If the point is not inside any cell (or if the cells are not computed), 
            // return the site that is closest to the agentPosition by Euclidean distance.
            return Sites.OrderBy(site => site.Position.DistanceTo(agentPosition)).FirstOrDefault();
        }

        
        public List<Site<TPoint2D>> GetSectors()
        {
            return Sites;
        }
    }
}
