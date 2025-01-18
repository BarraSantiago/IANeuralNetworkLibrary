using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class ACSComponent : EcsComponent
{
    public IVector Direction;
    public IVector Separation;
    public IVector Cohesion;
    public IVector Alignment;
    public IVector ACS;
}