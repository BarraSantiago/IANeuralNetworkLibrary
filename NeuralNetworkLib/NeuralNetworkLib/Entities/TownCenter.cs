using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Entities;

public struct CreationCost
{
    public int Gold;
    public int Wood;
    public int Food;

    public CreationCost Sum(CreationCost a)
    {
        return new CreationCost
        {
            Gold = a.Gold + Gold,
            Wood = a.Wood + Wood,
            Food = a.Food + Food
        };
    }
}

public class TownCenter
{
    public SimNode<IVector> Position;
    public List<TcAgent<IVector, ITransform<IVector>>> Agents;
    public List<(TcAgent<IVector, ITransform<IVector>>, ResourceType)> AgentsResources;
    public Action<int, TownCenter, AgentTypes> OnSpawnUnit;

    public int RefugeeCount
    {
        get => GetRefugeeCountAsync().Result;
        set => _ = SetRefugeeCountAsync(value);
    }

    private const int AlarmDuration = 10;
    private int _refugeeCount;
    private int _maxWtDistance = 25;
    private int _gold;
    private int _wood;
    private int _food;
    public int InitialCarts = 1;
    public int InitialBuilders = 1;
    public int InitialGatherer = 5;
    private int _gathererCount = 5;
    private List<SimNode<IVector>> WatchTowerConstructions = new List<SimNode<IVector>>();
    private List<SimNode<IVector>> WatchTowerPositions = new List<SimNode<IVector>>();

    private Dictionary<ResourceType, int> _gatherersPerResource = new()
    {
        { ResourceType.None, 0 },
        { ResourceType.Food, 0 },
        { ResourceType.Gold, 0 },
        { ResourceType.Wood, 0 }
    };

    public int Gold
    {
        get => _gold;
        set => _gold = value;
    }

    public int Wood
    {
        get => _wood;
        set => _wood = value;
    }

    public int Food
    {
        get => _food;
        set => _food = value;
    }

    public CreationCost CartCost = new CreationCost { Gold = 20, Wood = 30, Food = 5 };
    public CreationCost BuilderCost = new CreationCost { Gold = 8, Wood = 15, Food = 10 };
    public CreationCost GathererCost = new CreationCost { Gold = 5, Wood = 2, Food = 10 };
    public CreationCost WatchTowerCost = new CreationCost { Gold = 200, Wood = 400, Food = 0 };
    public CreationCost WatchTowerBuildCost = new CreationCost { Gold = 2, Wood = 4, Food = 0 };

    public TownCenter(SimNode<IVector> position)
    {
        Position = position;
        AgentsResources = new List<(TcAgent<IVector, ITransform<IVector>>, ResourceType)>();
        WatchTowerConstructions = new List<SimNode<IVector>>();
        WatchTowerPositions = new List<SimNode<IVector>>();
        GetWatchTowerConstruction();
        _gold = 0;
        _wood = 0;
        _food = 0;
    }

    #region UnitSpawn

    public void ManageSpawning()
    {
        if (Gold < GathererCost.Gold || Wood < GathererCost.Wood || Food < GathererCost.Food) return;

        if (_gathererCount % 3 == 0 && !HasEnoughResources(BuilderCost.Sum(CartCost.Sum(GathererCost))))
        {
            return;
        }

        SpawnGatherer();
        _gathererCount++;

        if (_gathererCount % 3 == 0)
        {
            SpawnBuilder();
            SpawnCart();
        }
    }

    // TODO Implement units spawning
    private void SpawnCart()
    {
        if (!HasEnoughResources(CartCost)) return;
        ReduceResources(CartCost);
        OnSpawnUnit?.Invoke(1, this, AgentTypes.Cart);
    }

    private void SpawnBuilder()
    {
        if (!HasEnoughResources(BuilderCost)) return;
        ReduceResources(BuilderCost);
        OnSpawnUnit?.Invoke(1, this, AgentTypes.Builder);
    }

    private void SpawnGatherer()
    {
        if (!HasEnoughResources(GathererCost)) return;
        ReduceResources(GathererCost);
        OnSpawnUnit?.Invoke(1, this, AgentTypes.Gatherer);
    }

    #endregion

    public void AskForResources(TcAgent<IVector, ITransform<IVector>> agent, ResourceType resourceNeeded)
    {
        AgentsResources.Add((agent, resourceNeeded));
    }


