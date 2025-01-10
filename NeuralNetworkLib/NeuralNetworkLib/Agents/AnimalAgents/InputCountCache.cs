using System.Collections.Concurrent;
using System.Linq;
using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.Agents.SimAgents
{
    public static class InputCountCache
    {
        private static readonly ConcurrentDictionary<(AnimalAgentTypes, BrainType), int> cache = new ConcurrentDictionary<(AnimalAgentTypes, BrainType), int>();

        public static int GetInputCount(AnimalAgentTypes agentType, BrainType brainType)
        {
            (AnimalAgentTypes agentType, BrainType brainType) key = (agentType, brainType);
            if (cache.TryGetValue(key, out int inputCount)) return inputCount;
            
            inputCount = DataContainer.inputCounts
                .FirstOrDefault(input => input.agentType == agentType && input.brainType == brainType).inputCount;
            cache[key] = inputCount;

            return inputCount;
        }
    }
}