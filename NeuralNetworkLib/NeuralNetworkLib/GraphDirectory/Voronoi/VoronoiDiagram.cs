namespace NeuralNetworkLib.GraphDirectory.Voronoi;

public class VoronoiDiagram<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public List<Site<TPoint2D>> Sites { get; set; }
    public List<Node<TPoint2D>> Nodes { get; set; }

    /// <summary>
    /// The bounding polygon in which the cells are computed.
    /// </summary>
    public List<TPoint2D> BoundingPolygon { get; set; }

    public VoronoiDiagram(List<Site<TPoint2D>> sites, List<Node<TPoint2D>> nodes, List<TPoint2D> boundingPolygon)
    {
        Sites = sites;
        Nodes = nodes;
        BoundingPolygon = boundingPolygon;
    }

    /// <summary>
    /// Computes the cell polygons using the standard power diagram method.
    /// Each cell is computed by starting with the full bounding polygon and then
    /// clipping it with half–planes determined by the bisector between the site and every other site.
    /// </summary>
    public void ComputeCellsStandard()
    {
        foreach (Site<TPoint2D> site in Sites)
        {
            List<TPoint2D> cell = new List<TPoint2D>(BoundingPolygon);
            foreach (Site<TPoint2D> other in Sites)
            {
                if (other == site)
                    continue;

                TPoint2D diff = site.Position.Subtract(other.Position);
                double normSq = diff.Dot(diff);
                if (normSq < 1e-9)
                    continue;

                TPoint2D mid = site.Position.Add(other.Position).Divide(2.0);
                double correctionFactor = (other.PowerWeight - site.PowerWeight) / (2.0 * normSq);
                TPoint2D correction = diff.Multiply(correctionFactor);
                TPoint2D A = mid.Add(correction);
                cell = ClipPolygonWithLine(cell, A, diff);
                if (cell.Count == 0)
                    break;
            }

            site.CellPolygon = cell;
        }
    }

    /// <summary>
    /// Computes the cell polygons with a feedback mechanism.
    /// In each feedback iteration the error (cellWeight - targetWeight) is computed for each site.
    /// When clipping the candidate cell, the bisector is shifted by a term proportional to the difference
    /// in error between two sites. Sites with too heavy a cell will have their boundaries pushed inward.
    /// </summary>
    /// <param name="targetWeight">The desired weight for each cell (typically total node weight divided by number of sites).</param>
    /// <param name="feedbackCoefficient">
    /// A coefficient that scales the feedback term. For many problems a starting value of around 0.0001 is suggested.
    /// </param>
    public void ComputeCellsWithFeedback(double targetWeight, double feedbackCoefficient, int iterations)
    {
        // First, compute the standard cells.
        ComputeCellsStandard();

        // Run a few feedback iterations.
        for (int iter = 0; iter < iterations; iter++)
        {
            // Update each site's cell weight.
            ComputeCellWeights();

            // Compute error for each site.
            Dictionary<Site<TPoint2D>, double> errors = new Dictionary<Site<TPoint2D>, double>();
            foreach (var site in Sites)
            {
                double error = site.CellWeight - targetWeight;
                errors[site] = error;
            }

            // Recompute each cell using feedback adjustments.
            foreach (Site<TPoint2D> site in Sites)
            {
                List<TPoint2D> cell = new List<TPoint2D>(BoundingPolygon);
                foreach (Site<TPoint2D> other in Sites)
                {
                    if (other == site)
                        continue;

                    TPoint2D diff = site.Position.Subtract(other.Position);
                    double normSq = diff.Dot(diff);
                    if (normSq < 1e-9)
                        continue;

                    TPoint2D mid = site.Position.Add(other.Position).Divide(2.0);
                    double baseCorrection = (other.PowerWeight - site.PowerWeight) / (2.0 * normSq);
                    double errorDiff = errors[site] - errors[other];
                    double feedbackTerm = feedbackCoefficient * errorDiff / (2.0 * Math.Sqrt(normSq));
                    double totalCorrectionFactor = baseCorrection + feedbackTerm;

                    // Optionally clamp the correction factor to avoid extreme shifts.
                    double maxCorrectionFactor = 10.0;
                    totalCorrectionFactor = Math.Max(-maxCorrectionFactor,
                        Math.Min(maxCorrectionFactor, totalCorrectionFactor));

                    TPoint2D correction = diff.Multiply(totalCorrectionFactor);
                    TPoint2D A = mid.Add(correction);
                    cell = ClipPolygonWithLine(cell, A, diff);
                    if (cell.Count == 0)
                        break;
                }

                site.CellPolygon = cell;
            }
        }
    }

    /// <summary>
    /// Computes the node weight contained within each site's cell.
    /// </summary>
    public void ComputeCellWeights()
    {
        foreach (Site<TPoint2D> site in Sites)
        {
            double total = 0;
            foreach (Node<TPoint2D> node in Nodes)
            {
                if (PointInPolygon(node.Position, site.CellPolygon))
                    total += node.Weight;
            }

            site.CellWeight = total;
        }
    }

    /// <summary>
    /// Standard point-in-polygon test using the ray-casting algorithm.
    /// </summary>
    public bool PointInPolygon(TPoint2D p, List<TPoint2D> polygon)
    {
        bool inside = false;
        int count = polygon.Count;
        if (count < 3)
            return false;

        for (int i = 0, j = count - 1; i < count; j = i++)
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
    /// Clips a polygon with the half-plane defined by the line (through boundaryPoint with normal boundaryNormal).
    /// Only points for which (p - boundaryPoint)·boundaryNormal >= 0 are retained.
    /// Uses a Sutherland-Hodgman style algorithm.
    /// </summary>
    private List<TPoint2D> ClipPolygonWithLine(List<TPoint2D> polygon, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
    {
        List<TPoint2D> output = new List<TPoint2D>();
        if (polygon.Count == 0)
            return output;

        TPoint2D prev = polygon[polygon.Count - 1];
        bool prevInside = (prev.Subtract(boundaryPoint)).Dot(boundaryNormal) >= 0;

        foreach (TPoint2D curr in polygon)
        {
            bool currInside = (curr.Subtract(boundaryPoint)).Dot(boundaryNormal) >= 0;
            if (currInside)
            {
                if (!prevInside)
                {
                    TPoint2D intersection = ComputeIntersection(prev, curr, boundaryPoint, boundaryNormal);
                    output.Add(intersection);
                }

                output.Add(curr);
            }
            else if (prevInside)
            {
                TPoint2D intersection = ComputeIntersection(prev, curr, boundaryPoint, boundaryNormal);
                output.Add(intersection);
            }

            prev = curr;
            prevInside = currInside;
        }

        return output;
    }

    /// <summary>
    /// Computes the intersection point between a segment (start to end) and the line defined by boundaryPoint and boundaryNormal.
    /// </summary>
    private TPoint2D ComputeIntersection(TPoint2D start, TPoint2D end, TPoint2D boundaryPoint, TPoint2D boundaryNormal)
    {
        TPoint2D direction = end.Subtract(start);
        double t = (boundaryPoint.Subtract(start)).Dot(boundaryNormal) / direction.Dot(boundaryNormal);
        return start.Add(direction.Multiply(t));
    }

    /// <summary>
    /// Balances the cells by computing a target weight (total node weight divided by the number of sites)
    /// and then running the feedback-based cell computation.
    /// </summary>
    /// <param name="feedbackCoefficient">
    /// A coefficient that scales the feedback adjustment. A starting value of 0.0001 is suggested,
    /// but you may need to tune it based on your data.
    /// </param>
    public void BalanceCells(double feedbackCoefficient, int iterations)
    {
        double totalNodeWeight = Nodes.Sum(n => n.Weight);
        double targetWeight = totalNodeWeight / Sites.Count;
        ComputeCellsWithFeedback(targetWeight, feedbackCoefficient, iterations);
        // Final weight computation after rebalancing
        ComputeCellWeights();
    }

    public Site<TPoint2D> GetClosestPointOfInterest(TPoint2D agentPosition)
    {
        foreach (Site<TPoint2D>? site in Sites)
        {
            if (PointInPolygon(agentPosition, site.CellPolygon))
                return site;
        }

        return Sites.OrderBy(site => site.Position.DistanceTo(agentPosition)).FirstOrDefault();
    }
}