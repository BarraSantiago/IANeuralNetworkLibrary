using System.Collections.Concurrent;
using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.GraphDirectory;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.NeuralNetDirectory;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.DataManagement;

using AStarPath = AStarPathfinder<SimNode<IVector>, IVector, CoordinateNode>;
using Voronoi = VoronoiDiagram<Point2D>;

public struct BrainConfiguration
{
    public AgentTypes AgentType;
    public BrainType BrainType;
    public int InputCount;
    public int OutputCount;
    public int[] HiddenLayers;
}

public class DataContainer
{
    public static Sim2DGraph Graph;
    public static NodeUpdater NodeUpdater = new();
    public static Dictionary<uint, AnimalAgent<IVector, ITransform<IVector>>> Animals = new();
    public static Dictionary<uint, TcAgent<IVector, ITransform<IVector>>> TcAgents = new();

    public static FlockingManager FlockingManager = new();
    public static Dictionary<(BrainType, AgentTypes), BrainConfiguration> InputCountCache;
    public static BrainConfiguration[]? inputCounts;
    public static Dictionary<int, BrainType> HerbBrainTypes = new();
    public static Dictionary<int, BrainType> CarnBrainTypes = new();
    public static AStarPath? GathererPathfinder;
    public static AStarPath? BuilderPathfinder;
    public static AStarPath? CartPathfinder;
    public static Voronoi[] Voronois;
    public static Action<NodeTerrain> OnUpdateVoronoi = UpdateVoronoi;
    public static FitnessStagnationManager FitnessStagnationManager = new();

    private const string FilePath = "BrainConfigurations.json";

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

        UpdateInputCache();

