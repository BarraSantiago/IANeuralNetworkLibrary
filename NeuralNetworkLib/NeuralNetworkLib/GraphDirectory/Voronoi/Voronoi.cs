using NeuralNetworkLib.Utils;

namespace Pathfinder.Voronoi
{
    public class Voronoi<TCoordinate, TCoordinateType>
        where TCoordinate : IEquatable<TCoordinate>, ICoordinate<TCoordinateType>, new()
        where TCoordinateType : IEquatable<TCoordinateType>, new()
    {
        private readonly List<Limit<TCoordinate, TCoordinateType>> limits = new();
        private readonly List<Sector<TCoordinate,TCoordinateType>> sectors = new();
        private TCoordinate _origin = new TCoordinate();
        private TCoordinate _mapSize = new TCoordinate();
        private float _cellSize;
        
        public void Init(TCoordinate origin, TCoordinate mapSize, float cellSize)
        {
            _origin.SetCoordinate(origin.GetCoordinate());
            _mapSize.SetCoordinate(mapSize.GetCoordinate());
            _cellSize = cellSize;
            
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

            foreach (TCoordinate? mine in pointsOfInterest)
            {
                // Agrego los nodos como sectores
                SimNode<TCoordinateType> node = new SimNode<TCoordinateType>();
                node.SetCoordinate(mine.GetCoordinate());
                sectors.Add(new Sector<TCoordinate, TCoordinateType>(node));
                sectors[^1].MapDimensions = _mapSize;
            }

            foreach (Sector<TCoordinate, TCoordinateType>? sector in sectors)
            {
                // Agrego los limites a cada sector
                sector.AddSegmentLimits(limits);
            }

            for (int i = 0; i < pointsOfInterest.Count; i++)
            {
                for (int j = 0; j < pointsOfInterest.Count; j++)
                {
                    // Agrego los segmentos entre cada sector (menos entre si mismo)
                    if (i == j) continue;
                    sectors[i].AddSegment(pointsOfInterest[i], pointsOfInterest[j]);
                }
            }

            foreach (Sector<TCoordinate, TCoordinateType>? sector in sectors)
            {
                // Calculo las intersecciones
                sector.SetIntersections();
            }
        }

        public SimNode<TCoordinateType> GetClosestPointOfInterest(TCoordinate agentPosition)
        {
            // Calculo que mina esta mas cerca a x position
            return sectors != null ? (from sector in sectors 
                where sector.CheckPointInSector(agentPosition) select sector.Mine).FirstOrDefault() : null;
        }

        public List<Sector<TCoordinate,TCoordinateType>> SectorsToDraw()
        {
            return sectors;
        }
    }
}