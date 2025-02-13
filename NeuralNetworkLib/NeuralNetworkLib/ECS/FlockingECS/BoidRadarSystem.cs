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
    private List<(uint id, IVector position, ITransform<IVector> transform)> boidData;
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
        boidData = queriedEntities
            .Select(id => (id, transformComponents[id].Transform.position, transformComponents[id].Transform)).ToList();
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(queriedEntities, parallelOptions, boidId =>
        {
            BoidConfigComponent? boidConfig = boidConfigComponents[boidId];
            float detectionRadiusSquared = boidConfig.detectionRadius * boidConfig.detectionRadius;
            IVector boidPosition = transformComponents[boidId].Transform.position;

            List<ITransform<IVector>> nearBoids = new List<ITransform<IVector>>();
            foreach ((uint nearId, IVector? nearPos, ITransform<IVector> nearTransform) in boidData)
            {
                if (boidId == nearId || nearPos == null) continue;

                float distanceSquared = IVector.DistanceSquared(boidPosition, nearPos);
                if (distanceSquared <= detectionRadiusSquared)
                    nearBoids.Add(nearTransform);
            }

            transformComponents[boidId].NearBoids = nearBoids;
        });
    }

    protected override void PostExecute(float deltaTime)
    {
    }
}