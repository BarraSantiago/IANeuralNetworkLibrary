namespace VoronoiDiagram
{
    public class IntersectionPoint<TCoordinate> 
        where TCoordinate : IEquatable<TCoordinate>
    {
        private TCoordinate position;
        private float angle;

        public TCoordinate Position { get => position; }
        public float Angle { get => angle; set => angle = value; }

        public IntersectionPoint(TCoordinate position)
        {
            this.position = position;
            angle = 0f;
        }
    }
}