using System.Collections.Concurrent;
using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class CohesionSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private List<(TransformComponent transform, ACSComponent acs)> entityData;
    private IEnumerable<uint> queriedEntities;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    public override void Deinitialize()
    {
        queriedEntities = null;
        entityData = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(ACSComponent), typeof(TransformComponent));
        ConcurrentDictionary<uint, TransformComponent> transformComponents = ECSManager.GetComponents<TransformComponent>();
        ConcurrentDictionary<uint, ACSComponent> ACSComponents = ECSManager.GetComponents<ACSComponent>();
        
        entityData = queriedEntities
            .Select(id => (transform: transformComponents[id], acs: ACSComponents[id]))
            .ToList();
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(entityData, parallelOptions, data =>
        {
            if (data.transform.NearBoids.Count == 0) return;

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector> b in data.transform.NearBoids)
            {
                avg += b.position;
            }

            avg /= data.transform.NearBoids.Count;
            IVector direction = avg - data.transform.Transform.position; // Corrected logic
            data.acs.Cohesion = EnsureValidVector(direction.Normalized());
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