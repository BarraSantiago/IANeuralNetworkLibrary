using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class ACSSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private IDictionary<uint, BoidConfigComponent> boidConfigComponents = null;
    private IDictionary<uint, TransformComponent> transformComponents = null;
    private IDictionary<uint, ACSComponent> ACSComponents = null;
    private IEnumerable<uint> queriedEntities = null;
    private List<(BoidConfigComponent config, ACSComponent acs)> entityData;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    public override void Deinitialize()
    {
        boidConfigComponents = null;
        queriedEntities = null;
        transformComponents = null;
        ACSComponents = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        boidConfigComponents ??= ECSManager.GetComponents<BoidConfigComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(BoidConfigComponent),
            typeof(TransformComponent), typeof(ACSComponent));
        transformComponents ??= ECSManager.GetComponents<TransformComponent>();
        ACSComponents ??= ECSManager.GetComponents<ACSComponent>();
        entityData = queriedEntities.Select(id => (boidConfigComponents[id], ACSComponents[id])).ToList();
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(entityData, parallelOptions, data =>
        {
            data.acs.ACS = EnsureValidVector(((data.acs.Alignment * data.config.alignmentOffset) +
                                             (data.acs.Cohesion * data.config.cohesionOffset) +
                                             (data.acs.Separation * data.config.separationOffset) +
                                             (data.acs.Direction * data.config.directionOffset)).Normalized());
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