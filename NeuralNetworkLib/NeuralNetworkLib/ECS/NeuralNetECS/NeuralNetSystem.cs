using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.ECS.NeuralNetECS;

public sealed class NeuralNetSystem : ECSSystem
{
    private ParallelOptions parallelOptions;
    private IDictionary<uint, NeuralNetComponent> neuralNetworkComponents = null;
    private IDictionary<uint, OutputComponent> outputComponents = null;
    private IDictionary<uint, InputComponent> inputComponents = null;
    private IDictionary<uint, BrainAmountComponent> brainAmountComponents = null;
    private IEnumerable<uint> queriedEntities = null;

    public override void Initialize()
    {
        parallelOptions = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = Environment.ProcessorCount 
        };
    }

    public override void Deinitialize()
    {
        neuralNetworkComponents = null;
        outputComponents = null;
        inputComponents = null;
        queriedEntities = null;
        brainAmountComponents = null;
    }

    protected override void PreExecute(float deltaTime)
    {
        neuralNetworkComponents ??= ECSManager.GetComponents<NeuralNetComponent>();
        outputComponents ??= ECSManager.GetComponents<OutputComponent>();
        inputComponents ??= ECSManager.GetComponents<InputComponent>();
        brainAmountComponents ??= ECSManager.GetComponents<BrainAmountComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(
            typeof(NeuralNetComponent), typeof(OutputComponent), typeof(InputComponent));
    }

    protected override void Execute(float deltaTime)
    {
        OrderablePartitioner<uint>? partitioner = Partitioner.Create(queriedEntities, EnumerablePartitionerOptions.NoBuffering);
            
        Parallel.ForEach(partitioner, parallelOptions, entityId =>
        {
            NeuralNetComponent? neuralNet = neuralNetworkComponents[entityId];
            OutputComponent? outputComp = outputComponents[entityId];
            float[][] inputsArray = inputComponents[entityId].inputs;
            int brainAmount = brainAmountComponents[entityId].BrainAmount;

            for (int i = 0; i < brainAmount; i++)
            {
                if(inputsArray[i] == null) return;
                if (i >= neuralNet.Layers.Length || i >= inputsArray.Length) continue;

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
            }
        });
    }

    private float[] Synapsis(NeuronLayer layer, float[] inputs)
    {
        int neuronCount = (int)layer.NeuronsCount;
        float[] outputs = new float[neuronCount];
        int totalOperations = neuronCount * inputs.Length;

        bool useParallel = totalOperations > 10_000; // Tune based on your workload

        if (useParallel)
        {
            Parallel.For(0, neuronCount, parallelOptions, j =>
            {
                ComputeNeuronOutput(layer.neurons[j], inputs, outputs, j);
            });
        }
        else
        {
            for (int j = 0; j < neuronCount; j++)
            {
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ComputeWeightedSum(Neuron neuron, ReadOnlySpan<float> inputs)
    {
        float sum = 0f;
        int inputLength = inputs.Length;
        ReadOnlySpan<float> weights = neuron.weights;

        if (Vector.IsHardwareAccelerated && inputLength >= Vector<float>.Count)
        {
            Vector<float> sumVector = Vector<float>.Zero;
            int vectorSize = Vector<float>.Count;
            int i = 0;

            for (; i <= inputLength - vectorSize; i += vectorSize)
            {
                Vector<float> weightVec = new Vector<float>(weights.Slice(i).ToArray());
                Vector<float> inputVec = new Vector<float>(inputs.Slice(i).ToArray());
                sumVector += weightVec * inputVec;
            }

            sum = Vector.Dot(sumVector, Vector<float>.One);

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

    protected override void PostExecute(float deltaTime) { }
}
