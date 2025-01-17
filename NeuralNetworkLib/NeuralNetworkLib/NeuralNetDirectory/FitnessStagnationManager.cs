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
    // TODO in each epoch, fitness data should be updated
    private List<AgentFitnessData> fitnessData = new();
    
    // Esto deberia empezar a partir de 1000 generaciones y revisar si hubo avances comparando las primeras generaciones
    // con las ultimas.
    private const int GenerationsPerCheck = 100;
    private const double StagnationThreshold = 0.1;


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
        foreach (AgentFitnessData agentData in fitnessData)
        {
            if (agentData.FitnessData.Count < GenerationsPerCheck) continue;

            if (!CalculateStagnation(agentData.FitnessData)) continue;

            for (int i = 0; i < DataContainer.inputCounts.Length; i++)
            {
                if (DataContainer.inputCounts[i].AgentType != agentData.AgentType ||
                    DataContainer.inputCounts[i].BrainType != agentData.BrainType) continue;

                List<int> oldLayers = DataContainer.inputCounts[i].HiddenLayersInputs.ToList();

                for (int j = 0; j < oldLayers.Count; j++)
                {
                    oldLayers[j]++;
                }

                // Modificar esto teniendo en cuenta inputs y outputs de la red
                oldLayers.Add(oldLayers[0]);

                DataContainer.inputCounts[i].HiddenLayersInputs = oldLayers.ToArray();
                agentData.FitnessData.Clear();

                stagnation = true;
            }
        }

        if (stagnation)
        {
            DataContainer.InputCountCache = DataContainer.inputCounts.ToDictionary(
                input => (brainType: input.BrainType, agentType: input.AgentType));

            // TODO create save for new brain configuration
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