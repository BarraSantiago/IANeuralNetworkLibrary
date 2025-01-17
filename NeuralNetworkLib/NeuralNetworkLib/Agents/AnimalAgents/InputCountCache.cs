using System.Collections.Concurrent;
using System.Linq;
using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.Agents.SimAgents
{
    public static class InputCountCache
    {
        private static readonly ConcurrentDictionary<(AgentTypes, BrainType), int> cache = new ConcurrentDictionary<(AgentTypes, BrainType), int>();

        public static int GetInputCount(AgentTypes agentType, BrainType brainType)
        {
            (AgentTypes agentType, BrainType brainType) key = (agentType, brainType);
            if (cache.TryGetValue(key, out int inputCount)) return inputCount;
            
            inputCount = DataContainer.inputCounts
                .FirstOrDefault(input => input.AgentType == agentType && input.BrainType == brainType).InputCount;
            cache[key] = inputCount;

            return inputCount;
        }
    }
}