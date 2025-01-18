using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class BoidRadarSystem : ECSSystem
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
        transformComponents = null;
        ACSComponents = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        boidConfigComponents ??= ECSManager.GetEntitiesWhitFlagTypes(new []{FlagType.Cart});
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(BoidConfigComponent),
            typeof(TransformComponent), typeof(ACSComponent));
        transformComponents ??= ECSManager.GetComponents<TransformComponent>();
        ACSComponents ??= ECSManager.GetComponents<ACSComponent>();
    }

    protected override void Execute(float deltaTime)
    {
        List<ITransform<IVector>> insideRadiusBoids = new();
        float detectionRadiusSquared = boid.detectionRadious * boid.detectionRadious;
        IVector boidPosition = boid.transform.position;

        // TODO Fix boid search
        /*Parallel.ForEach(Scavengers.Values, scavenger =>
        {
            if (scavenger?.Transform.position == null || boid == scavenger.boid)
            {
                return;
            }

            IVector scavengerPosition = scavenger.Transform.position;
            float distanceSquared = IVector.DistanceSquared(boidPosition, scavengerPosition);

            if (distanceSquared > detectionRadiusSquared) return;
            lock (insideRadiusBoids)
            {
                insideRadiusBoids.Add(scavenger.boid.transform);
            }
        });*/

        return insideRadiusBoids;
    }

    protected override void PostExecute(float deltaTime)
    {
        throw new NotImplementedException();
    }
}