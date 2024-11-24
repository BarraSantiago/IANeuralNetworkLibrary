
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NeuralNetworkLib.DataManagement
{
    public enum SimAgentTypes
    {
        Carnivore,
        Herbivore,
        Scavenger
    }
    public enum BrainType
    {
        Movement,
        ScavengerMovement,
        Eat,
        Attack,
        Escape,
        Flocking
    }
    
    public static class NeuronDataSystem
    {
        public static void SaveNeurons(List<AgentNeuronData> agentsData, string directoryPath, int generation)
        {
            var groupedData = agentsData
                .GroupBy(agent => new { agent.AgentType, agent.BrainType })
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var group in groupedData)
            {
                string agentTypeDirectory = Path.Combine(directoryPath, group.Key.AgentType.ToString());
                Directory.CreateDirectory(agentTypeDirectory);

                string fileName = $"gen{generation}{group.Key.BrainType}.json";
                string filePath = Path.Combine(agentTypeDirectory, fileName);
                string json = "";// JsonSerializer.Serialize(group.Value);
                File.WriteAllText(filePath, json);
            }
        }

        public static Dictionary<SimAgentTypes, Dictionary<BrainType, List<AgentNeuronData>>>
            LoadLatestNeurons(string directoryPath)
        {
            var agentsData = new Dictionary<SimAgentTypes, Dictionary<BrainType, List<AgentNeuronData>>>();
            var directories = Directory.GetDirectories(directoryPath);

            foreach (var agentTypeDirectory in directories)
            {
                var agentType = Enum.Parse<SimAgentTypes>(Path.GetFileName(agentTypeDirectory));
                agentsData[agentType] = new Dictionary<BrainType, List<AgentNeuronData>>();

                var files = Directory.GetFiles(agentTypeDirectory, "gen*.json");
                if (files.Length == 0)
                    continue;

                var latestFile = files
                    .OrderByDescending(f => int.Parse(Path.GetFileName(f).Split('n')[1].Split('.')[0])).First();
                var brainType = Enum.Parse<BrainType>(Path.GetFileName(latestFile).Split('n')[1].Split('.')[0]);

                var json = File.ReadAllText(latestFile);
                //var agentData = JsonSerializer.Deserialize<List<AgentNeuronData>>(json);
                //agentsData[agentType][brainType] = agentData;
            }

            return agentsData;
        }
    }
}