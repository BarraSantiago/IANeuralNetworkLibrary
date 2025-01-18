using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class ACSSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private IDictionary<uint, BoidConfigComponent> boidConfigComponents = null;
    private IDictionary<uint, TransformComponent> transformComponents = null;
    private IDictionary<uint, ACSComponent> ACSComponents = null;
    private IEnumerable<uint> queriedEntities = null;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    public override void Deinitialize()
    {
        boidConfigComponents = null;
        queriedEntities = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        boidConfigComponents ??= ECSManager.GetComponents<BoidConfigComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(BoidConfigComponent),
            typeof(TransformComponent), typeof(ACSComponent));
        transformComponents ??= ECSManager.GetComponents<TransformComponent>();
        ACSComponents ??= ECSManager.GetComponents<ACSComponent>();
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(queriedEntities, parallelOptions, boidId =>
        {
            IVector ACS = (ACSComponents[boidId].Alignment * boidConfigComponents[boidId].alignmentOffset) +
                          (ACSComponents[boidId].Cohesion * boidConfigComponents[boidId].cohesionOffset) +
                          (ACSComponents[boidId].Separation * boidConfigComponents[boidId].separationOffset) +
                          (ACSComponents[boidId].Direction * boidConfigComponents[boidId].directionOffset);
            ACSComponents[boidId].ACS = EnsureValidVector(ACS.Normalized());
        });
    }

    protected override void PostExecute(float deltaTime)
    {
    }

    private IVector EnsureValidVector(IVector vector)
    {
        if (vector == null || float.IsNaN(vector.X) || float.IsNaN(vector.Y))
        {
            return MyVector.zero();
        }

        return vector;
    }
}