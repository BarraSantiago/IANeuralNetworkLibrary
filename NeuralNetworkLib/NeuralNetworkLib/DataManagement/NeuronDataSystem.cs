﻿using Newtonsoft.Json;

namespace NeuralNetworkLib.DataManagement
{
    public enum AgentTypes
    {
        Carnivore,
        Herbivore,
        Gatherer,
        Cart,
        Builder
    }

    public enum BrainType
    {
        Movement,
        Eat,
        Attack,
        Escape,
        Flocking
    }

    public static class NeuronDataSystem
    {
        public static Action<bool> OnSpecificLoaded;

        
        public static void DeleteBrainFiles(AgentTypes agentType, BrainType brainType, string directoryPath)
        {
            string agentTypeDirectory = Path.Combine(directoryPath, agentType.ToString());
            string brainTypeDirectory = Path.Combine(agentTypeDirectory, brainType.ToString());

            if (Directory.Exists(brainTypeDirectory))
            {
                string[] files = Directory.GetFiles(brainTypeDirectory);
                foreach (string file in files)
                {
                    File.Delete(file);
                }
            }
        }
        
        public static void SaveNeurons(List<AgentNeuronData> agentsData, string directoryPath, int generation)
        {
            if (agentsData == null)
            {
                throw new ArgumentNullException(nameof(agentsData), "Agents data cannot be null.");
            }

            var groupedData = agentsData
                .GroupBy(agent => new { agent.AgentType, agent.BrainType })
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var group in groupedData)
            {
                string agentTypeDirectory = Path.Combine(directoryPath, group.Key.AgentType.ToString());
                string brainTypeDirectory = Path.Combine(agentTypeDirectory, group.Key.BrainType.ToString());
                Directory.CreateDirectory(brainTypeDirectory);

                string fileName = $"gen{generation}.json";
                string filePath = Path.Combine(brainTypeDirectory, fileName);
                string json = JsonConvert.SerializeObject(group.Value);
                File.WriteAllText(filePath, json);
            }
        }

        public static Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>> LoadLatestNeurons(
            string directoryPath)
        {
            Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>> agentsData =
                new Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>>();

            string[] agentDirectories = Directory.Exists(directoryPath)
                ? Directory.GetDirectories(directoryPath)
                : Array.Empty<string>();

            foreach (string agentTypeDirectory in agentDirectories)
            {
                AgentTypes agentType = Enum.Parse<AgentTypes>(Path.GetFileName(agentTypeDirectory));
                agentsData[agentType] = new Dictionary<BrainType, List<AgentNeuronData>?>();

                string[] brainDirectories = Directory.GetDirectories(agentTypeDirectory);
                foreach (string brainTypeDirectory in brainDirectories)
                {
                    BrainType brainType = Enum.Parse<BrainType>(Path.GetFileName(brainTypeDirectory));
                    string[] files = Directory.GetFiles(brainTypeDirectory, "gen*.json");
                    if (files.Length == 0)
                        continue;

                    string? latestFile = files
                        .OrderByDescending(f =>
                        {
                            string? fileName = Path.GetFileName(f);
                            string[]? parts = fileName.Split('n');
                            if (parts.Length > 1 && int.TryParse(parts[1].Split('.')[0], out int generation))
                            {
                                return generation;
                            }

                            return -1;
                        }).First();

                    string json = File.ReadAllText(latestFile);
                    List<AgentNeuronData>? agentData;
                    try
                    {
                        agentData = JsonConvert.DeserializeObject<List<AgentNeuronData>>(json);
                    }
                    catch (JsonException)
                    {
                        agentData = new List<AgentNeuronData>();
                    }

                    agentsData[agentType][brainType] = agentData;
                }
            }

            return agentsData;
        }

        public static Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>> LoadSpecificNeurons(
            string directoryPath, int specificGeneration)
        {
            Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>> agentsData =
                new Dictionary<AgentTypes, Dictionary<BrainType, List<AgentNeuronData>?>>();
            string[] agentDirectories = Directory.Exists(directoryPath)
                ? Directory.GetDirectories(directoryPath)
                : Array.Empty<string>();

            foreach (string agentTypeDirectory in agentDirectories)
            {
                AgentTypes agentType = Enum.Parse<AgentTypes>(Path.GetFileName(agentTypeDirectory));
                agentsData[agentType] = new Dictionary<BrainType, List<AgentNeuronData>?>();

                string[] brainDirectories = Directory.GetDirectories(agentTypeDirectory);
                foreach (string brainTypeDirectory in brainDirectories)
                {
                    BrainType brainType = Enum.Parse<BrainType>(Path.GetFileName(brainTypeDirectory));
                    string[] files = Directory.GetFiles(brainTypeDirectory, "gen*.json");
                    if (files.Length == 0)
                        continue;

                    string? targetFile = files
                        .FirstOrDefault(f =>
                        {
                            string? fileName = Path.GetFileName(f);
                            string[]? parts = fileName.Split('n');
                            return parts.Length > 1 && int.TryParse(parts[1].Split('.')[0], out int generation) &&
                                   generation == specificGeneration;
                        });

                    if (targetFile == null)
                    {
                        targetFile = files
                            .OrderByDescending(f =>
                            {
                                string? fileName = Path.GetFileName(f);
                                string[]? parts = fileName.Split('n');
                                if (parts.Length > 1 && int.TryParse(parts[1].Split('.')[0], out int generation))
                                {
                                    return generation;
                                }

                                return -1;
                            }).First();
                        OnSpecificLoaded?.Invoke(false);
                    }
                    else
                    {
                        OnSpecificLoaded?.Invoke(true);
                    }

                    string json = File.ReadAllText(targetFile);
                    List<AgentNeuronData>? agentData;
                    try
                    {
                        agentData = JsonConvert.DeserializeObject<List<AgentNeuronData>>(json);
                    }
                    catch (JsonException)
                    {
                        agentData = new List<AgentNeuronData>();
                    }

                    agentsData[agentType][brainType] = agentData;
                }
            }

            return agentsData;
        }
    }
}