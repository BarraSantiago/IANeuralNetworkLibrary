using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.GraphDirectory.Voronoi;

public class Voronoi<TCoordinate, TCoordinateType>
    where TCoordinate : IEquatable<TCoordinate>, ICoordinate<TCoordinateType>, new()
    where TCoordinateType : IEquatable<TCoordinateType>, new()
{
    private readonly List<Limit<TCoordinate, TCoordinateType>> limits = new();
    private readonly List<Sector<TCoordinate, TCoordinateType>> sectors = new();
    private static List<TCoordinate> _allNodes = new();
    private List<SimNode<TCoordinate>> _nodesInSector = new();
    private TCoordinate _origin = new TCoordinate();
    private TCoordinate _mapSize = new TCoordinate();
    private float _cellSize;
    private int targetCapacity;

    public void Init(TCoordinate origin, TCoordinate mapSize, float cellSize, List<TCoordinate> allNodes)
    {
        _origin.SetCoordinate(origin.GetCoordinate());
        _mapSize.SetCoordinate(mapSize.GetCoordinate());
        _cellSize = cellSize;
        _allNodes = allNodes;
        InitLimits();
    }

    private void InitLimits()
    {
        // Calculo los limites del mapa con sus dimensiones, distancia entre nodos y punto de origen
        TCoordinate mapSize = new TCoordinate();
        mapSize.SetCoordinate(_mapSize.GetCoordinate());
        mapSize.Multiply(_cellSize);
        TCoordinate offset = new TCoordinate();
        offset.SetCoordinate(_origin.GetCoordinate());


        TCoordinate coordinateUp = new TCoordinate();
        coordinateUp.SetCoordinate(0, mapSize.GetY());
        coordinateUp.Add(offset.GetCoordinate());
        limits.Add(new Limit<TCoordinate, TCoordinateType>(coordinateUp, Direction.Up));

        TCoordinate coordinateDown = new TCoordinate();
        coordinateDown.SetCoordinate(mapSize.GetX(), 0f);
        coordinateDown.Add(offset.GetCoordinate());
        limits.Add(new Limit<TCoordinate, TCoordinateType>(coordinateDown, Direction.Down));

        TCoordinate coordinateRight = new TCoordinate();
        coordinateRight.SetCoordinate(mapSize.GetX(), mapSize.GetY());
        coordinateRight.Add(offset.GetCoordinate());
        limits.Add(new Limit<TCoordinate, TCoordinateType>(coordinateRight, Direction.Right));

        TCoordinate coordinateLeft = new TCoordinate();
        coordinateLeft.SetCoordinate(0, 0);
        coordinateLeft.Add(offset.GetCoordinate());
        limits.Add(new Limit<TCoordinate, TCoordinateType>(coordinateLeft, Direction.Left));
    }

    public void SetVoronoi(List<TCoordinate> pointsOfInterest)
    {
        sectors.Clear();
        if (pointsOfInterest.Count <= 0) return;

        Parallel.ForEach(pointsOfInterest, point =>
        {
            SimNode<TCoordinateType> node = new SimNode<TCoordinateType>();
            node.SetCoordinate(point.GetCoordinate());
            Sector<TCoordinate, TCoordinateType> sector = new Sector<TCoordinate, TCoordinateType>(node)
            {
                MapDimensions = _mapSize
            };
            lock (sectors)
            {
                sectors.Add(sector);
            }
        });

        Parallel.ForEach(sectors, sector => { sector.AddSegmentLimits(limits); });

        Parallel.For(0, pointsOfInterest.Count, i =>
        {
            for (int j = 0; j < pointsOfInterest.Count; j++)
            {
                if (i == j) continue;
                // TODO fix this
                sectors[i].AddSegment(pointsOfInterest[i], pointsOfInterest[j]);
            }
        });

        // TODO fix this
        foreach (Sector<TCoordinate, TCoordinateType> sector in sectors)
        {
            sector.SetIntersections();
        }

        //BalanceSectorsByWeight();
    }

    private void BalanceSectorsByWeight()
    {
        for (int i = 0; i < 7; i++)
        {
            bool balanced = true;
            foreach (Sector<TCoordinate, TCoordinateType>? sector in sectors)
            {
                int totalWeight = sector.CalculateTotalWeight(GetAllNodes());
                List<Sector<TCoordinate, TCoordinateType>> neighbors = GetNeighboringSectors(sector);

                foreach (Sector<TCoordinate, TCoordinateType>? neighbor in neighbors)
                {
                    int neighborWeight = neighbor.CalculateTotalWeight(GetAllNodes());
                    if (totalWeight <= neighborWeight) continue;

                    balanced = false;
                    sector.AdjustSectorByWeight(neighbor, GetAllNodes());
                }
            }

            if (balanced) break;
        }
    }

    private List<Sector<TCoordinate, TCoordinateType>> GetNeighboringSectors(
        Sector<TCoordinate, TCoordinateType> sector)
    {
        List<Sector<TCoordinate, TCoordinateType>> neighbors = new List<Sector<TCoordinate, TCoordinateType>>();

        foreach (Sector<TCoordinate, TCoordinateType>? otherSector in sectors)
        {
            if (otherSector == sector) continue;

            if (AreSectorsNeighbors(sector, otherSector))
            {
                neighbors.Add(otherSector);
            }
        }

        return neighbors;
    }

    private bool AreSectorsNeighbors(Sector<TCoordinate, TCoordinateType> sector1,
        Sector<TCoordinate, TCoordinateType> sector2)
    {
        foreach (TCoordinate point1 in sector1.PointsToDraw())
        {
            foreach (TCoordinate point2 in sector2.PointsToDraw())
            {
                if (Approximately(point1.GetX(), point2.GetX()) && Approximately(point1.GetY(), point2.GetY()))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private List<SimNode<TCoordinate>> GetAllNodes()
    {
        if (_nodesInSector != null) return _nodesInSector;
        _nodesInSector = new List<SimNode<TCoordinate>>();

        foreach (Sector<TCoordinate, TCoordinateType>? sector in sectors)
        {
            foreach (TCoordinate? node in _allNodes)
            {
                if (!sector.CheckPointInSector(node)) continue;
                SimNode<TCoordinate> newNode = new SimNode<TCoordinate>(node);
                _nodesInSector.Add(newNode);
            }
        }

        return _nodesInSector;
    }

    public SimNode<TCoordinateType> GetClosestPointOfInterest(TCoordinate agentPosition)
    {
        // Calculo que mina esta mas cerca a x position
        return sectors != null
            ? (from sector in sectors
                where sector.CheckPointInSector(agentPosition)
                select sector.PointOfInterest).FirstOrDefault()
            : null;
    }

    public List<Sector<TCoordinate, TCoordinateType>> SectorsToDraw()
    {
        return sectors;
    }

    private bool Approximately(float a, float b, float tolerance = 0.0001f)
    {
        return Math.Abs(a - b) < tolerance;
    }
}