    public INode<IVector> GetWatchTowerConstruction()
    {
        const int maxTowerDistance = 5;
        if (WatchTowerConstructions.Count > 0)
        {
            switch (WatchTowerConstructions.First().NodeTerrain)
            {
                case NodeTerrain.Construction when
                    WatchTowerConstructions.First().GetAdjacentNode() != null:
                    IVector coord = WatchTowerConstructions.First().GetAdjacentNode();
                    return DataContainer.Graph.NodesType[(int)coord.X, (int)coord.Y];
                case NodeTerrain.WatchTower:
                    WatchTowerPositions.Add(WatchTowerConstructions.First());
                    WatchTowerConstructions.RemoveAt(0);
                    break;
            }
        }

        IVector townCenterPosition = Position.GetCoordinate();
        SimNode<IVector> node = null;

        for (int x = (int)townCenterPosition.X - _maxWtDistance; x <= (int)townCenterPosition.X + _maxWtDistance; x++)
        {
            for (int y = (int)townCenterPosition.Y - _maxWtDistance;
                 y <= (int)townCenterPosition.Y + _maxWtDistance;
                 y++)
            {
                if (x < 0 || y < 0 || x >= DataContainer.Graph.MaxX || y >= DataContainer.Graph.MaxY)
                {
                    continue;
                }

                node = DataContainer.Graph.NodesType[x, y];

                if (node.NodeType != NodeType.Plains || node.NodeTerrain == NodeTerrain.Construction ||
                    node.NodeTerrain == NodeTerrain.WatchTower) continue;

                bool isFarEnough = true;

                foreach (SimNode<IVector>? watchTower in WatchTowerPositions)
                {
                    if (IVector.Distance(node.GetCoordinate(), watchTower.GetCoordinate()) >
                        maxTowerDistance) continue;
                    isFarEnough = false;
                    break;
                }

                if (isFarEnough)
                {
                    WatchTowerConstructions.Add(node);
                    return node;
                }
            }
        }

        _maxWtDistance += 5;
        return null;
    }

    public ResourceType GetResourceNeeded()
    {
        ResourceType resourceNeeded = ResourceType.None;

        switch (_gatherersPerResource.OrderBy(x => x.Value).First().Key)
        {
            case ResourceType.Food:
                resourceNeeded = ResourceType.Food;
                _gatherersPerResource[ResourceType.Food]++;
                break;
            case ResourceType.Gold:
                resourceNeeded = ResourceType.Gold;
                _gatherersPerResource[ResourceType.Gold]++;
                break;
            case ResourceType.Wood:
                resourceNeeded = ResourceType.Wood;
                _gatherersPerResource[ResourceType.Wood]++;
                break;
            case ResourceType.None:
                break;
            default:
                throw new Exception("TownCenter: GetResourceNeeded, resource type not found");
        }

        return resourceNeeded;
    }

    private void ReduceResources(CreationCost cost)
    {
        _gold -= cost.Gold;
        _wood -= cost.Wood;
        _food -= cost.Food;
    }

    private bool HasEnoughResources(CreationCost cost)
    {
        return _gold >= cost.Gold && _wood >= cost.Wood && _food >= cost.Food;
    }

    public ResourceType RemoveFromResource(ResourceType resourceGathering)
    {
        switch (resourceGathering)
        {
            case ResourceType.Food:
                _gatherersPerResource[ResourceType.Food]--;
                break;
            case ResourceType.Gold:
                _gatherersPerResource[ResourceType.Gold]--;
                break;
            case ResourceType.Wood:
                _gatherersPerResource[ResourceType.Wood]--;
                break;
            case ResourceType.None:
                break;
            default:
                throw new Exception("TownCenter: RemoveFromResource, resource type not found");
        }

        return ResourceType.None;
    }

    public void SoundAlarm()
    {
        foreach (TcAgent<IVector, ITransform<IVector>>? agent in Agents)
        {
            agent.Retreat = true;
        }
    }

    private void CallOffAlarm()
    {
        _refugeeCount = 0;
        foreach (TcAgent<IVector, ITransform<IVector>>? agent in Agents)
        {
            agent.Retreat = false;
        }
    }

    private Task<int> GetRefugeeCountAsync()
    {
        return Task.FromResult(_refugeeCount);
    }

    private async Task SetRefugeeCountAsync(int value)
    {
        _refugeeCount = value;
        if (_refugeeCount >= Agents.Count)
        {
            await CallFunctionAfterDelay(CallOffAlarm, AlarmDuration);
        }
    }

    private async Task CallFunctionAfterDelay(Action functionToCall, int delayInSeconds)
    {
        await Task.Delay(delayInSeconds * 1000);
        functionToCall();
    }
}