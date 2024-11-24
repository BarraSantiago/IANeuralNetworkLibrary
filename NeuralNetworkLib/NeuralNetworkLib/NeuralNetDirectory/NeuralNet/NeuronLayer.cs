using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.NeuralNetDirectory.NeuralNet
{
    
    public class NeuronLayer
    {
        public BrainType BrainType;
        public SimAgentTypes AgentType;
        public float Bias {  get; set; }= 1;
        private readonly float p = 0.5f;
        public Neuron[] neurons;
        private float[] outputs;
        public float InputsCount { get; }
        public float NeuronsCount => neurons.Length;
        public float OutputsCount => outputs.Length;

        public NeuronLayer(float inputsCount, int neuronsCount, float bias, float p)
        {
            InputsCount = inputsCount;
            this.Bias = bias;
            this.p = p;

            SetNeuronsCount(neuronsCount);
        }


        private void SetNeuronsCount(int neuronsCount)
        {
            neurons = new Neuron[neuronsCount];

            for (int i = 0; i < neurons.Length; i++)
            {
                neurons[i] = new Neuron(InputsCount, Bias);
            }

            outputs = new float[neurons.Length];
        }
    }
}