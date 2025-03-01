using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.ECS.NeuralNetECS;

public sealed class NeuralNetSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private ConcurrentDictionary<uint, NeuralNetComponent> neuralNetworkComponents = null!;
    private ConcurrentDictionary<uint, OutputComponent> outputComponents = null!;
    private ConcurrentDictionary<uint, InputComponent> inputComponents = null!;
    private ConcurrentDictionary<uint, BrainAmountComponent> brainAmountComponents = null!;
    private ConcurrentBag<uint> queriedEntities = null!;

    // Cache the Vector<float>.One value.
    private static readonly Vector<float> VectorOne = new Vector<float>(1f);

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };
    }

    public override void Deinitialize()
    {
        neuralNetworkComponents = null!;
        outputComponents = null!;
        inputComponents = null!;
        brainAmountComponents = null!;
        queriedEntities = null!;
    }

    protected override void PreExecute(float deltaTime)
    {
        neuralNetworkComponents = new ConcurrentDictionary<uint, NeuralNetComponent>(
            ECSManager.GetComponents<NeuralNetComponent>());
        outputComponents = new ConcurrentDictionary<uint, OutputComponent>(
            ECSManager.GetComponents<OutputComponent>());
        inputComponents = new ConcurrentDictionary<uint, InputComponent>(
            ECSManager.GetComponents<InputComponent>());
        brainAmountComponents = new ConcurrentDictionary<uint, BrainAmountComponent>(
            ECSManager.GetComponents<BrainAmountComponent>());

        IEnumerable<uint> entities = ECSManager.GetEntitiesWithComponentTypes(
            typeof(NeuralNetComponent), typeof(OutputComponent), typeof(InputComponent));
        queriedEntities = new ConcurrentBag<uint>(entities);
    }

    protected override void Execute(float deltaTime)
    {
        // Use a partitioner over the ConcurrentBag’s underlying collection.
        OrderablePartitioner<uint>? partitioner = Partitioner.Create(queriedEntities, EnumerablePartitionerOptions.NoBuffering);

        Parallel.ForEach(partitioner, parallelOptions, entityId =>
        {
            // Lookups in ConcurrentDictionary are thread-safe.
            if (!neuralNetworkComponents.TryGetValue(entityId, out NeuralNetComponent? neuralNet) ||
                !outputComponents.TryGetValue(entityId, out OutputComponent? outputComp) ||
                !inputComponents.TryGetValue(entityId, out InputComponent? inputComp) ||
                !brainAmountComponents.TryGetValue(entityId, out BrainAmountComponent? brainComp))
            {
                return;
            }

            float[][] inputsArray = inputComp.inputs;
            int brainAmount = brainComp.BrainAmount;

            Parallel.For(0, brainAmount, parallelOptions, i =>
            {
                if (i >= inputsArray.Length || inputsArray[i] == null)
                    return;
                if (i >= neuralNet.Layers.Length)
                    return;

                float[] currentInputs = inputsArray[i];
                NeuronLayer[] layers = neuralNet.Layers[i];

                for (int j = 0; j < layers.Length; j++)
                {
                    currentInputs = Synapsis(layers[j], currentInputs);
                }

                if (layers.Length > 0 && layers[^1].OutputsCount == currentInputs.Length)
                {
                    outputComp.Outputs[i] = currentInputs;
                }
            });
        });
    }

    private float[] Synapsis(NeuronLayer layer, float[] inputs)
    {
        int neuronCount = (int)layer.NeuronsCount;
        float[] outputs = new float[neuronCount];
        int totalOperations = neuronCount * inputs.Length;

        bool useParallel = totalOperations > 1000; // Tune based on your workload

        if (useParallel)
        {
            Parallel.For(0, neuronCount, parallelOptions, j =>
            {
                if (inputs.Length != layer.neurons[j].weights.Length)
                {
                    return;
                }
                ComputeNeuronOutput(layer.neurons[j], inputs, outputs, j);
            });
        }
        else
        {
            for (int j = 0; j < neuronCount; j++)
            {
                if (inputs.Length != layer.neurons[j].weights.Length)
                {
                    continue;
                }
                ComputeNeuronOutput(layer.neurons[j], inputs, outputs, j);
            }
        }

        return outputs;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeNeuronOutput(Neuron neuron, float[] inputs, float[] outputs, int index)
    {
        float sum = ComputeWeightedSum(neuron, inputs);
        outputs[index] = FastTanh(sum);
    }

    // Use a thread-local buffer to assist with vectorized operations.
    private static readonly ThreadLocal<float[]> vectorBuffer = new(() => new float[Vector<float>.Count]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ComputeWeightedSum(Neuron neuron, ReadOnlySpan<float> inputs)
    {
        float sum = 0f;
        int inputLength = inputs.Length;
        ReadOnlySpan<float> weights = neuron.weights;

        if (inputLength != weights.Length)
        {
            return -1;
        }
        
        if (Vector.IsHardwareAccelerated && inputLength >= Vector<float>.Count)
        {
            Vector<float> sumVector = Vector<float>.Zero;
            int vectorSize = Vector<float>.Count;
            int i = 0;
            float[] buffer = vectorBuffer.Value;

            for (; i <= inputLength - vectorSize; i += vectorSize)
            {
                // Use Span.CopyTo without extra allocations.
                weights.Slice(i, vectorSize).CopyTo(buffer);
                Vector<float> weightVec = new(buffer);
                inputs.Slice(i, vectorSize).CopyTo(buffer);
                Vector<float> inputVec = new(buffer);
                sumVector += weightVec * inputVec;
            }

            sum = Vector.Dot(sumVector, VectorOne);

            for (; i < inputLength; i++)
            {
                sum += weights[i] * inputs[i];
            }
        }
        else
        {
            for (int i = 0; i < inputLength; i++)
            {
                sum += weights[i] * inputs[i];
            }
        }

        return sum + neuron.bias;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float FastTanh(float x)
    {
        // 5th-order rational approximation accurate to ±0.00005
        float x2 = x * x;
        float p = x * (135135.0f + x2 * (17325.0f + x2 * 378.0f));
        float q = 135135.0f + x2 * (62370.0f + x2 * (3150.0f + x2 * 28.0f));
        return p / q;
    }

    protected override void PostExecute(float deltaTime)
    {
    }
}
