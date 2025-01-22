using System.Collections.Concurrent;
using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.Utils;
using Pathfinder;

namespace NeuralNetworkLib.DataManagement;

using AStarPath = AStarPathfinder<SimNode<IVector>, IVector, CoordinateNode>;

public struct NeuronInputCount
{
    public AgentTypes AgentType;
    public BrainType BrainType;
    public int InputCount;
    public int OutputCount;
    public int[] HiddenLayersInputs;
}

public class DataContainer
{
    public static Sim2Graph Graph;

    public static Dictionary<uint, AnimalAgent<IVector, ITransform<IVector>>> Animals = new();
    public static Dictionary<uint, TcAgent<IVector, ITransform<IVector>>> TcAgents = new();

    public static FlockingManager FlockingManager = new();
    public static Dictionary<(BrainType, AgentTypes), NeuronInputCount> InputCountCache;
    public static NeuronInputCount[]? inputCounts;
    public static Dictionary<int, BrainType> HerbBrainTypes = new();
    public static Dictionary<int, BrainType> CarnBrainTypes = new();
    public static AStarPath? GathererPathfinder;
    public static AStarPath? BuilderPathfinder;
    public static AStarPath? CartPathfinder;
    public static Voronoi<CoordinateNode, MyVector>[] Voronois;
    public static Action<NodeTerrain> OnUpdateVoronoi = UpdateVoronoi;

    private const string FilePath = "path/to/your/file.json";

    private static ParallelOptions parallelOptions = new ParallelOptions
    {
        MaxDegreeOfParallelism = 32
    };

    public static void Init()
    {
        HerbBrainTypes = new Dictionary<int, BrainType>();
        CarnBrainTypes = new Dictionary<int, BrainType>();

        HerbBrainTypes[0] = BrainType.Eat;
        HerbBrainTypes[1] = BrainType.Movement;
        HerbBrainTypes[2] = BrainType.Escape;

        CarnBrainTypes[0] = BrainType.Movement;
        CarnBrainTypes[1] = BrainType.Attack;

        LoadInputCount();

        InputCountCache = inputCounts.ToDictionary(input => (brainType: input.BrainType, agentType: input.AgentType));

        InitPathfinder(ref GathererPathfinder, 0, 0);
        InitPathfinder(ref CartPathfinder, 50, 15);
        InitPathfinder(ref BuilderPathfinder, 30, 0);
        InitVoronois();
    }


