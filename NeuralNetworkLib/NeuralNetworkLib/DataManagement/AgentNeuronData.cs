using System.Collections.Generic;

namespace NeuralNetworkLib.DataManagement
{
    public class AgentNeuronData
    {
        public uint AgentId { get; set; }
        public List<float[]> NeuronWeights { get; set; }
        public BrainType BrainType;
        public SimAgentTypes AgentType;
        private readonly float bias = 1;
        private readonly float p = 0.5f;
        private int totalWeights;
    }
}