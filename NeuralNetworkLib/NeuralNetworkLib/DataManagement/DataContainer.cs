using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.DataManagement;

public struct NeuronInputCount
{
    public SimAgentTypes agentType;
    public BrainType brainType;
    public int inputCount;
    public int outputCount;
    public int[] hiddenLayersInputs;
}

public class DataContainer
{
    public static Sim2Graph graph;
    public static Dictionary<uint, SimAgent<IVector, ITransform<IVector>>> Agents = new Dictionary<uint, SimAgent<IVector, ITransform<IVector>>>();
    public static Dictionary<uint, Scavenger<IVector, ITransform<IVector>>> Scavengers = new Dictionary<uint, Scavenger<IVector, ITransform<IVector>>>();
    public static FlockingManager flockingManager = new FlockingManager();
    public static Dictionary<(BrainType, SimAgentTypes), NeuronInputCount> InputCountCache;
    public static NeuronInputCount[] inputCounts;
    public static Dictionary<int, BrainType> herbBrainTypes = new  Dictionary<int, BrainType>();
    public static Dictionary<int, BrainType> scavBrainTypes = new  Dictionary<int, BrainType>();
    public static Dictionary<int, BrainType> carnBrainTypes = new  Dictionary<int, BrainType>();

    public static INode<IVector> CoordinateToNode(IVector coordinate)
    {
        if (coordinate.X < 0 || coordinate.Y < 0 || coordinate.X >= graph.MaxX || coordinate.Y >= graph.MaxY)
        {
            return null;
        }

        return graph.NodesType[(int)coordinate.X, (int)coordinate.Y];
    }


    public static INode<IVector> GetNearestNode(SimNodeType nodeType, IVector position)
    {
        INode<IVector> nearestNode = null;
        float minDistance = float.MaxValue;

        foreach (SimNode<IVector> node in graph.NodesType)
        {
            if (node.NodeType != nodeType) continue;

            float distance = IVector.Distance(position, node.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;

            nearestNode = node;
        }

        return nearestNode;
    }

    public static SimAgent<IVector, ITransform<IVector>> GetNearestEntity(SimAgentTypes entityType, IVector position)
    {
        SimAgent<IVector, ITransform<IVector>> nearestAgent = null;
        float minDistance = float.MaxValue;

        foreach (SimAgent<IVector, ITransform<IVector>> agent in Agents.Values)
        {
            if (agent.agentType != entityType) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;
            nearestAgent = agent;
        }

        return nearestAgent;
    }

    public static List<ITransform<IVector>> GetBoidsInsideRadius(Boid<IVector, ITransform<IVector>> boid)
    {
        List<ITransform<IVector>> insideRadiusBoids = new List<ITransform<IVector>>();
        float detectionRadiusSquared = boid.detectionRadious * boid.detectionRadious;
        IVector boidPosition = boid.transform.position;

        Parallel.ForEach(Scavengers.Values, scavenger =>
        {
            if (scavenger?.Transform.position == null || boid == scavenger.boid)
            {
                return;
            }

            IVector scavengerPosition = scavenger.Transform.position;
            float distanceSquared = IVector.DistanceSquared(boidPosition, scavengerPosition);

            if (distanceSquared > detectionRadiusSquared) return;
            lock (insideRadiusBoids)
            {
                insideRadiusBoids.Add(scavenger.boid.transform);
            }
        });

        return insideRadiusBoids;
    }

    public static int GetBrainTypeKeyByValue(BrainType value, SimAgentTypes agentType)
    {
        Dictionary<int, BrainType> brainTypes = agentType switch
        {
            SimAgentTypes.Carnivore => carnBrainTypes,
            SimAgentTypes.Herbivore => herbBrainTypes,
            SimAgentTypes.Scavenger => scavBrainTypes,
            _ => throw new ArgumentException("Invalid agent type")
        };

        foreach (KeyValuePair<int, BrainType> kvp in brainTypes)
        {
            if (kvp.Value == value)
            {
                return kvp.Key;
            }
        }

        throw new KeyNotFoundException(
            $"The value '{value}' is not present in the brainTypes dictionary for agent type '{agentType}'.");
    }
}