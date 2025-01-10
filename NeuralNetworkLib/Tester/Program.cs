using NeuralNetworkLib.Utils;
using Newtonsoft.Json;
using Xunit;

namespace Tester;

public class Sim2GraphTests
{
    [Fact]
    public void SaveGraph_SavesCorrectDataToFile()
    {
        // Arrange
        var graph = new Sim2Graph(2, 2, 1.0f);
        graph.CreateGraph(2, 2, 1.0f);
        string filePath = "test_graph.json";

        // Act
        graph.SaveGraph(filePath);

        // Assert
        var json = File.ReadAllText(filePath);
        var nodeData = JsonConvert.DeserializeObject<List<Sim2Graph.NodeData>>(json);
        Assert.NotNull(nodeData);
        Assert.Equal(4, nodeData.Count);
        File.Delete(filePath);
    }

    [Fact]
    public void LoadGraph_LoadsCorrectDataFromFile()
    {
        // Arrange
        var graph = new Sim2Graph(2, 2, 1.0f);
        int[] nodeTypes = { 0, 1, 2, 3 };
        int[] nodeTerrains = { 0, 1, 2, 3 };

        // Act
        graph.LoadGraph(nodeTypes, nodeTerrains);

        // Assert
        for (int i = 0; i < 2; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                Assert.Equal((NodeType)nodeTypes[i * 2 + j], graph.NodesType[i, j].NodeType);
                Assert.Equal((NodeTerrain)nodeTerrains[i * 2 + j], graph.NodesType[i, j].NodeTerrain);
            }
        }
    }

    [Fact]
    public void SaveGraph_EmptyGraph_SavesEmptyData()
    {
        // Arrange
        var graph = new Sim2Graph(0, 0, 1.0f);
        string filePath = "empty_graph.json";

        // Act
        graph.SaveGraph(filePath);

        // Assert
        var json = File.ReadAllText(filePath);
        var nodeData = JsonConvert.DeserializeObject<List<Sim2Graph.NodeData>>(json);
        Assert.NotNull(nodeData);
        Assert.Empty(nodeData);
        File.Delete(filePath);
    }

    [Fact]
    public void LoadGraph_EmptyData_LoadsEmptyGraph()
    {
        // Arrange
        var graph = new Sim2Graph(0, 0, 1.0f);
        int[] nodeTypes = { };
        int[] nodeTerrains = { };

        // Act
        graph.LoadGraph(nodeTypes, nodeTerrains);

        // Assert
        Assert.Empty(graph.CoordNodes);
    }
}