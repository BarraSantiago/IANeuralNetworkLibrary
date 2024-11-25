namespace NeuralNetworkLib.Utils
{
    public class ITransform<TVector>
        where TVector : IVector, IEquatable<TVector>
    {
        public ITransform (TVector position)
        {
            this.position = position;
        }
        public TVector position { get; set; }
        public TVector forward;

        public ITransform()
        {
            position = default(TVector);
            forward = default(TVector);
        }
    }
}