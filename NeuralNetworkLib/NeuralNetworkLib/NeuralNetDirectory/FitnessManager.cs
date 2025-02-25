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
    public FitnessManager(Dictionary<uint, AnimalAgent<TVector, TTransform>> animalAgents)
    {
        _animalAgents = animalAgents;
    }

    public void Tick()
    {
        Parallel.ForEach(_animalAgents.Keys, agentId =>
        {
            AnimalAgent<TVector, TTransform>? agent = _animalAgents[agentId];
            CalculateAnimalsFitness(agent.agentType, agentId);
        });

        Parallel.ForEach(_tcAgents.Keys, agentId =>
        {
            TcAgent<TVector, TTransform>? agent = _tcAgents[agentId];
            CalculateTcFitness(agent.AgentType, agentId);
        });
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

        if (!IsMovingTowardsTarget(agentId, targetPosition))
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
        IVector target = agent.FoodPosition.GetCoordinate();

        if (!IsMovingTowardsTarget(agentId, targetPosition))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }

        if (IsMovingTowardsTarget(agentId, target))
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

        if (IsMovingTowardsTarget(agentId, targetPosition))
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

        bool movingToPrey = IsMovingTowardsTarget(agentId, herbPosition);

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
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
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
        else
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
        else
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void BuilderMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target))
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
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Movement:
                    CartMovementFC(agentId, nnComponent);
                    break;
                case BrainType.Gather:
                    CartGatherFC(agentId, nnComponent);
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

        if (isMaintainingDistance || isAligningWithFlock || IsMovingTowardsTarget(agentId, targetPosition))
        {
            Reward(nnComponent, reward, BrainType.Flocking);
        }

        if (isColliding || !IsMovingTowardsTarget(agentId, targetPosition))
        {
            Punish(nnComponent, punishment, BrainType.Flocking);
        }
    }

    private void CartMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);
        IVector target = agent._target.CurrentNode.GetCoordinate();

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector predatorPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agentId, predatorPosition))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }

        if (IsMovingTowardsTarget(agentId, target))
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void CartGatherFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;
        if (agent.CurrentNode.NodeTerrain != NodeTerrain.TownCenter) return;
        switch (agent.resourceCarrying)
        {
            case ResourceType.Gold:
                HandleResource(nnComponent, agent.CurrentGold, 30, agent.TownCenter.Gold, 2);
                break;
            case ResourceType.Wood:
                HandleResource(nnComponent, agent.CurrentWood, 30, agent.TownCenter.Wood, 2);
                break;
            case ResourceType.Food:
                HandleResource(nnComponent, agent.CurrentFood, 30, agent.TownCenter.Food, 3);
                break;
            case ResourceType.None:
                Punish(nnComponent, punishment, BrainType.Gather);
                break;
            default:
                throw new ArgumentException("Cart Gatherer, not a resource type");
        }
    }

    private void HandleResource(NeuralNetComponent nnComponent, int resource, int resourceLimit, int tcResource, int minResource)
    {
        if (resource < resourceLimit || tcResource > 0 && resource < minResource)
        {
            Reward(nnComponent, reward, BrainType.Movement);
        }
        else if (tcResource <= 0)
        {
            Punish(nnComponent, punishment, BrainType.Movement);
        }
    }

    private void CartDeliverFC(uint agentId, NeuralNetComponent nnComponent)
    {
        Cart agent = _tcAgents[agentId] as Cart;
        if ((agent.CurrentFood > 0 || agent.CurrentGold > 0 || agent.CurrentWood > 0) &&
            agent.CurrentNode.GetCoordinate().Adyacent(agent._target.CurrentNode.GetCoordinate()))
        {
            Reward(nnComponent, reward, BrainType.Deliver);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Deliver);
        }
    }

    private void CartReturnResourcesFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target) &&
            (agent.CurrentFood > 0 || agent.CurrentGold > 0 || agent.CurrentWood > 0))
        {
            Reward(nnComponent, reward, BrainType.ReturnResources);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.ReturnResources);
        }
    }

    private void CartWaitFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat)
        {
            Reward(nnComponent, reward, BrainType.Wait);
        }
        else
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void GathererFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
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
        else
        {
            Punish(nnComponent, punishment, BrainType.Wait);
        }
    }

    private void GathererMovementFC(uint agentId, NeuralNetComponent nnComponent)
    {
        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target))
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
        Gatherer agent = _tcAgents[agentId] as Gatherer;
        if (agent is { CurrentFood: <= 0 } or { CurrentFood: >= 15, ResourceGathering: ResourceType.Food } or
            { CurrentGold: >= 15 } or { CurrentWood: >= 15 })
        {
            Punish(nnComponent, punishment, BrainType.Gather);
        }
        else
        {
            Reward(nnComponent, reward, BrainType.Gather);
        }
    }


    private void EatFitnessCalculator(uint agentId, NeuralNetComponent nnComponent)
    {
        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];

        if (agent.Food <= 0) return;

        float rewardMod = (float)agent.Food * 2 / agent.FoodLimit;
        Reward(nnComponent, reward * rewardMod, BrainType.Eat);
    }

    private bool IsMovingTowardsTarget(uint agentId, IVector targetPosition)
    {
        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];
        IVector currentPosition = agent.Transform.position;
        IVector agentDirection = agent.Transform.forward;

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
        
        ref float fitnessMod = ref neuralNetComponent.FitnessMod[id];
        fitnessMod = Math.Min(fitnessMod * FitnessModIncrement, MaxFitnessMod);
        neuralNetComponent.Fitness[id] += reward * fitnessMod;
    }

    private void Punish(NeuralNetComponent neuralNetComponent, float punishment, BrainType brainType)
    {
        const float mod = 0.9f;
        (BrainType brainType, AgentTypes AgentType) key = (brainType, neuralNetComponent.Layers[0][0].AgentType);
        if (!_brainTypeKeyCache.TryGetValue(key, out int id))
        {
            id = DataContainer.GetBrainTypeKeyByValue(brainType, neuralNetComponent.Layers[0][0].AgentType);
            _brainTypeKeyCache[key] = id;
        }

        ref float fitnessMod = ref neuralNetComponent.FitnessMod[id];
        fitnessMod *= mod;
        neuralNetComponent.Fitness[id] /= punishment + 0.05f * fitnessMod;
    }

    private float IncreaseFitnessMod(float fitnessMod)
    {
        const float maxFitness = 2;
        const float mod = 1.1f;
        fitnessMod *= mod;
        if (fitnessMod > maxFitness) fitnessMod = maxFitness;
        return fitnessMod;
    }
}