﻿using NeuralNetworkLib.Agents.TCAgent;
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
    public SimNode<IVector> position;
    public List<TcAgent<IVector, ITransform<IVector>>> agents;
    public List<(TcAgent<IVector, ITransform<IVector>>, ResourceType)> agentsResources; 
    
    private int gold;
    private int wood;
    private int food;
    private int _initialCarts = 1;
    private int _initialBuilders = 1;
    private int _initialGatherer = 5;
    private int gathererCount = 5;
    private Dictionary<ResourceType, int> _gatherersPerResource = new()
    {
        {ResourceType.None, 0},
        {ResourceType.Food, 0},
        {ResourceType.Gold, 0},
        {ResourceType.Wood, 0}
    };
    
    public int Gold
    {
        get => gold;
        set => gold = value;
    }

    public int Wood
    {
        get => wood;
        set => wood = value;
    }

    public int Food
    {
        get => food;
        set => food = value;
    }

    public CreationCost CartCost = new CreationCost { Gold = 20, Wood = 30, Food = 5 };
    public CreationCost BuilderCost = new CreationCost { Gold = 8, Wood = 15, Food = 10 };
    public CreationCost GathererCost = new CreationCost { Gold = 5, Wood = 2, Food = 10 };
    public CreationCost WatchTowerCost = new CreationCost { Gold = 200, Wood = 400, Food = 0 };


    public TownCenter()
    {
        gold = 0;
        wood = 0;
        food = 0;
    }

    #region UnitSpawn

    public void ManageSpawning()
    {
        if (Gold < GathererCost.Gold || Wood < GathererCost.Wood || Food < GathererCost.Food) return;
        
        if (gathererCount % 3 == 0 && !HasEnoughResources(BuilderCost.Sum(CartCost.Sum(GathererCost))))
        {
            return;
        }

        SpawnGatherer();
        gathererCount++;

        if (gathererCount % 3 == 0)
        {
            SpawnBuilder();
            SpawnCart();
        }
    }

    public void SpawnInitialUnits()
    {
        for (int i = 0; i < _initialCarts; i++)
        {
            // Spawn Cart
        }

        for (int i = 0; i < _initialBuilders; i++)
        {
            // Spawn Builder
        }

        for (int i = 0; i < _initialGatherer; i++)
        {
            // Spawn Gatherer
        }
    }

    // TODO Implement units spawning
    public void SpawnCart()
    {
        if (!HasEnoughResources(CartCost)) return;
        ReduceResources(CartCost);
        // Spawn Cart
    }

    public void SpawnBuilder()
    {
        if (!HasEnoughResources(BuilderCost)) return;
        ReduceResources(BuilderCost);
        // Spawn Builder
    }

    public void SpawnGatherer()
    {
        if (!HasEnoughResources(GathererCost)) return;
        ReduceResources(GathererCost);
        // Spawn Gatherer
    }
    
    #endregion


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
    
    public void ReduceResources(CreationCost cost)
    {
        gold -= cost.Gold;
        wood -= cost.Wood;
        food -= cost.Food;
    }

    public bool HasEnoughResources(CreationCost cost)
    {
        return gold >= cost.Gold && wood >= cost.Wood && food >= cost.Food;
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
}