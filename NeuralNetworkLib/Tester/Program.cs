using System.Text.Json;
using Tester.DataManagement;

namespace Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            string directoryPath = "NeuronData";
            int generation = 1;

            // Create test data
            List<AgentNeuronData> agentsData = new List<AgentNeuronData>
            {
                new AgentNeuronData
                {
                    AgentId = 1,
                    AgentType = SimAgentTypes.Carnivorous,
                    BrainType = BrainType.Attack,
                    NeuronWeights = new List<float[]> { new float[] { 0.1f, 0.2f }, new float[] { 0.3f, 0.4f } },
                    Fitness = 5,
                },
                new AgentNeuronData
                {
                    AgentId = 2,
                    AgentType = SimAgentTypes.Carnivorous,
                    BrainType = BrainType.Attack,
                    NeuronWeights = new List<float[]> { new float[] { 0.1f, 0.2f }, new float[] { 0.3f, 0.4f } },
                    Fitness = 5.2f,
                    Bias = 2,
                    P = 0.6f,
                    TotalWeights = 4
                },
                new AgentNeuronData
                {
                    AgentId = 3,
                    AgentType = SimAgentTypes.Carnivorous,
                    BrainType = BrainType.Attack,
                    NeuronWeights = new List<float[]> { new float[] { 0.1f, 0.2f }, new float[] { 1.3f, 0.4f } },
                    Fitness = 5
                },
                new AgentNeuronData
                {
                    AgentId = 1,
                    AgentType = SimAgentTypes.Carnivorous,
                    BrainType = BrainType.Movement,
                    NeuronWeights = new List<float[]> { new float[] { 0.1f, 0.2f }, new float[] { 0.3f, 0.4f } },
                    Fitness = 2
                },
                new AgentNeuronData
                {
                    AgentId = 2,
                    AgentType = SimAgentTypes.Herbivore,
                    BrainType = BrainType.Escape,
                    NeuronWeights = new List<float[]> { new float[] { 0.5f, 0.6f }, new float[] { 0.7f, 0.8f } }
                },
                new AgentNeuronData
                {
                    AgentId = 2,
                    AgentType = SimAgentTypes.Herbivore,
                    BrainType = BrainType.Movement,
                    NeuronWeights = new List<float[]> { new float[] { 0.5f, 0.6f }, new float[] { 0.7f, 0.8f } }
                },
                new AgentNeuronData
                {
                    AgentId = 3,
                    AgentType = SimAgentTypes.Scavenger,
                    BrainType = BrainType.ScavengerMovement,
                    NeuronWeights = new List<float[]> { new float[] { 0.9f, 1.0f }, new float[] { 1.1f, 1.2f } }
                },
                new AgentNeuronData
                {
                    AgentId = 3,
                    AgentType = SimAgentTypes.Scavenger,
                    BrainType = BrainType.Eat,
                    NeuronWeights = new List<float[]> { new float[] { 0.9f, 1.0f }, new float[] { 1.1f, 1.2f } }
                }
            };
            

            // Save the data
            NeuronDataSystem.SaveNeurons(agentsData, directoryPath, generation);
            Console.WriteLine("Save Done");

            // Load the data
            Dictionary<SimAgentTypes, Dictionary<BrainType, List<AgentNeuronData>>> loadedData = NeuronDataSystem.LoadLatestNeurons(directoryPath);
            Console.WriteLine("Load Started");

            // Display the loaded data
            foreach (SimAgentTypes agentType in loadedData.Keys)
            {
                Console.WriteLine($"Agent Type: {agentType}");
                foreach (BrainType brainType in loadedData[agentType].Keys)
                {
                    Console.WriteLine($"  Brain Type: {brainType}");
                    foreach (AgentNeuronData agentData in loadedData[agentType][brainType])
                    {
                        Console.WriteLine($"    Agent ID: {agentData.AgentId}");
                        Console.WriteLine($"    Neuron Weights: {JsonSerializer.Serialize(agentData.NeuronWeights)}");
                        Console.WriteLine($"    Fitness: {JsonSerializer.Serialize(agentData.Fitness)}");
                        Console.WriteLine($"    Bias: {JsonSerializer.Serialize(agentData.Bias)}");
                        Console.WriteLine($"    P: {JsonSerializer.Serialize(agentData.P)}");
                        Console.WriteLine($"    Total Weights: {JsonSerializer.Serialize(agentData.TotalWeights)}");
                    }
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}