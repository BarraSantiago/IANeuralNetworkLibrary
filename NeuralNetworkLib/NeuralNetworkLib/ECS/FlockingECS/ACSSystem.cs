using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class ACSSystem : ECSSystem
{
    private (BoidConfigComponent config, ACSComponent acs)[] entityDataArray;
    private ParallelOptions parallelOptions;
    private int entityCount;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
    }

    public override void Deinitialize()
    {
        entityDataArray = null;
    }


    protected override void PreExecute(float deltaTime)
    {
        (uint[] ids, BoidConfigComponent[] configs) = ECSManager.GetComponentsDirect<BoidConfigComponent>();
        (_, ACSComponent[] acsComponents) = ECSManager.GetComponentsDirect<ACSComponent>();

        entityCount = ids.Length;
        if (entityDataArray == null || entityDataArray.Length < entityCount)
            entityDataArray = new (BoidConfigComponent, ACSComponent)[entityCount];

        for (int i = 0; i < entityCount; i++)
        {
            entityDataArray[i] = (configs[i], acsComponents[i]);
        }
    }

    protected override void Execute(float deltaTime)
    {
        Parallel.For(0, entityCount, parallelOptions, i =>
        {
            (BoidConfigComponent config, ACSComponent acs) = entityDataArray[i];
            IVector ACS = (acs.Alignment * config.alignmentOffset) +
                          (acs.Cohesion * config.cohesionOffset) +
                          (acs.Separation * config.separationOffset) +
                          (acs.Direction * config.directionOffset);
            acs.ACS = EnsureValidVector(ACS.Normalized());
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