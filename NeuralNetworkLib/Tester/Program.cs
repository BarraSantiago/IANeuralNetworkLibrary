using System;
using System.Collections.Generic;
using System.Linq;

namespace VoronoiWeightBalancing
{
    /// <summary>
    /// A simple 2D point with basic vector operations.
    /// </summary>
    public struct Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

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
        public double DistanceTo(Point2D other) =>
            Math.Sqrt((X - other.X) * (X - other.X) + (Y - other.Y) * (Y - other.Y));

        // Dot product.
        public static double Dot(Point2D a, Point2D b) => a.X * b.X + a.Y * b.Y;

        public override string ToString() => $"({X:F2}, {Y:F2})";
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
    public class Site
    {
        public Point2D Position { get; set; }
        public List<Point2D> CellPolygon { get; set; } = new List<Point2D>();
        public double CellWeight { get; set; }

        public Site(Point2D pos)
        {
            Position = pos;
        }
    }

    /// <summary>
    /// Builds the Voronoi diagram and supports weight balancing.
    /// The diagram is computed over a bounding convex polygon (here a rectangle).
    /// </summary>
    public class VoronoiDiagram
    {
        public List<Site> Sites { get; set; }
        public List<Node> Nodes { get; set; }
        public List<Point2D> BoundingPolygon { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sites">List of sites (points of interest).</param>
        /// <param name="nodes">List of nodes (with weights) that lie in the region.</param>
        /// <param name="boundingPolygon">
        /// A convex polygon (e.g. a rectangle) that defines the zone.
        /// The polygon vertices should be given in order.
        /// </param>
        public VoronoiDiagram(List<Site> sites, List<Node> nodes, List<Point2D> boundingPolygon)
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
            foreach (Site site in Sites)
            {
                // Start with the whole bounding polygon.
                List<Point2D> cell = new List<Point2D>(BoundingPolygon);

                foreach (Site other in Sites)
                {
                    if (other == site) continue;

                    // For sites "site" and "other", the perpendicular bisector
                    // divides the plane into two half–planes. We want to keep points p
                    // that are closer to "site" than "other." One way to express this is:
                    //   (p - midpoint) · (site - other) >= 0
                    // where midpoint = (site.Position + other.Position) / 2.
                    Point2D midpoint = (site.Position + other.Position) / 2.0;
                    Point2D normal = site.Position - other.Position; // directs from "other" toward "site"

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
        private List<Point2D> ClipPolygonWithLine(List<Point2D> polygon, Point2D boundaryPoint, Point2D boundaryNormal)
        {
            List<Point2D> output = new List<Point2D>();

            if (polygon.Count == 0)
                return output;

            Point2D prev = polygon[polygon.Count - 1];
            bool prevInside = (Point2D.Dot(prev - boundaryPoint, boundaryNormal) >= 0);

            foreach (Point2D curr in polygon)
            {
                bool currInside = (Point2D.Dot(curr - boundaryPoint, boundaryNormal) >= 0);

                if (currInside)
                {
                    if (!prevInside)
                    {
                        // Edge goes from outside to inside: add intersection.
                        Point2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
                        output.Add(intersection);
                    }
                    output.Add(curr);
                }
                else if (prevInside)
                {
                    // Edge goes from inside to outside: add intersection.
                    Point2D intersection = LineIntersection(prev, curr, boundaryPoint, boundaryNormal);
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
        private Point2D LineIntersection(Point2D A, Point2D B, Point2D boundaryPoint, Point2D boundaryNormal)
        {
            Point2D AB = B - A;
            double t = Point2D.Dot(boundaryPoint - A, boundaryNormal) / Point2D.Dot(AB, boundaryNormal);
            return A + AB * t;
        }

        /// <summary>
        /// For each site, computes the total weight from the Nodes that lie inside its cell polygon.
        /// </summary>
        public void ComputeCellWeights()
        {
            foreach (Site site in Sites)
            {
                double total = 0.0;
                foreach (Node node in Nodes)
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
        public bool PointInPolygon(Point2D p, List<Point2D> polygon)
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
                foreach (Site site in Sites)
                {
                    // Find all nodes that fall inside the cell.
                    List<Node> nodesInCell = Nodes.Where(n => PointInPolygon(n.Position, site.CellPolygon)).ToList();
                    if (nodesInCell.Count == 0)
                        continue; // No nodes in cell? Skip adjustment.

                    // Compute the weighted centroid.
                    double sumWeights = nodesInCell.Sum(n => n.Weight);
                    Point2D centroid = new Point2D(0, 0);
                    foreach (Node node in nodesInCell)
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
    }

    /// <summary>
    /// Example program: creates a bounding rectangle, several sites (points of interest) and randomly distributed nodes.
    /// Then computes the Voronoi diagram and iteratively balances the weights.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            // Define the bounding rectangle.
            double width = 200.0, height = 200.0;
            List<Point2D> boundingPolygon = new List<Point2D>
            {
                new Point2D(0,0),
                new Point2D(width,0),
                new Point2D(width, height),
                new Point2D(0, height)
            };

            // Define sites (points of interest). You can add as many as you need.
            List<Site> sites = new List<Site>
            {
                new Site(new Point2D(20, 30)),
                new Site(new Point2D(50, 50)),
                new Site(new Point2D(80, 20)),
                new Site(new Point2D(70, 80))
            };

            // Generate a number of nodes randomly throughout the zone.
            // In this example every node has weight 1.
            Random rnd = new Random();
            List<Node> nodes = new List<Node>();
            int numNodes = 1000;
            for (int i = 0; i < numNodes; i++)
            {
                double x = rnd.NextDouble() * width;
                double y = rnd.NextDouble() * height;
                nodes.Add(new Node(new Point2D(x, y), 1.0));
            }

            // Create the Voronoi diagram.
            VoronoiDiagram vd = new VoronoiDiagram(sites, nodes, boundingPolygon);

            // Compute initial cells and cell weights.
            vd.ComputeCells();
            vd.ComputeCellWeights();

            Console.WriteLine("Initial cell weights:");
            foreach (Site site in sites)
            {
                Console.WriteLine($"Site at {site.Position} has weight {site.CellWeight:F2}");
            }

            // Perform weight balancing (e.g. 20 iterations, with a step size of 0.2).
            vd.BalanceWeights(iterations: 20, step: 0.2);

            Console.WriteLine("\nAfter weight balancing:");
            foreach (Site site in sites)
            {
                Console.WriteLine($"Site at {site.Position} has weight {site.CellWeight:F2}");
            }

            // (Optional) Print cell polygon vertices for each site.
            for (int i = 0; i < sites.Count; i++)
            {
                Console.WriteLine($"\nCell polygon for site {i} at {sites[i].Position}:");
                foreach (Point2D p in sites[i].CellPolygon)
                {
                    Console.WriteLine(p);
                }
            }

            Console.WriteLine("\nPress any key to exit.");
            Console.ReadKey();
        }
    }
}
