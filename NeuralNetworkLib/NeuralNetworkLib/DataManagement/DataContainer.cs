using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flocking;
using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

using SimAgentType = NeuralNetworkLib.Agents.SimAgents.SimAgent<NeuralNetworkLib.Utils.IVector, NeuralNetworkLib.Utils.ITransform<NeuralNetworkLib.Utils.IVector>>;
using SimBoid = NeuralNetworkLib.Agents.Flocking.Boid<NeuralNetworkLib.Utils.IVector, NeuralNetworkLib.Utils.ITransform<NeuralNetworkLib.Utils.IVector>>;

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
    private static Dictionary<uint, SimAgentType> _agents = new Dictionary<uint, SimAgentType>();
    private static Dictionary<uint, Scavenger<IVector, ITransform<IVector>>> _scavengers = new Dictionary<uint, Scavenger<IVector, ITransform<IVector>>>();
    public static FlockingManager flockingManager = new FlockingManager();
    public static Dictionary<(BrainType, SimAgentTypes), NeuronInputCount> InputCountCache;
    public static NeuronInputCount[] inputCounts;
    private static Dictionary<int, BrainType> herbBrainTypes = new  Dictionary<int, BrainType>();
    private static Dictionary<int, BrainType> scavBrainTypes = new  Dictionary<int, BrainType>();
    private static Dictionary<int, BrainType> carnBrainTypes = new  Dictionary<int, BrainType>();

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

    public static SimAgentType GetNearestEntity(SimAgentTypes entityType, IVector position)
    {
        SimAgentType nearestAgent = null;
        float minDistance = float.MaxValue;

        foreach (SimAgentType agent in _agents.Values)
        {
            if (agent.agentType != entityType) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;
            nearestAgent = agent;
        }

        return nearestAgent;
    }

    public static List<ITransform<IVector>> GetBoidsInsideRadius(SimBoid boid)
    {
        List<ITransform<IVector>> insideRadiusBoids = new List<ITransform<IVector>>();
        float detectionRadiusSquared = boid.detectionRadious * boid.detectionRadious;
        IVector boidPosition = boid.transform.position;

        Parallel.ForEach(_scavengers.Values, scavenger =>
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

        foreach (var kvp in brainTypes)
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