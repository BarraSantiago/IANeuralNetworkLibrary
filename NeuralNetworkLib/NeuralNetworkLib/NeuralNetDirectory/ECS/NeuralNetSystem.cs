using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;

namespace NeuralNetworkLib.NeuralNetDirectory.ECS
{
    public sealed class NeuralNetSystem : ECSSystem
    {
        private ParallelOptions parallelOptions;
        private IDictionary<uint, NeuralNetComponent> neuralNetworkComponents;
        private IDictionary<uint, OutputComponent> outputComponents;
        private IDictionary<uint, InputComponent> inputComponents;
        private IEnumerable<uint> queriedEntities;

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
        }

        protected override void PreExecute(float deltaTime)
        {
            neuralNetworkComponents ??= ECSManager.GetComponents<NeuralNetComponent>();
            outputComponents ??= ECSManager.GetComponents<OutputComponent>();
            inputComponents ??= ECSManager.GetComponents<InputComponent>();
            queriedEntities ??= ECSManager.GetEntitiesWithComponentTypes(
                typeof(NeuralNetComponent), typeof(OutputComponent), typeof(InputComponent));
        }

        protected override void Execute(float deltaTime)
        {
            const int MaxBrains = 3;
            Parallel.ForEach(queriedEntities, parallelOptions, entityId =>
            {
                Parallel.For(0, MaxBrains, i =>
                {
                    NeuralNetComponent neuralNetwork = neuralNetworkComponents[entityId];
                    float[][] inputs = inputComponents[entityId].inputs;
                    float[] outputs = new float[3];
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