        InitPathfinder(ref GathererPathfinder, 0, 0);
        InitPathfinder(ref CartPathfinder, 5, 1);
        InitPathfinder(ref BuilderPathfinder, 5, 0);
        InitVoronois();
    }

    public static void UpdateInputCache()
    {
        InputCountCache = inputCounts.ToDictionary(input => (brainType: input.BrainType, agentType: input.AgentType));
    }


    private static void LoadInputCount()
    {
        inputCounts = NeuronInputCountManager.LoadNeuronInputCounts(FilePath);

        if (inputCounts == null || inputCounts.Length == 0)
        {
            inputCounts = new[]
            {
                new BrainConfiguration
                {
                    AgentType = AgentTypes.Carnivore, BrainType = BrainType.Movement, InputCount = 5,
                    OutputCount = 3, HiddenLayers = new[] { 3 }
                },
                new BrainConfiguration
                {
                    AgentType = AgentTypes.Carnivore, BrainType = BrainType.Attack, InputCount = 4,
                    OutputCount = 1, HiddenLayers = new[] { 1 }
                },
                new BrainConfiguration
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Eat, InputCount = 4, OutputCount = 1,
                    HiddenLayers = new[] { 1 }
                },
                new BrainConfiguration
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Movement, InputCount = 8,
                    OutputCount = 2, HiddenLayers = new[] { 3 }
                },
                new BrainConfiguration
                {
                    AgentType = AgentTypes.Herbivore, BrainType = BrainType.Escape, InputCount = 4, OutputCount = 1,
                    HiddenLayers = new[] { 1 }
                },
            };
        }
    }

    private static void InitPathfinder(ref AStarPath? pathfinder, int mountainCost = 0, int sandCost = 0)
    {
        const int normalCost = 5;
        const int maxModCost = 10;

        foreach (SimNode<IVector> node in Graph.NodesType)
        {
            node.SetCost(normalCost);
            switch (node.NodeType)
            {
                case NodeType.Lake:
                    node.SetCost(normalCost);
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

        pathfinder = new AStarPath(Graph.NodesType);
    }


    private static List<Node<Point2D>> allNodes = new List<Node<Point2D>>();
    private static double width = 200.0, height = 200.0;

    private static List<Point2D> boundingPolygon = new List<Point2D>
    {
        new Point2D(0, 0),
        new Point2D(width, 0),
        new Point2D(width, height),
        new Point2D(0, height)
    };

    private static void InitVoronois()
    {
        foreach (SimNode<IVector> node in DataContainer.Graph.NodesType)
        {
            allNodes.Add(new Node<Point2D>(new Point2D(node.GetCoordinate().X, node.GetCoordinate().Y),
                node.GetCost()));
        }

        Voronois = new VoronoiDiagram<Point2D>[Enum.GetValues(typeof(NodeTerrain)).Length];
        VoronoiDiagram<Point2D>.Nodes = allNodes;

        foreach (NodeTerrain terrain in Enum.GetValues(typeof(NodeTerrain)))
        {
            if (terrain is NodeTerrain.Construction or NodeTerrain.WatchTower or NodeTerrain.Empty) continue;

            List<Site<Point2D>> sites = GetSites(terrain);
            Voronois[(int)terrain] = new Voronoi(sites, boundingPolygon);
            Voronois[(int)terrain].ComputeCellWeights();
            // TODO Fix this
            //Voronois[(int)terrain].BalanceWeights(iterations: 5, step: 0.2);
        }
    }


    private static List<Site<Point2D>> GetSites(NodeTerrain terrain)
    {
        ConcurrentBag<Site<Point2D>> pointsOfInterest = new ConcurrentBag<Site<Point2D>>();

        List<SimNode<IVector>> nodesList = Graph.NodesType.Cast<SimNode<IVector>>().ToList();

        Parallel.ForEach(nodesList, parallelOptions, simNode =>
        {
            if (terrain == NodeTerrain.Empty) return;

            if (terrain == NodeTerrain.TownCenter)
            {
                if (simNode.NodeTerrain != terrain && simNode.NodeTerrain != NodeTerrain.WatchTower) return;

                lock (pointsOfInterest)
                {
                    pointsOfInterest.Add(new Site<Point2D>(new Point2D(simNode.GetCoordinate().X,
                        simNode.GetCoordinate().Y)));
                }
            }
            else if (simNode.NodeTerrain == terrain)
            {
                lock (pointsOfInterest)
                {
                    pointsOfInterest.Add(new Site<Point2D>(new Point2D(simNode.GetCoordinate().X,
                        simNode.GetCoordinate().Y)));
                }
            }
        });

        return pointsOfInterest.ToList();
    }

    public static void UpdateVoronoi(NodeTerrain terrain)
    {
        if (Voronois == null) return;
        if (terrain is NodeTerrain.Construction or NodeTerrain.Empty) return;
        terrain = terrain == NodeTerrain.WatchTower ? NodeTerrain.TownCenter : terrain;
        List<Site<Point2D>> sites = GetSites(terrain);
        Voronois[(int)terrain] = new Voronoi(sites, boundingPolygon);
        Voronois[(int)terrain].ComputeCellsStandard();
        Voronois[(int)terrain].ComputeCellWeights();
        // TODO Fix this
        //Voronois[(int)terrain].BalanceWeights(iterations: 7, step: 0.2);
    }

    public static SimNode<IVector> GetNode(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Graph.MaxX || y >= Graph.MaxY)
        {
            return null;
        }

        return Graph.NodesType[x, y];
    }

    public static SimNode<IVector> GetNode(Point2D point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Graph.MaxX || point.Y >= Graph.MaxY)
        {
            return null;
        }

        return Graph.NodesType[(int)point.X, (int)point.Y];
    }

    public static SimNode<IVector> GetNode(IVector point)
    {
        if (point.X < 0 || point.Y < 0 || point.X >= Graph.MaxX || point.Y >= Graph.MaxY)
        {
            return null;
        }

        return Graph.NodesType[(int)point.X, (int)point.Y];
    }


    public static AnimalAgent<IVector, ITransform<IVector>> GetNearestEntity(AgentTypes entityType, IVector position)
    {
        AnimalAgent<IVector, ITransform<IVector>> nearestAgent = null;
        // Use squared distance to avoid sqrt calls.
        float minDistSq = float.MaxValue;

        foreach (AnimalAgent<IVector, ITransform<IVector>>? agent in Animals.Values)
        {
            if (agent.agentType != entityType)
                continue;

            // Get the agent’s current coordinate once.
            IVector agentPos = agent.CurrentNode.GetCoordinate();
            float dx = position.X - agentPos.X;
            float dy = position.Y - agentPos.Y;
            float distSq = dx * dx + dy * dy;

            // If this agent is closer, update our best candidate.
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearestAgent = agent;
                // Optionally, if distSq is 0 we can break early.
                if (minDistSq == 0)
                    break;
            }
        }

        return nearestAgent;
    }

    public static (uint, bool) GetNearestPrey(IVector position)
    {
        uint nearestAgentId = 0;
        bool isAnimal = true;
        float minDistSq = float.MaxValue;

        // First search among herbivores in the Animals collection.
        foreach (KeyValuePair<uint, AnimalAgent<IVector, ITransform<IVector>>> kvp in Animals)
        {
            AnimalAgent<IVector, ITransform<IVector>> agent = kvp.Value;
            // Only consider herbivores.
            if (agent.agentType != AgentTypes.Herbivore)
                continue;

            IVector agentPos = agent.CurrentNode.GetCoordinate();
            float dx = position.X - agentPos.X;
            float dy = position.Y - agentPos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearestAgentId = kvp.Key;
                isAnimal = true;
                if (minDistSq == 0)
                    break;
            }
        }

        // Then search among the TcAgents.
        foreach (KeyValuePair<uint, TcAgent<IVector, ITransform<IVector>>> kvp in TcAgents)
        {
            TcAgent<IVector, ITransform<IVector>> agent = kvp.Value;
            // Only consider agents of type Cart that have some food.
            // (Assuming that “CurrentFood > 0” is required.)
            if (agent.AgentType != AgentTypes.Cart || agent.CurrentFood <= 0)
                continue;

            IVector agentPos = agent.CurrentNode.GetCoordinate();
            float dx = position.X - agentPos.X;
            float dy = position.Y - agentPos.Y;
            float distSq = dx * dx + dy * dy;

            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                nearestAgentId = kvp.Key;
                isAnimal = false;
                if (minDistSq == 0)
                    break;
            }
        }

        return (nearestAgentId, isAnimal);
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