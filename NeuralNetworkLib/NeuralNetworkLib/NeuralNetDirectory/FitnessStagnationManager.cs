using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.NeuralNetDirectory;

public struct AgentFitnessData
{
    public AgentTypes AgentType;
    public BrainType BrainType;
    public List<float> FitnessData;
}

public class FitnessStagnationManager
{
    private List<AgentFitnessData> fitnessData = new();

    // TODO Esto deberia empezar a partir de 300 generaciones y revisar si hubo avances comparando las primeras
    // generaciones con las ultimas.
    private const int GenerationsPerCheck = 100;
    private const int MinGenerationsAmount = 200;
    private const double StagnationThreshold = 0.1;
    private const string DirectoryPath = "NeuronData";
    const string filePath = "BrainConfigurations.json";


    public void AddFitnessData(AgentTypes agentType, BrainType brainType, float averageFitness)
    {
        foreach (AgentFitnessData agentFitnessData in fitnessData)
        {
            if (agentFitnessData.AgentType != agentType || agentFitnessData.BrainType != brainType) continue;
            agentFitnessData.FitnessData.Add(averageFitness);
            return;
        }

        fitnessData.Add(new AgentFitnessData
        {
            AgentType = agentType,
            BrainType = brainType,
            FitnessData = new List<float>() { averageFitness }
        });
    }

    public void AnalyzeData()
    {
        bool stagnation = false;
        List<(AgentTypes agentType, BrainType brainType)>
            keys = new List<(AgentTypes agentType, BrainType brainType)>();
        foreach (AgentFitnessData agentData in fitnessData)
        {
            if (agentData.FitnessData.Count - MinGenerationsAmount < GenerationsPerCheck) continue;

            if (!CalculateStagnation(agentData.FitnessData)) continue;
            BrainConfiguration brain = new BrainConfiguration();

            int index = 0;
            for (int i = 0; i < DataContainer.inputCounts.Length; i++)
            {
                if (DataContainer.inputCounts[i].BrainType == agentData.BrainType &&
                    DataContainer.inputCounts[i].AgentType == agentData.AgentType)
                {
                    brain = DataContainer.inputCounts[i];
                    index = i;
                    break;
                }
            }

            if (brain.AgentType != agentData.AgentType ||
                brain.BrainType != agentData.BrainType) continue;

            List<int> newHiddenLayers = brain.HiddenLayers.ToList();

            bool increaseNeurons = false;
            for (int j = 0; j < newHiddenLayers.Count; j++)
            {
                if (newHiddenLayers[j] < brain.InputCount)
                {
                    newHiddenLayers[j]++;
                    increaseNeurons = true;
                    break;
                }
            }

            if (!increaseNeurons) newHiddenLayers.Add(brain.OutputCount);

            DataContainer.inputCounts[index].HiddenLayers = newHiddenLayers.ToArray();
            
            agentData.FitnessData.Clear();

            stagnation = true;
            keys.Add((agentData.AgentType, agentData.BrainType));
            break;
        }

        if (!stagnation) return;

        DataContainer.UpdateInputCache();   

        BrainConfiguration[]? inputCounts = DataContainer.inputCounts;

        NeuronInputCountManager.SaveNeuronInputCounts(inputCounts, filePath);
        foreach ((AgentTypes agentType, BrainType brainType) tuple in keys)
        {
            NeuronDataSystem.DeleteBrainFiles(tuple.agentType, tuple.brainType, DirectoryPath);
        }
    }

    private bool CalculateStagnation(List<float> fitness)
    {
        double average = fitness.Average();
        double sumOfSquaresOfDifferences = fitness.Select(val => (val - average) * (val - average)).Sum();
        double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / fitness.Count);
        return standardDeviation < StagnationThreshold;
    }
}