namespace NeuralNetworkLib.GraphDirectory.Voronoi;

public class VoronoiDiagram<TPoint2D>
    where TPoint2D : IEquatable<TPoint2D>, IPoint2D<TPoint2D>, new()
{
    public List<Site<TPoint2D>> Sites { get; set; }
    public static List<Node<TPoint2D>> Nodes { get; set; }

    /// <summary>
    /// The bounding polygon in which the cells are computed.
    /// </summary>
    public List<TPoint2D> BoundingPolygon { get; set; }

    public VoronoiDiagram(List<Site<TPoint2D>> sites, List<TPoint2D> boundingPolygon)
    {
        Sites = sites;
        BoundingPolygon = boundingPolygon;
    }

    /// <summary>
    /// Computes the cell polygons using the standard power diagram approach.
    /// Each cell is computed by clipping the full bounding polygon with half–planes defined by the bisector between the site and every other site.
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
    /// Computes cell polygons with feedback adjustments so that cells move toward balancing node weights.
    /// In this version the extra feedback shift is expressed in distance units (with a maximum shift of 1 unit per bisector update).
    /// If (errors[site] - errors[other]) equals 1 (in node-weight units), then with feedbackCoefficient = 1 the bisector shifts by 1 unit.
    /// </summary>
    /// <param name="targetWeight">
    /// Desired node weight per cell (typically total node weight divided by the number of sites).
    /// </param>
    /// <param name="feedbackCoefficient">
    /// Coefficient scaling the feedback term. Set this to 1/typicalNodeWeight if you want an error difference of 1 to correspond to 1 unit of distance.
    /// </param>
    public void ComputeCellsWithFeedback(double targetWeight, double feedbackCoefficient, int iterations)
    {
        // First, compute the standard cells.
        //ComputeCellsStandard();

        for (int iter = 0; iter < iterations; iter++)
        {
            ComputeCellWeights();
            // Record the error (cellWeight - targetWeight) for each site.
            Dictionary<Site<TPoint2D>, double> errors = new Dictionary<Site<TPoint2D>, double>();
            foreach (var site in Sites)
            {
                errors[site] = site.CellWeight - targetWeight;
            }

            // Recompute each cell with a modified bisector that includes the feedback shift.
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
                    // Standard power diagram correction.
                    double baseCorrectionFactor = (other.CellWeight - site.CellWeight) / (2.0 * normSq);
                    TPoint2D baseCorrection = diff.Multiply(baseCorrectionFactor);
                    // Create a unit vector in the direction of diff.
                    double norm = Math.Sqrt(normSq);
                    TPoint2D u = diff.Divide(norm);
                    // Feedback shift: move by a distance proportional to the error difference.
                    double feedbackShift = feedbackCoefficient * (errors[site] - errors[other]);
                    // Clamp the feedback shift to ±1 unit.
                    feedbackShift = Math.Max(-3.0, Math.Min(3.0, feedbackShift));
                    TPoint2D feedbackCorrection = u.Multiply(feedbackShift);
                    // The new bisector point.
                    TPoint2D A = mid.Add(baseCorrection).Add(feedbackCorrection);

                    cell = ClipPolygonWithLine(cell, A, diff);
                    if (cell.Count == 0)
                        break;
                }

                site.CellPolygon = cell;
            }
        }
    }

    /// <summary>
    /// Computes the total node weight contained in each site's cell.
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
    /// Standard point-in-polygon test using the ray–casting algorithm.
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
    /// Clips a polygon with the half–plane defined by the line passing through boundaryPoint with normal boundaryNormal.
    /// Only points p satisfying (p - boundaryPoint)·boundaryNormal >= 0 are retained.
    /// Uses a Sutherland–Hodgman style algorithm.
    /// </summary>
    private List<TPoint2D> ClipPolygonWithLine(List<TPoint2D> polygon, TPoint2D boundaryPoint,
        TPoint2D boundaryNormal)
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
    private TPoint2D ComputeIntersection(TPoint2D start, TPoint2D end, TPoint2D boundaryPoint,
        TPoint2D boundaryNormal)
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