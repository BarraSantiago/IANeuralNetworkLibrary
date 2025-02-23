using System.Collections.Concurrent;
using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class DirectionSystem : ECSSystem
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
    
        entityData = queriedEntities.Select(id => (transform: transformComponents[id], acs: ACSComponents[id])).ToList();
    }
    
    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(entityData, parallelOptions, data =>
        {
            if (data.transform.NearBoids.Count == 0) return;
    
            IVector avgDirection = MyVector.zero();
            foreach (ITransform<IVector> neighbor in data.transform.NearBoids)
            {
                if (neighbor?.position == null) continue;
                avgDirection += neighbor.position - data.transform.Transform.position;
            }
    
            avgDirection /= data.transform.NearBoids.Count;
                
            data.acs.Direction = EnsureValidVector(avgDirection.Normalized());
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