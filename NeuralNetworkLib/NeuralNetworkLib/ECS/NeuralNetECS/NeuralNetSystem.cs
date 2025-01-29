using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.ECS.NeuralNetECS
{
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
            neuralNetworkComponents ??= ECSManager.GetComponents<NeuralNetComponent>();
            outputComponents ??= ECSManager.GetComponents<OutputComponent>();
            inputComponents ??= ECSManager.GetComponents<InputComponent>();
            queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(
                typeof(NeuralNetComponent), typeof(OutputComponent), typeof(InputComponent));
            brainAmountComponents ??= ECSManager.GetComponents<BrainAmountComponent>();
        }

        protected override void Execute(float deltaTime)
        {
            Parallel.ForEach(queriedEntities, parallelOptions, entityId =>
            {
                int brainAmount = brainAmountComponents[entityId].BrainAmount;
                Parallel.For(0, brainAmount, i =>
                {
                    NeuralNetComponent neuralNetwork = neuralNetworkComponents[entityId];
                    float[][] inputs = inputComponents[entityId].inputs;
                    float[] outputs = new float[3];
                    
                    if (i >= neuralNetwork.Layers.Count || i >= inputs.Length)
                    {
                        return; // Skip if index is out of bounds
                    }
                    
                    for (int j = 0; j < neuralNetwork.Layers[i].Count; j++)
                    {
                        outputs = Synapsis(neuralNetwork.Layers[i][j], inputs[i]);
                        inputs[i] = outputs;
                    }

                    if ((int)neuralNetwork.Layers[i][^1].OutputsCount != outputs.Length) return;

                    outputComponents[entityId].Outputs[i] = outputs;
                });
            });
        }

        protected override void PostExecute(float deltaTime)
        {
        }

        private float[] Synapsis(NeuronLayer layer, float[] inputs)
        {
            float[] outputs = new float[(int)layer.NeuronsCount];
            Parallel.For(0, (int)layer.NeuronsCount, parallelOptions, j =>
            {
                float a = 0;
                for (int i = 0; i < inputs.Length; i++)
                {
                    a += layer.neurons[j].weights[i] * inputs[i];
                }
                a += layer.neurons[j].bias;
                outputs[j] = (float)Math.Tanh(a);
            });
            return outputs;
        }
    }
}