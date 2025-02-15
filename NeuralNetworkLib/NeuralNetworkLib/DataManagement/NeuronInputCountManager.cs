namespace NeuralNetworkLib.DataManagement;

using System.IO;
using Newtonsoft.Json;

public static class NeuronInputCountManager
{
    public static void SaveNeuronInputCounts(BrainConfiguration[]? inputCounts, string filePath)
    {
        string json = JsonConvert.SerializeObject(inputCounts, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public static BrainConfiguration[]? LoadNeuronInputCounts(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
            throw new FileNotFoundException("The specified file was not found.", filePath);
        }

        string json = File.ReadAllText(filePath);
        return JsonConvert.DeserializeObject<BrainConfiguration[]>(json);
    }
}