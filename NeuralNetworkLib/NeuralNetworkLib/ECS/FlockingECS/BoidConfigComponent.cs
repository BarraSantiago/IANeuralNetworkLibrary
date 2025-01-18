using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class BoidConfigComponent : EcsComponent
{
    public float detectionRadious = 6.0f;
    public float alignmentOffset;
    public float cohesionOffset;
    public float separationOffset;
    public float directionOffset;
}