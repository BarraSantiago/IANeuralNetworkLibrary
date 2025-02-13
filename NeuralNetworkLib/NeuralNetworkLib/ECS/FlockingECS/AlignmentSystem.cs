using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class AlignmentSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private IDictionary<uint, TransformComponent> transformComponents = null;
    private IDictionary<uint, ACSComponent> ACSComponents = null;
    private IEnumerable<uint> queriedEntities = null;
    private List<(TransformComponent transform, ACSComponent acs)> entityData;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    public override void Deinitialize()
    {
        queriedEntities = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(typeof(ACSComponent), typeof(TransformComponent));
        transformComponents ??= ECSManager.GetComponents<TransformComponent>();
        ACSComponents ??= ECSManager.GetComponents<ACSComponent>();
        entityData = queriedEntities.Select(id => (transformComponents[id], ACSComponents[id])).ToList();
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(entityData, parallelOptions, data =>
        {
            if (data.transform.NearBoids.Count == 0) return;

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector>? b in data.transform.NearBoids)
                avg += b.forward;

            avg /= data.transform.NearBoids.Count;
            data.acs.Alignment = EnsureValidVector(avg.Normalized());
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