namespace NeuralNetworkLib.NeuralNetDirectory.NeuralNet
{
    public class Neuron
    {
        public readonly float bias;
        public readonly float[] weights;

        public Neuron(float weightsCount, float bias)
        {
            weights = new float[(int)weightsCount];

            Random random = new Random();
            for (int i = 0; i < weights.Length; i++) weights[i] = (float)(random.NextDouble() * 2.0 - 1.0);
            this.bias = bias;
        }
    }
}