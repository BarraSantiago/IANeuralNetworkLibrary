using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;
using Pathfinder;

namespace NeuralNetworkLib.DataManagement;

using AStarPath = AStarPathfinder<SimNode<IVector>, IVector, SimCoordinate>;

public struct NeuronInputCount
{
    public AnimalAgentTypes agentType;
    public BrainType brainType;
    public int inputCount;
    public int outputCount;
    public int[] hiddenLayersInputs;
}

public class DataContainer
{
    public static Sim2Graph graph;

    public static Dictionary<uint, AnimalAgent<IVector, ITransform<IVector>>> Animals = new();
    public static Dictionary<uint, TcAgent<IVector, ITransform<IVector>>> TCAgents = new();

    public static FlockingManager flockingManager = new();
    public static Dictionary<(BrainType, AnimalAgentTypes), NeuronInputCount> InputCountCache;
    public static NeuronInputCount[] inputCounts;
    public static Dictionary<int, BrainType> herbBrainTypes = new();
    public static Dictionary<int, BrainType> carnBrainTypes = new();
    public static AStarPath gathererPathfinder;
    public static AStarPath builderPathfinder;
    public static AStarPath cartPathfinder;

    public static void Init()
    {
        herbBrainTypes = new Dictionary<int, BrainType>();
        carnBrainTypes = new Dictionary<int, BrainType>();

        herbBrainTypes[0] = BrainType.Eat;
        herbBrainTypes[1] = BrainType.Movement;
        herbBrainTypes[2] = BrainType.Escape;

        carnBrainTypes[0] = BrainType.Eat;
        carnBrainTypes[1] = BrainType.Movement;
        carnBrainTypes[2] = BrainType.Attack;

        inputCounts = new[]
        {
            new NeuronInputCount
            {
                agentType = AnimalAgentTypes.Carnivore, brainType = BrainType.Movement, inputCount = 5,
                outputCount = 3, hiddenLayersInputs = new[] { 3 }
            },
            new NeuronInputCount
            {
                agentType = AnimalAgentTypes.Carnivore, brainType = BrainType.Attack, inputCount = 4,
                outputCount = 1, hiddenLayersInputs = new[] { 1 }
            },
            new NeuronInputCount
            {
                agentType = AnimalAgentTypes.Herbivore, brainType = BrainType.Eat, inputCount = 4, outputCount = 1,
                hiddenLayersInputs = new[] { 1 }
            },
            new NeuronInputCount
            {
                agentType = AnimalAgentTypes.Herbivore, brainType = BrainType.Movement, inputCount = 8,
                outputCount = 2, hiddenLayersInputs = new[] { 3 }
            },
            new NeuronInputCount
            {
                agentType = AnimalAgentTypes.Herbivore, brainType = BrainType.Escape, inputCount = 4, outputCount = 1,
                hiddenLayersInputs = new[] { 1 }
            },
        };

        InputCountCache = inputCounts.ToDictionary(input => (input.brainType, input.agentType));

        InitPathfinder(ref gathererPathfinder, 0, 0);
        InitPathfinder(ref cartPathfinder, 50, 15);
        InitPathfinder(ref builderPathfinder, 30, 0);
    }

    private static void InitPathfinder(ref AStarPath pathfinder, int mountainCost = 0, int sandCost = 0)
    {
        if (pathfinder == null) throw new ArgumentNullException(nameof(pathfinder));
        const int normalCost = 100;
        const int maxModCost = 30;
        
        foreach (SimNode<IVector> node in graph.NodesType)
        {
            node.SetCost(normalCost);
            switch (node.NodeType)
            {
                case NodeType.Lake:
                    node.SetCost(1000);
                    node.isBlocked = true;
                    break;
                case NodeType.Mountain:
                    node.SetCost(mountainCost);
                    if(mountainCost > maxModCost) node.isBlocked = true;
                    break;
                case NodeType.Sand:
                    node.SetCost(sandCost);
                    if(sandCost > maxModCost) node.isBlocked = true;
                    break;
                default:
                    break;
            }
        }

        pathfinder = new AStarPath(graph.NodesType.Cast<SimNode<IVector>>().ToList());
    }

    public static INode<IVector> CoordinateToNode(IVector coordinate)
    {
        if (coordinate.X < 0 || coordinate.Y < 0 || coordinate.X >= graph.MaxX || coordinate.Y >= graph.MaxY)
        {
            return null;
        }

        return graph.NodesType[(int)coordinate.X, (int)coordinate.Y];
    }


    public static INode<IVector> GetNearestNode(NodeTerrain nodeType, IVector position)
    {
        INode<IVector> nearestNode = null;
        float minDistance = float.MaxValue;

        foreach (SimNode<IVector> node in graph.NodesType)
        {
            if (node.NodeTerrain != nodeType) continue;

            float distance = IVector.Distance(position, node.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;

            nearestNode = node;
        }

        return nearestNode;
    }

    public static AnimalAgent<IVector, ITransform<IVector>> GetNearestEntity(AnimalAgentTypes entityType,
        IVector position)
    {
        AnimalAgent<IVector, ITransform<IVector>> nearestAgent = null;
        float minDistance = float.MaxValue;

        foreach (AnimalAgent<IVector, ITransform<IVector>> agent in Animals.Values)
        {
            if (agent.agentType != entityType) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;
            nearestAgent = agent;
        }

        return nearestAgent;
    }

    public static (uint, bool) GetNearestPrey(IVector position)
    {
        uint nearestAgent = 0;
        bool isAnimal = true;
        float minDistance = float.MaxValue;

        foreach (var prey in Animals)
        {
            var agent = prey.Value;
            if (agent.agentType != AnimalAgentTypes.Herbivore) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (distance > minDistance) continue;
            minDistance = distance;
            nearestAgent = prey.Key;
        }

        foreach (var prey in TCAgents)
        {
            var agent = prey.Value;
            if (agent.AgentType != AgentTypes.Cart && agent.CurrentFood > 0) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (distance > minDistance) continue;
            minDistance = distance;
            isAnimal = false;
            nearestAgent = prey.Key;
        }

        return (nearestAgent, isAnimal);
    }

    public static IVector GetPosition(uint id, bool isAnimal)
    {
        return isAnimal ? Animals[id].CurrentNode.GetCoordinate() : TCAgents[id].CurrentNode.GetCoordinate();
    }

    public static void Attack(uint id, bool isAnimal)
    {
        Herbivore<IVector, ITransform<IVector>> herbivore = (Herbivore<IVector, ITransform<IVector>>)Animals[id];
        herbivore.Hp -= 1;
    }

    public static List<ITransform<IVector>> GetBoidsInsideRadius(Boid<IVector, ITransform<IVector>> boid)
    {
        List<ITransform<IVector>> insideRadiusBoids = new();
        float detectionRadiusSquared = boid.detectionRadious * boid.detectionRadious;
        IVector boidPosition = boid.transform.position;

        // TODO Fix boid search
        /*Parallel.ForEach(Scavengers.Values, scavenger =>
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
        });*/

        return insideRadiusBoids;
    }

    public static int GetBrainTypeKeyByValue(BrainType value, AnimalAgentTypes agentType)
    {
        Dictionary<int, BrainType> brainTypes = agentType switch
        {
            AnimalAgentTypes.Carnivore => carnBrainTypes,
            AnimalAgentTypes.Herbivore => herbBrainTypes,
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