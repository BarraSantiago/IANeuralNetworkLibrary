using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.Flocking
{
    public class Boid<TVector, TTransform> 
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<TVector>, new()
    {
        public float detectionRadious = 6.0f;
        public float alignmentOffset;
        public float cohesionOffset;
        public float separationOffset;
        public float directionOffset;
        public IVector target = new MyVector();
        public TTransform transform = new TTransform();

        private Func<Boid<TVector, TTransform>, TVector> alignment;
        private Func<Boid<TVector, TTransform>, TVector> cohesion;
        private Func<Boid<TVector, TTransform>, TVector> separation;
        private Func<Boid<TVector, TTransform>, TVector> direction;
        public List<ITransform<IVector>> NearBoids { get; set; }

        public void Init(Func<Boid<TVector, TTransform>, TVector> Alignment,
            Func<Boid<TVector, TTransform>, TVector> Cohesion,
            Func<Boid<TVector, TTransform>, TVector> Separation,
            Func<Boid<TVector, TTransform>, TVector> Direction)
        {
            this.alignment = Alignment;
            this.cohesion = Cohesion;
            this.separation = Separation;
            this.direction = Direction;
        }
        
        public IVector ACS()
        {
            IVector ACS = (alignment(this) * alignmentOffset) +
                          (cohesion(this) * cohesionOffset) +
                          (separation(this) * separationOffset) +
                          (direction(this) * directionOffset);
            return ACS.Normalized();
        }
        
        public TVector GetDirection()
        {
            return direction(this);
        }
        
        public TVector GetAlignment()
        {
            return alignment(this);
        }
        
        public TVector GetCohesion()
        {
            return cohesion(this);
        }
        
        public TVector GetSeparation()
        {
            return separation(this);
        }
    }
}