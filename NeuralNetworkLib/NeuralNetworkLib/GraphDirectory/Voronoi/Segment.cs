using NeuralNetworkLib.Utils;

namespace Pathfinder.Voronoi
{
    public class Segment<TCoordinate, TCoordinateType> 
        where TCoordinate : ICoordinate<TCoordinateType>, new()
        where TCoordinateType : IEquatable<TCoordinateType>
    {
        private readonly TCoordinate direction;
        private readonly TCoordinate mediatrix;

        public TCoordinate Origin { get; }

        public TCoordinate Final { get; }

        public TCoordinate Mediatrix => mediatrix;
        public TCoordinate Direction => direction;
        public List<TCoordinate> Intersections { get; } = new();

        public Segment(TCoordinate origin, TCoordinate final)
        {
            this.Origin = origin;
            this.Final = final;

            // Mediatriz: la línea perpendicular que pasa por el punto medio:
            // 1. Sumamos la coordenada X del origen y final
            // 2. Dividimos para obtener la coordenada X de la mediatriz
            // 3. Repetimos para las coordenadas Y
            mediatrix = new TCoordinate();
            mediatrix.SetCoordinate((origin.GetX() + final.GetX()) / 2, (origin.GetY() + final.GetY()) / 2); 

            // Calculo la direccion del segmento:
            // 1. Calculo el vector perpendicular al vector que va del origen al final
            direction = new TCoordinate();
            direction.SetCoordinate(final.GetX() - origin.GetX(), final.GetY() - origin.GetY());
            direction.Perpendicular();
        }
    }
}