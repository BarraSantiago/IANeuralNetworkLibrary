using System.Numerics;
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
        parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = 32 };
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
        // Cache component dictionaries and the queried entities.
        neuralNetworkComponents ??= ECSManager.GetComponents<NeuralNetComponent>();
        outputComponents ??= ECSManager.GetComponents<OutputComponent>();
        inputComponents ??= ECSManager.GetComponents<InputComponent>();
        brainAmountComponents ??= ECSManager.GetComponents<BrainAmountComponent>();
        queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(
            typeof(NeuralNetComponent), typeof(OutputComponent), typeof(InputComponent));
    }

    protected override void Execute(float deltaTime)
    {
        // Parallelize over the entities; each entity will process all of its brains in a simple for-loop.
        Parallel.ForEach(queriedEntities, parallelOptions, entityId =>
        {
            // Cache per-entity components to avoid repeated dictionary lookups.
            NeuralNetComponent neuralNet = neuralNetworkComponents[entityId];
            OutputComponent outputComp = outputComponents[entityId];
            // Assume each entity has a 2D array of inputs (one per brain).
            float[][] inputsArray = inputComponents[entityId].inputs;
            int brainAmount = brainAmountComponents[entityId].BrainAmount;

            // Process each brain sequentially (if brainAmount is small, this is more efficient than nesting parallel loops)
            for (int i = 0; i < brainAmount; i++)
            {
                // Validate that the brain index is in range.
                if (i >= neuralNet.Layers.Count || i >= inputsArray.Length)
                    continue;

                float[] currentInputs = inputsArray[i];
                List<NeuronLayer> layers = neuralNet.Layers[i];
                int layerCount = layers.Count;

                // Feed forward through all layers.
                for (int j = 0; j < layerCount; j++)
                {
                    currentInputs = Synapsis(layers[j], currentInputs);
                }

                // Only assign the output if the final layer produced the expected number of outputs.
                if (layerCount > 0 && layers[layerCount - 1].OutputsCount == currentInputs.Length)
                {
                    outputComp.Outputs[i] = currentInputs;
                }
            }
        });
    }

    protected override void PostExecute(float deltaTime)
    {
        // No post-execution logic needed here.
    }

    private float[] Synapsis(NeuronLayer layer, float[] inputs)
    {
        int neuronCount = (int)layer.NeuronsCount;
        int inputLength = inputs.Length;
        float[] outputs = new float[neuronCount];

        // Threshold below which parallel overhead may outweigh benefits.
        const int ParallelThreshold = 30;

        // Use SIMD if hardware accelerated and there is a reasonable vector length.
        bool useSIMD = Vector.IsHardwareAccelerated && inputLength >= Vector<float>.Count;
        int simdSize = useSIMD ? Vector<float>.Count : 1;

        // Local function to compute dot product using SIMD if possible.
        float DotProduct(float[] weights)
        {
            float sum = 0f;
            if (useSIMD)
            {
                int i = 0;
                Vector<float> vecSum = Vector<float>.Zero;
                // Process chunks of simdSize.
                for (; i <= inputLength - simdSize; i += simdSize)
                {
                    Vector<float> vecInput = new Vector<float>(inputs, i);
                    Vector<float> vecWeights = new Vector<float>(weights, i);
                    vecSum += vecInput * vecWeights;
                }

                for (int j = 0; j < simdSize; j++)
                {
                    sum += vecSum[j];
                }

                // Process remaining elements.
                for (int iRem = inputLength - (inputLength % simdSize); iRem < inputLength; iRem++)
                {
                    sum += weights[iRem] * inputs[iRem];
                }
            }
            else
            {
                for (int i = 0; i < inputLength; i++)
                {
                    sum += weights[i] * inputs[i];
                }
            }

            return sum;
        }

        // Define an action to process one neuron.
        Action<int> processNeuron = j =>
        {
            Neuron neuron = layer.neurons[j];
            float sum = DotProduct(neuron.weights);
            sum += neuron.bias;
            outputs[j] = (float)Math.Tanh(sum);
        };

        if (neuronCount < ParallelThreshold)
        {
            for (int j = 0; j < neuronCount; j++)
            {
                processNeuron(j);
            }
        }
        else
        {
            Parallel.For(0, neuronCount, parallelOptions, j => { processNeuron(j); });
        }


        return outputs;
    }
}