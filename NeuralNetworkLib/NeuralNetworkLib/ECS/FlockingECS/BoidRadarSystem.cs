using System.Collections.Concurrent;
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
        boidConfigComponents ??= ECSManager.GetComponents<BoidConfigComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(BoidConfigComponent),
            typeof(TransformComponent), typeof(ACSComponent));
        transformComponents ??= ECSManager.GetComponents<TransformComponent>();
        ACSComponents ??= ECSManager.GetComponents<ACSComponent>();
    }

    protected override void Execute(float deltaTime)
    {
        if (boidConfigComponents == null || transformComponents == null)
        {
            return;
        }

        Parallel.ForEach(queriedEntities, parallelOptions, boidId =>
        {
            ConcurrentBag<ITransform<IVector>> boidsInsideRadius = new();

            float detectionRadiusSquared = boidConfigComponents[boidId].detectionRadious *
                                           boidConfigComponents[boidId].detectionRadious;

            IVector boidPosition = transformComponents[boidId].Transform.position;

            foreach (var nearBoidId in queriedEntities)
            {
                if (transformComponents[nearBoidId].Transform.position == null || boidId == nearBoidId)
                {
                    continue;
                }

                IVector nearBoidPosition = transformComponents[nearBoidId].Transform.position;
                float distanceSquared = IVector.DistanceSquared(boidPosition, nearBoidPosition);

                if (distanceSquared <= detectionRadiusSquared)
                {
                    boidsInsideRadius.Add(transformComponents[nearBoidId].Transform);
                }
            }

            transformComponents[boidId].NearBoids = boidsInsideRadius.ToList();
        });
    }

    protected override void PostExecute(float deltaTime)
    {
    }
}