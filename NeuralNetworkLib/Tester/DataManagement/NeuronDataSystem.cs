using System.Text.Json;
using System.Text.RegularExpressions;

namespace Tester.DataManagement
{
    public enum SimAgentTypes
    {
        Carnivorous,
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
                string json = JsonSerializer.Serialize(group.Value);
                File.WriteAllText(filePath, json);
                Console.WriteLine("Saving in path: " + filePath);

            }
        }
        
        public static Dictionary<SimAgentTypes, Dictionary<BrainType, List<AgentNeuronData>>> LoadLatestNeurons(string directoryPath)
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

                var latestFile = files.OrderByDescending(f =>
                {
                    var fileName = Path.GetFileName(f);
                    var match = Regex.Match(fileName, @"gen(\d+)");
                    if (match.Success)
                    {
                        return int.Parse(match.Groups[1].Value);
                    }
                    return 0;
                }).First();

                var json = File.ReadAllText(latestFile);
                var agentDataList = JsonSerializer.Deserialize<List<AgentNeuronData>>(json);

                foreach (var agentData in agentDataList)
                {
                    if (!agentsData[agentType].ContainsKey(agentData.BrainType))
                    {
                        agentsData[agentType][agentData.BrainType] = new List<AgentNeuronData>();
                    }
                    agentsData[agentType][agentData.BrainType].Add(agentData);
                }
            }

            return agentsData;
        }        
    }
}