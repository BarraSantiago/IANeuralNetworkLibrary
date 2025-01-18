using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class TransformComponent : EcsComponent
{
    public ITransform<IVector> Transform = new ITransform<IVector>();
    public List<ITransform<IVector>> NearBoids;

}