using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class CohesionSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private IDictionary<uint, TransformComponent> transformComponents = null;
    private IDictionary<uint, ACSComponent> ACSComponents = null;
    private IEnumerable<uint> queriedEntities = null;

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
    }
    protected override void Execute(float deltaTime)
    {
        Parallel.ForEach(queriedEntities, parallelOptions, boidId =>
        {
            if (transformComponents[boidId].NearBoids.Count == 0) return;

            IVector avg = MyVector.zero();
            foreach (ITransform<IVector> b in transformComponents[boidId].NearBoids)
            {
                avg += b.position;
            }

            avg /= transformComponents[boidId].NearBoids.Count;
            MyVector average = avg - transformComponents[boidId].Transform.position;
            ACSComponents[boidId].Cohesion = EnsureValidVector(avg.Normalized());
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