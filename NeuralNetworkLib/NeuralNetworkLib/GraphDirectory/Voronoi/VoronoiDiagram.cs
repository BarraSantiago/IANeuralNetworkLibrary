namespace NeuralNetworkLib.GraphDirectory.Voronoi;

/// <summary>
/// A generic Voronoi (power) diagram.
/// The cell for each site is computed by clipping the bounding polygon with
/// half–planes defined by the weighted (power) distance.
/// Weight balancing is achieved by adjusting each site's PowerWeight.
/// </summary>
public class VoronoiDiagram<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public List<Site<TPoint2D>> Sites { get; set; }
    public List<Node<TPoint2D>> Nodes { get; set; }

    /// <summary>
    /// The bounding polygon (typically convex, e.g. a rectangle) in which the cells are computed.
    /// </summary>
    public List<TPoint2D> BoundingPolygon { get; set; }

    public VoronoiDiagram(List<Site<TPoint2D>> sites, List<Node<TPoint2D>> nodes, List<TPoint2D> boundingPolygon)
    {
        Sites = sites;
        Nodes = nodes;
        BoundingPolygon = boundingPolygon;
    }

    /// <summary>
    /// Computes the cell (as a polygon) for each site using the power diagram definition.
    /// For each pair of sites, the half–plane is defined by:
    ///    ||p - s_i||^2 - w_i <= ||p - s_j||^2 - w_j,
    /// which can be rewritten as (p - A) · (s_i - s_j) >= 0, with A computed below.
    /// </summary>
    public void ComputeCells()
    {
        foreach (var site in Sites)
        {
            // Start with the full bounding polygon.
            List<TPoint2D> cell = new List<TPoint2D>(BoundingPolygon);
            foreach (var other in Sites)
            {
                if (other == site)
                    continue;

                // Compute the vector from other to this site.
                TPoint2D diff = site.Position.Subtract(other.Position);
                double normSq = diff.Dot(diff);
                if (normSq < 1e-9)
                    continue; // avoid division by zero for coincident sites

                // Standard (unweighted) bisector would use the midpoint:
                TPoint2D mid = site.Position.Add(other.Position).Divide(2.0);

                // With power weights, we shift the bisector.
                // Let the half–plane be defined by (p - A) · (site.Position - other.Position) >= 0.
                // Choose A = mid + correction, with:
                //    correction = diff * ((other.PowerWeight - site.PowerWeight) / (2 * normSq)).
                double correctionFactor = (other.PowerWeight - site.PowerWeight) / (2.0 * normSq);
                TPoint2D correction = diff.Multiply(correctionFactor);
                TPoint2D A = mid.Add(correction);

                // Use diff as the normal.
                // The half–plane for site is: (p - A) · diff >= 0.
                cell = ClipPolygonWithLine(cell, A, diff);
                if (cell.Count == 0)
                    break;
            }

            site.CellPolygon = cell;
        }
    }

    /// <summary>
    /// Clips a polygon with a half–plane defined by a boundary point and a boundary normal.
    /// Points p satisfying (p - boundaryPoint) · boundaryNormal >= 0 are kept.
    /// Uses the Sutherland–Hodgman algorithm.
    /// </summary>
    private List<TPoint2D> ClipPolygonWithLine(List<TPoint2D> polygon, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
    {
        List<TPoint2D> output = new List<TPoint2D>();
        if (polygon.Count == 0)
            return output;

        TPoint2D prev = polygon[polygon.Count - 1];
        bool prevInside = (prev.Subtract(boundaryPoint)).Dot(boundaryNormal) >= 0;
        foreach (var curr in polygon)
        {
            bool currInside = (curr.Subtract(boundaryPoint)).Dot(boundaryNormal) >= 0;
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
    /// Computes the intersection between the segment (from A to B) and the line defined by:
    /// (p - boundaryPoint) · boundaryNormal = 0.
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
            // Check if p is between the y-coordinates of the edge, and then use the x-coordinate.
            if (((pi.Y > p.Y) != (pj.Y > p.Y)) &&
                (p.X < (pj.X - pi.X) * (p.Y - pi.Y) / (pj.Y - pi.Y) + pi.X))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    /// <summary>
    /// Computes the total node weight for each site's cell.
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
    /// Returns the site (point of interest) whose cell contains the given point.
    /// If no cell contains the point, returns the site with the closest Position.
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

    /// <summary>
    /// Adjusts each site's PowerWeight (without modifying its fixed Position) so that its cell's total node weight
    /// approaches the target weight. This is done iteratively. Increasing a site's PowerWeight will shrink its cell,
    /// and vice versa.
    /// </summary>
    /// <param name="iterations">Number of balancing iterations.</param>
    /// <param name="step">
    /// The adjustment factor for the PowerWeight update.
    /// A typical value might be 0.1.
    /// </param>
    public void BalanceWeights(int iterations, double step = 0.1)
    {
        // Compute target node weight per cell.
        double totalNodeWeight = Nodes.Sum(n => n.Weight);
        double targetWeight = totalNodeWeight / Sites.Count;

        for (int iter = 0; iter < iterations; iter++)
        {
            // Recompute cells and their node weights.
            ComputeCells();
            ComputeCellWeights();

            // Adjust the power weight for each site based on the difference.
            foreach (var site in Sites)
            {
                double diff = site.CellWeight - targetWeight;
                // If cell weight is above target, increase PowerWeight to shrink the cell;
                // if below target, decrease PowerWeight to expand the cell.
                site.PowerWeight += step * diff;
            }
        }

        // Final update.
        ComputeCells();
        ComputeCellWeights();
    }
}