    private static void LoadInputCount()
    {
        inputCounts = NeuronInputCountManager.LoadNeuronInputCounts(FilePath);

        if (inputCounts == null || inputCounts.Length == 0)
        {
            inputCounts = new[]
            {
                new NeuronInputCount
                {
                    AgentType = AgentTypes.Carnivore, BrainType = BrainType.Movement, InputCount = 5,
                    OutputCount = 3, HiddenLayersInputs = new[] { 3 }
                },
                new NeuronInputCount
                {
                    AgentType = AgentTypes.Carnivore, BrainType = BrainType.Attack, InputCount = 4,
                    OutputCount = 1, HiddenLayersInputs = new[] { 1 }
                },
                new NeuronInputCount
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Eat, InputCount = 4, OutputCount = 1,
                    HiddenLayersInputs = new[] { 1 }
                },
                new NeuronInputCount
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Movement, InputCount = 8,
                    OutputCount = 2, HiddenLayersInputs = new[] { 3 }
                },
                new NeuronInputCount
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Escape, InputCount = 4, OutputCount = 1,
                    HiddenLayersInputs = new[] { 1 }
                },
            };
        }
    }

    private static void InitPathfinder(ref AStarPath? pathfinder, int mountainCost = 0, int sandCost = 0)
    {
        const int normalCost = 100;
        const int maxModCost = 30;

        foreach (SimNode<IVector> node in Graph.NodesType)
        {
            node.SetCost(normalCost);
            switch (node.NodeType)
            {
                case NodeType.Lake:
                    node.SetCost(1000);
                    node.isBlocked = true;
                    break;
                case NodeType.Mountain:
                    node.SetCost(node.GetCost() + mountainCost);
                    if (mountainCost > maxModCost) node.isBlocked = true;
                    break;
                case NodeType.Sand:
                    node.SetCost(node.GetCost() + sandCost);
                    if (sandCost > maxModCost) node.isBlocked = true;
                    break;
                default:
                    break;
            }
        }

        pathfinder = new AStarPath(Graph.NodesType.Cast<SimNode<IVector>>().ToList());
    }


    private static void InitVoronois()
    {
        Voronois = new Voronoi<CoordinateNode, MyVector>[Enum.GetValues(typeof(NodeTerrain)).Length];

        foreach (NodeTerrain terrain in Enum.GetValues(typeof(NodeTerrain)))
        {
            if (terrain is NodeTerrain.Construction or NodeTerrain.WatchTower) continue;

            Voronois[(int)terrain] = new Voronoi<CoordinateNode, MyVector>();
            Voronois[(int)terrain].Init(new CoordinateNode(), Graph.MapSize, Sim2Graph.CellSize);
            
            UpdateVoronoi(terrain);
        }
    }


    private static void UpdateVoronoi(NodeTerrain terrain)
    {
        ConcurrentBag<CoordinateNode> pointsOfInterest = new ConcurrentBag<CoordinateNode>();

        List<SimNode<IVector>> nodesList = Graph.NodesType.Cast<SimNode<IVector>>().ToList();

        Parallel.ForEach(nodesList, parallelOptions, simNode =>
        {
            if (terrain == NodeTerrain.Empty) return;
            
            if (terrain == NodeTerrain.TownCenter)
            {
                if (simNode.NodeTerrain != terrain && simNode.NodeTerrain != NodeTerrain.WatchTower) return;
                
                lock (pointsOfInterest)
                {
                    pointsOfInterest.Add(Graph.CoordNodes[(int)simNode.GetCoordinate().X,
                        (int)simNode.GetCoordinate().Y]);
                }
            }
            else if (simNode.NodeTerrain == terrain)
            {
                lock (pointsOfInterest)
                {
                    pointsOfInterest.Add(Graph.CoordNodes[(int)simNode.GetCoordinate().X,
                        (int)simNode.GetCoordinate().Y]);
                }
            }
        });

        Voronois[(int)terrain].SetVoronoi(pointsOfInterest.ToList());
    }

    public static INode<IVector> CoordinateToNode(IVector coordinate)
    {
        if (coordinate.X < 0 || coordinate.Y < 0 || coordinate.X >= Graph.MaxX || coordinate.Y >= Graph.MaxY)
        {
            return null;
        }

        return Graph.NodesType[(int)coordinate.X, (int)coordinate.Y];
    }

    public static INode<IVector> GetNearestNode(NodeType nodeType, IVector position)
    {
        INode<IVector> nearestNode = null;
        float minDistance = float.MaxValue;

        foreach (SimNode<IVector> node in Graph.NodesType)
        {
            if (node.NodeType != nodeType) continue;

            float distance = IVector.Distance(position, node.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;

            nearestNode = node;
        }

        return nearestNode;
    }

    public static INode<IVector> GetNearestNode(NodeTerrain nodeTerrain, IVector position)
    {
        INode<IVector> nearestNode = null;
        float minDistance = float.MaxValue;

        foreach (SimNode<IVector> node in Graph.NodesType)
        {
            if (node.NodeTerrain != nodeTerrain) continue;

            float distance = IVector.Distance(position, node.GetCoordinate());

            if (minDistance < distance) continue;

            minDistance = distance;

            nearestNode = node;
        }

        return nearestNode;
    }

    public static AnimalAgent<IVector, ITransform<IVector>> GetNearestEntity(AgentTypes entityType,
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

        foreach (KeyValuePair<uint, AnimalAgent<IVector, ITransform<IVector>>> prey in Animals)
        {
            AnimalAgent<IVector, ITransform<IVector>>? agent = prey.Value;
            if (agent.agentType != AgentTypes.Herbivore) continue;

            float distance = IVector.Distance(position, agent.CurrentNode.GetCoordinate());

            if (distance > minDistance) continue;
            minDistance = distance;
            nearestAgent = prey.Key;
        }

        foreach (KeyValuePair<uint, TcAgent<IVector, ITransform<IVector>>> prey in TcAgents)
        {
            TcAgent<IVector, ITransform<IVector>>? agent = prey.Value;
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
        return isAnimal ? Animals[id].CurrentNode.GetCoordinate() : TcAgents[id].CurrentNode.GetCoordinate();
    }

    public static void Attack(uint id, bool isAnimal)
    {
        if (isAnimal)
        {
            Herbivore<IVector, ITransform<IVector>> herbivore = (Herbivore<IVector, ITransform<IVector>>)Animals[id];

            lock (herbivore)
            {
                herbivore.Hp -= 1;
            }
        }
        else
        {
            Cart? cart = (Cart)TcAgents[id];

            lock (cart)
            {
                cart.Attacked();
            }
        }
    }

    public static int GetBrainTypeKeyByValue(BrainType value, AgentTypes agentType)
    {
        Dictionary<int, BrainType> brainTypes = agentType switch
        {
            AgentTypes.Carnivore => CarnBrainTypes,
            AgentTypes.Herbivore => HerbBrainTypes,
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