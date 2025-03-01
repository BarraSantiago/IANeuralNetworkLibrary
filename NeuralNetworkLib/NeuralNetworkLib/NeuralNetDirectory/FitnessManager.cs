using System.Collections.Concurrent;
using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.ECS.FlockingECS;
using NeuralNetworkLib.ECS.NeuralNetECS;
using NeuralNetworkLib.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.NeuralNetDirectory;

public class FitnessManager<TVector, TTransform>
    where TTransform : ITransform<IVector>, new()
    where TVector : IVector, IEquatable<TVector>
{
    private static Dictionary<uint, AnimalAgent<TVector, TTransform>> _animalAgents;
    private static Dictionary<uint, TcAgent<TVector, TTransform>> _tcAgents;
    private readonly ConcurrentDictionary<(BrainType, AgentTypes), int> _brainTypeKeyCache = new();

    const float reward = 10;
    const float punishment = 0.90f;
    private const float MaxFitnessMod = 2f;
    private const float FitnessModIncrement = 1.1f;

    public FitnessManager(Dictionary<uint, AnimalAgent<TVector, TTransform>> animalAgents,
        Dictionary<uint, TcAgent<TVector, TTransform>> tcAgents)
    {
        _animalAgents = animalAgents;
        _tcAgents = tcAgents;
    }

    public void Tick()
    {
        Parallel.ForEach(_animalAgents.Keys,
            agentId => { CalculateAnimalsFitness(_animalAgents[agentId].agentType, agentId); });

        Parallel.ForEach(_tcAgents.Keys, agentId => { CalculateTcFitness(_tcAgents[agentId].AgentType, agentId); });
    }

    private void CalculateTcFitness(AgentTypes agentType, uint agentId)
    {
        NeuralNetComponent nnComponent = ECSManager.GetComponent<NeuralNetComponent>(agentId);

        switch (agentType)
        {
            case AgentTypes.Gatherer:
                GathererFitnessCalculator(agentId, nnComponent);
                break;
            case AgentTypes.Cart:
                CartFitnessCalculator(agentId, nnComponent);
                break;
            case AgentTypes.Builder:
                BuilderFitnessCalculator(agentId, nnComponent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(agentType), agentType, null);
        }
    }

    public void CalculateAnimalsFitness(AgentTypes agentType, uint agentId)
    {
        NeuralNetComponent nnComponent = ECSManager.GetComponent<NeuralNetComponent>(agentId);

        switch (agentType)
        {
            case AgentTypes.Carnivore:
                CarnivoreFitnessCalculator(agentId, nnComponent);
                break;
            case AgentTypes.Herbivore:
                HerbivoreFitnessCalculator(agentId, nnComponent);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(agentType), agentType, null);
        }
    }

    private void HerbivoreFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Movement:
                    HerbivoreMovementFC(agentId, nnComponent);
                    break;
                case BrainType.Eat:
                    EatFitnessCalculator(agentId, nnComponent);
                    break;
                case BrainType.Escape:
                    HerbivoreEscapeFC(agentId, nnComponent);
                    break;
                case BrainType.Attack:
                case BrainType.Flocking:
                default:
                    throw new ArgumentException("Herbivore doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void HerbivoreEscapeFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Herbivore<IVector, ITransform<IVector>> agent =
            _animalAgents[agentId] as Herbivore<IVector, ITransform<IVector>>;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent?.Transform.position);

        IVector targetPosition;

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        targetPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, targetPosition))
        {
            Reward(nnComponent, reward, BrainType.Escape);
        }

        if (agent?.Hp < 2)
        {
            Punish(nnComponent, punishment, BrainType.Escape);
        }
    }

    private void HerbivoreMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Herbivore<TVector, TTransform> agent = _animalAgents[agentId] as Herbivore<TVector, TTransform>;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector targetPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, targetPosition))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }

        if (agent.FoodPosition == null) return;

        IVector target = agent.FoodPosition.GetCoordinate();
        if (IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void CarnivoreFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Attack:
                    CarnivoreAttackFC(agentId, nnComponent);
                    break;
                case BrainType.Eat:
                    EatFitnessCalculator(agentId, nnComponent);
                    break;
                case BrainType.Movement:
                    CarnivoreMovementFC(agentId, nnComponent);
                    break;
                case BrainType.Escape:
                case BrainType.Flocking:
                default:
                    throw new ArgumentException("Carnivore doesn't have a brain type: ", nameof(brainType));
            }
        }
    }


    private void CarnivoreAttackFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Carnivore<TVector, TTransform> agent = (Carnivore<TVector, TTransform>)_animalAgents[agentId];
        AnimalAgent<IVector, ITransform<IVector>> nearestHerbivoreNode =
            DataContainer.GetNearestEntity(AgentTypes.Herbivore, agent.Transform.position);

        if (nearestHerbivoreNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector targetPosition = nearestHerbivoreNode.CurrentNode.GetCoordinate();

        if (IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, targetPosition))
        {
            float killRewardMod = agent.HasKilled ? 2 : 1;
            float attackedRewardMod = agent.HasAttacked ? 1.5f : 0;
            float damageRewardMod = (float)agent.DamageDealt * 2 / 5;
            float rewardMod = killRewardMod * attackedRewardMod * damageRewardMod;

            Reward(nnComponent, reward * rewardMod, BrainType.Attack);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Attack);
        }
    }

    private void CarnivoreMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];
        (uint, bool) nearestPrey = DataContainer.GetNearestPrey(agent.Transform.position);

        IVector herbPosition = DataContainer.GetPosition(nearestPrey.Item1, nearestPrey.Item2);

        bool movingToPrey = IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, herbPosition);

        if (movingToPrey)
        {
            float rewardMod = movingToPrey ? 1.15f : 0.9f;

            Reward(nnComponent, reward * rewardMod, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void BuilderFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _tcAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Build:
                    BuilderBuildFC(agentId, nnComponent);
                    break;
                case BrainType.Wait:
                    BuilderWaitFC(agentId, nnComponent);
                    break;
                case BrainType.Movement:
                    BuilderMovementFC(agentId, nnComponent);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId, nnComponent);
                    break;
                default:
                    throw new ArgumentException("Builder doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void BuilderBuildFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        if (agent.CurrentFood > 0 && agent is { CurrentGold: > 2, CurrentWood: > 4 })
        {
            Reward(nnComponent, reward, BrainType.Build);
        }
        else if (agent.CurrentState == Behaviours.Build)
        {
            Punish(nnComponent, punishment, BrainType.Build);
        }
    }

    private void BuilderWaitFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat && agent.CurrentNode is { NodeTerrain: NodeTerrain.TownCenter or NodeTerrain.WatchTower })
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else if (agent is { CurrentFood: <= 0, CurrentGold: <= 2, CurrentWood: <= 4 })
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else if (agent.CurrentState == Behaviours.Wait)
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void BuilderMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        if (agent.CurrentState != Behaviours.Walk) return;

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void CartFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _tcAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Movement:
                    CartMovementFC(agentId, nnComponent);
                    break;
                case BrainType.GetResources:
                    CartGetResourcesFC(agentId, nnComponent);
                    break;
                case BrainType.Deliver:
                    CartDeliverFC(agentId, nnComponent);
                    break;
                case BrainType.ReturnResources:
                    CartReturnResourcesFC(agentId, nnComponent);
                    break;
                case BrainType.Wait:
                    CartWaitFC(agentId, nnComponent);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId, nnComponent);
                    break;
                default:
                    throw new ArgumentException("Cart doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void FlockingFC(uint agentId, NeuralNetComponent nnComponent)
    {
        const float safeDistance = 0.7f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        IVector targetPosition = agent.TargetNode.GetCoordinate();
        TransformComponent component = ECSManager.GetComponent<TransformComponent>(agentId);
        bool isMaintainingDistance = true;
        bool isAligningWithFlock = true;
        bool isColliding = false;

        IVector averageDirection = null;
        int neighborCount = 0;

        foreach (ITransform<IVector> neighbor in component.NearBoids)
        {
            IVector neighborPosition = neighbor.position;
            float distance = agent.Transform.position.Distance(neighborPosition);

            if (distance < safeDistance)
            {
                isColliding = true;
                isMaintainingDistance = false;
            }

            averageDirection += neighbor.forward;
            neighborCount++;
        }

        if (neighborCount > 0)
        {
            averageDirection /= neighborCount;
            IVector agentDirection = agent.Transform.forward;
            float alignmentDotProduct = IVector.Dot(agentDirection, averageDirection.Normalized());

            if (alignmentDotProduct < 0.9f)
            {
                isAligningWithFlock = false;
            }
        }

        if (isMaintainingDistance || isAligningWithFlock ||
            IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, targetPosition))
        {
            Reward(nnComponent, reward, BrainType.Flocking);
        }

        if (isColliding || !IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, targetPosition))
        {
            Punish(nnComponent, punishment, BrainType.Flocking);
        }
    }

    private void CartMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector predatorPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, predatorPosition))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }

        if (agent.TargetAgent == null) return;
        IVector target = agent.TargetAgent.CurrentNode.GetCoordinate();
        if (IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void CartGetResourcesFC(uint agentId, NeuralNetComponent nnComponent)
    {
        const int resourceCapacity = 30;
        const int minTownCenterResource = 2;

        Cart agent = _tcAgents[agentId] as Cart;

        // Essential safety checks
        if (agent.TownCenter == null || agent.CurrentNode == null || agent.TargetNode == null)
        {
            Punish(nnComponent, punishment, BrainType.GetResources);
            return;
        }

        // Position validation
        bool isAtTownCenter = agent.CurrentNode.GetCoordinate().Adjacent(agent.TownCenter.Position.GetCoordinate());
        bool shouldGetResources = agent.CurrentState == Behaviours.GatherResources;

        // Town center resource check
        bool tcHasResources = false;
        Func<int> getTownCenterResource = () => 0;
        int resourceAmount = 0;
        switch (agent.resourceCarrying)
        {
            case ResourceType.Gold:
                tcHasResources = agent.TownCenter.Gold >= minTownCenterResource;
                getTownCenterResource = () => agent.TownCenter.Gold;
                resourceAmount = agent.CurrentGold;
                break;
            case ResourceType.Wood:
                tcHasResources = agent.TownCenter.Wood >= minTownCenterResource;
                getTownCenterResource = () => agent.TownCenter.Wood;
                resourceAmount = agent.CurrentWood;
                break;
            case ResourceType.Food:
                tcHasResources = agent.TownCenter.Food >= minTownCenterResource;
                getTownCenterResource = () => agent.TownCenter.Food;
                resourceAmount = agent.CurrentFood;
                break;
            case ResourceType.None:
                if (shouldGetResources) Punish(nnComponent, punishment, BrainType.GetResources);
                return;
            default:
                throw new ArgumentException("Invalid resource type");
        }

        bool shouldReward = isAtTownCenter && tcHasResources && resourceAmount < resourceCapacity;

        float rewardValue = CalculateDynamicReward(resourceAmount, resourceCapacity, getTownCenterResource());

        if (shouldGetResources)
        {
            if (shouldReward)
            {
                Reward(nnComponent, rewardValue, BrainType.GetResources);

                if (resourceAmount > resourceCapacity * 0.9f)
                {
                    Reward(nnComponent, rewardValue * 0.5f, BrainType.GetResources);
                }
            }
            else
            {
                float penalty = punishment * (isAtTownCenter ? 0.8f : 1.2f);
                Punish(nnComponent, penalty, BrainType.GetResources);
            }
        }
        else if (shouldReward)
        {
            Punish(nnComponent, punishment * 0.6f, BrainType.GetResources);
        }
    }

    private float CalculateDynamicReward(int currentResource, int maxCapacity, int tcResource)
    {
        // Scale reward based on both cart needs and town center availability
        float capacityRatio = currentResource / (float)maxCapacity;
        float tcResourceRatio = tcResource / (float)maxCapacity;

        return 1f + (1f - capacityRatio) + tcResourceRatio * 0.5f;
    }

    private void CartDeliverFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;
        if (agent.TargetAgent == null) return;

        IVector target = agent.TargetAgent.CurrentNode.GetCoordinate();
        bool hasResource = agent.CurrentGold + agent.CurrentWood + agent.CurrentFood > 0;
        bool movingToTarget = agent.CurrentState == Behaviours.Walk &&
                              IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target);
        bool isDelivering = agent.CurrentState == Behaviours.Deliver &&
                            agent.CurrentNode.GetCoordinate().Adjacent(target);


        if (hasResource && (isDelivering || movingToTarget))
        {
            Reward(nnComponent, reward, BrainType.Deliver);
        }
        else if (hasResource && agent.CurrentNode.GetCoordinate().Adjacent(target) &&
                 agent.CurrentState != Behaviours.Deliver)
        {
            Punish(nnComponent, punishment, BrainType.Deliver);
        }
        else if (agent.CurrentState == Behaviours.Deliver)
        {
            Punish(nnComponent, punishment, BrainType.Deliver);
        }
    }

    private void CartReturnResourcesFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;

        if (agent.returnResource)
        {
            IVector target = agent.TownCenter.Position.GetCoordinate();
            bool isAdjacent = agent.CurrentNode.GetCoordinate().Adjacent(agent.TownCenter.Position.GetCoordinate());
            bool hasResource = agent.CurrentGold + agent.CurrentWood + agent.CurrentFood > 0;
            bool isReturning = IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target);

            if ((isAdjacent && agent.CurrentState == Behaviours.ReturnResources || isReturning) && hasResource)
            {
                Reward(nnComponent, reward, BrainType.ReturnResources);
            }
            else
            {
                Punish(nnComponent, punishment, BrainType.ReturnResources);
            }
        }
        else if (agent.CurrentState == Behaviours.ReturnResources)
        {
            Punish(nnComponent, punishment, BrainType.ReturnResources);
        }
    }

    private void CartWaitFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent is
            {
                Retreat: true, TargetNode: { NodeTerrain: NodeTerrain.TownCenter or NodeTerrain.WatchTower },
                CurrentState: Behaviours.Walk or Behaviours.Wait
            })
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else if (agent is { Retreat: true, CurrentState: not Behaviours.Walk and Behaviours.Wait })
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
        else if (agent.CurrentState == Behaviours.Wait && !agent.Retreat)
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void GathererFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _tcAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Wait:
                    GathererWaitFC(agentId, nnComponent);
                    break;
                case BrainType.Movement:
                    GathererGatherFC(agentId, nnComponent);
                    break;
                case BrainType.Gather:
                    GathererMovementFC(agentId, nnComponent);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId, nnComponent);
                    break;
                default:
                    throw new ArgumentException("Gatherer doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void GathererWaitFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat && agent.CurrentNode is { NodeTerrain: NodeTerrain.TownCenter or NodeTerrain.WatchTower })
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else if (agent is { CurrentFood: <= 0 })
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else if (agent.CurrentState == Behaviours.Wait)
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void GathererMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        if (agent.CurrentState != Behaviours.Walk) return;

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agent.Transform.forward, agent.Transform.position, target))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void GathererGatherFC(uint agentId, NeuralNetComponent nnComponent)
    {
        const int resourceLimit = 15;
        Gatherer agent = _tcAgents[agentId] as Gatherer;

        bool isAdjacent = agent.CurrentNode.GetCoordinate().Adjacent(agent.TargetNode.GetCoordinate());
        bool isResourceNode = agent.TargetNode.NodeTerrain is NodeTerrain.Lake or NodeTerrain.Mine or NodeTerrain.Tree;
        bool hasFood = agent.CurrentFood > 0;
        bool resourceUnderLimit = true;
        float capacityRatio = 1;

        switch (agent.ResourceGathering)
        {
            case ResourceType.Food:
                resourceUnderLimit = agent.CurrentFood < resourceLimit;
                capacityRatio = (float)agent.CurrentFood / resourceLimit;
                break;
            case ResourceType.Gold:
                resourceUnderLimit = agent.CurrentGold < resourceLimit;
                capacityRatio = (float)agent.CurrentGold / resourceLimit;
                break;
            case ResourceType.Wood:
                resourceUnderLimit = agent.CurrentWood < resourceLimit;
                capacityRatio = (float)agent.CurrentWood / resourceLimit;
                break;
            default:
                resourceUnderLimit = false;
                break;
        }

        capacityRatio *= 10;
        bool shouldGather = isAdjacent && isResourceNode && hasFood && resourceUnderLimit;

        if (shouldGather)
        {
            if (agent.CurrentState == Behaviours.GatherResources)
            {
                Reward(nnComponent, reward + capacityRatio, BrainType.Gather);
            }
            else
            {
                Punish(nnComponent, punishment, BrainType.Gather);
            }
        }
        else if (agent.CurrentState == Behaviours.GatherResources)
        {
            Punish(nnComponent, punishment * 0.8f, BrainType.Gather);
        }
    }


    private void EatFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];

        if (agent.Food <= 0) return;

        float rewardMod = (float)agent.Food * 2 / agent.FoodLimit;
        Reward(nnComponent, reward * rewardMod, BrainType.Eat);
    }

    private bool IsMovingTowardsTarget(IVector agentDirection, IVector currentPosition, IVector targetPosition)
    {
        if (targetPosition == null || currentPosition == null) return false;
        IVector directionToTarget = (targetPosition - currentPosition).Normalized();
        if (directionToTarget == null || agentDirection == null) return false;
        float dotProduct = IVector.Dot(directionToTarget, agentDirection);

        return dotProduct > 0.9f;
    }

    private void Reward(NeuralNetComponent neuralNetComponent, float reward, BrainType brainType)
    {
        (BrainType brainType, AgentTypes AgentType) key = (brainType, neuralNetComponent.Layers[0][0].AgentType);
        if (!_brainTypeKeyCache.TryGetValue(key, out int id))
        {
            id = DataContainer.GetBrainTypeKeyByValue(brainType, neuralNetComponent.Layers[0][0].AgentType);
            _brainTypeKeyCache[key] = id;
        }

        neuralNetComponent.Fitness[id] += reward * IncreaseFitnessMod(ref neuralNetComponent.FitnessMod[id]);
    }

    private void Punish(NeuralNetComponent neuralNetComponent, float punishment, BrainType brainType)
    {
        const float modDecrement = 0.9f;
        (BrainType brainType, AgentTypes AgentType) key = (brainType, neuralNetComponent.Layers[0][0].AgentType);
        if (!_brainTypeKeyCache.TryGetValue(key, out int id))
        {
            id = DataContainer.GetBrainTypeKeyByValue(brainType, neuralNetComponent.Layers[0][0].AgentType);
            _brainTypeKeyCache[key] = id;
        }

        ref float fitnessMod = ref neuralNetComponent.FitnessMod[id];
        fitnessMod *= modDecrement;
        neuralNetComponent.Fitness[id] *= punishment + 0.05f * fitnessMod;
    }

    private float IncreaseFitnessMod(ref float fitnessMod)
    {
        return fitnessMod = Math.Min(fitnessMod * FitnessModIncrement, MaxFitnessMod);
    }
}