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

    public FitnessManager(Dictionary<uint, AnimalAgent<TVector, TTransform>> animalAgents)
    {
        _animalAgents = animalAgents;
    }

    public void Tick()
    {
        foreach (KeyValuePair<uint, AnimalAgent<TVector, TTransform>> agent in _animalAgents)
        {
            CalculateFitness(agent.Value.agentType, agent.Key);
        }
    }

    public void CalculateFitness(AgentTypes agentType, uint agentId)
    {
        switch (agentType)
        {
            case AgentTypes.Carnivore:
                CarnivoreFitnessCalculator(agentId);
                break;
            case AgentTypes.Herbivore:
                HerbivoreFitnessCalculator(agentId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(agentType), agentType, null);
        }
    }

    private void HerbivoreFitnessCalculator(uint agentId)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Movement:
                    HerbivoreMovementFC(agentId);
                    break;
                case BrainType.Eat:
                    EatFitnessCalculator(agentId);
                    break;
                case BrainType.Escape:
                    HerbivoreEscapeFC(agentId);
                    break;
                case BrainType.Attack:
                case BrainType.Flocking:
                default:
                    throw new ArgumentException("Herbivore doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void HerbivoreEscapeFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        Herbivore<IVector, ITransform<IVector>> agent =
            _animalAgents[agentId] as Herbivore<IVector, ITransform<IVector>>;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent?.Transform.position);

        IVector targetPosition;

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        targetPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agentId, targetPosition))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Escape);
        }

        if (agent?.Hp < 2)
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Escape);
        }
    }

    private void HerbivoreMovementFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        Herbivore<TVector, TTransform> agent = _animalAgents[agentId] as Herbivore<TVector, TTransform>;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector targetPosition = nearestPredatorNode.CurrentNode.GetCoordinate();
        IVector target = agent.FoodPosition.GetCoordinate();

        if (!IsMovingTowardsTarget(agentId, targetPosition))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }

        if (IsMovingTowardsTarget(agentId, target))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }
    }

    private void CarnivoreFitnessCalculator(uint agentId)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Attack:
                    CarnivoreAttackFC(agentId);
                    break;
                case BrainType.Eat:
                    EatFitnessCalculator(agentId);
                    break;
                case BrainType.Movement:
                    CarnivoreMovementFC(agentId);
                    break;
                case BrainType.Escape:
                case BrainType.Flocking:
                default:
                    throw new ArgumentException("Carnivore doesn't have a brain type: ", nameof(brainType));
            }
        }
    }


    private void CarnivoreAttackFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

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

            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward * rewardMod, BrainType.Attack);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Attack);
        }
    }

    private void CarnivoreMovementFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];
        (uint, bool) nearestPrey = DataContainer.GetNearestPrey(agent.Transform.position);

        IVector herbPosition = DataContainer.GetPosition(nearestPrey.Item1, nearestPrey.Item2);

        bool movingToPrey = IsMovingTowardsTarget(agentId, herbPosition);

        if (movingToPrey)
        {
            float rewardMod = movingToPrey ? 1.15f : 0.9f;

            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward * rewardMod, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }
    }

    private void BuilderFitnessCalculator(uint agentId)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Build:
                    BuilderBuildFC(agentId);
                    break;
                case BrainType.Wait:
                    BuilderWaitFC(agentId);
                    break;
                case BrainType.Movement:
                    BuilderMovementFC(agentId);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId);
                    break;
                default:
                    throw new ArgumentException("Builder doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void BuilderBuildFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        if (agent.CurrentFood > 0 && agent is { CurrentGold: > 2, CurrentWood: > 4 } )
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Build);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Build);
        }
    }

    private void BuilderWaitFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat && agent.CurrentNode is { NodeTerrain: NodeTerrain.TownCenter or NodeTerrain.WatchTower })
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Wait);
        }
        else if (agent is { CurrentFood: <= 0, CurrentGold: <= 2, CurrentWood: <= 4 })
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Wait);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Wait);
        }
    }

    private void BuilderMovementFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }
    }

    private void CartFitnessCalculator(uint agentId)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Movement:
                    CartMovementFC(agentId);
                    break;
                case BrainType.Gather:
                    CartGatherFC(agentId);
                    break;
                case BrainType.Deliver:
                    CartDeliverFC(agentId);
                    break;
                case BrainType.ReturnResources:
                    CartReturnResourcesFC(agentId);
                    break;
                case BrainType.Wait:
                    CartWaitFC(agentId);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId);
                    break;
                default:
                    throw new ArgumentException("Cart doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void FlockingFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;
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
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Flocking);
        }

        if (isColliding || !IsMovingTowardsTarget(agentId, targetPosition))
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Flocking);
        }
    }

    private void CartMovementFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        Cart agent = _tcAgents[agentId] as Cart;
        AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
            DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);
        IVector target = agent._target.CurrentNode.GetCoordinate();

        if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
        IVector predatorPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

        if (!IsMovingTowardsTarget(agentId, predatorPosition))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }

        if (IsMovingTowardsTarget(agentId, target))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }
    }

    private void CartGatherFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        Cart agent = _tcAgents[agentId] as Cart;
        if(agent.CurrentNode.NodeTerrain != NodeTerrain.TownCenter) return;
        switch (agent.resourceCarrying)
        {
            case ResourceType.Gold:
                HandleResource(agent.CurrentGold, 30, agent.TownCenter.Gold, 2);
                break;
            case ResourceType.Wood:
                HandleResource(agent.CurrentWood, 30, agent.TownCenter.Wood, 2);
                break;
            case ResourceType.Food:
                HandleResource(agent.CurrentFood, 30, agent.TownCenter.Food, 3);
                break;
            case ResourceType.None:
                Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Gather);
                break;
            default:
                break;
        }

        if (agent.CurrentFood < 30 || agent.CurrentGold < 30 || agent.CurrentWood < 30)
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Gather);
        }
    }
    private void HandleResource(int resource, int resourceLimit, int tcResource, int minResource)
    {
        if (resource >= resourceLimit || tcResource <= 0 && resource >= minResource)
        {
            OnFlag?.Invoke(Flags.OnFull);
        }
        else if (tcResource <= 0 && resource < minResource)
        {
            OnFlag?.Invoke(Flags.OnReturnResource);
        }
    }

    private void CartDeliverFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.CurrentFood > 0 || agent.CurrentGold > 0  || agent.CurrentWood > 0 )
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Deliver);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Deliver);
        }
    }

    private void CartReturnResourcesFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target) && (agent.CurrentFood > 0 || agent.CurrentGold > 0  || agent.CurrentWood > 0 ))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.ReturnResources);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.ReturnResources);
        }
    }

    private void CartWaitFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat)
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Wait);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Wait);
        }
    }

    private void GathererFitnessCalculator(uint agentId)
    {
        foreach (KeyValuePair<int, BrainType> brainType in _animalAgents[agentId].brainTypes)
        {
            switch (brainType.Value)
            {
                case BrainType.Wait:
                    GathererWaitFC(agentId);
                    break;
                case BrainType.Movement:
                    GathererGatherFC(agentId);
                    break;
                case BrainType.Gather:
                    GathererMovementFC(agentId);
                    break;
                case BrainType.Flocking:
                    FlockingFC(agentId);
                    break;
                default:
                    throw new ArgumentException("Gatherer doesn't have a brain type: ", nameof(brainType));
            }
        }
    }

    private void GathererWaitFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];
        if (agent.Retreat && agent.CurrentNode is { NodeTerrain: NodeTerrain.TownCenter or NodeTerrain.WatchTower })
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Wait);
        }
        else if (agent is { CurrentFood: <= 0 })
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Wait);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Wait);
        }
    }

    private void GathererMovementFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        TcAgent<TVector, TTransform> agent = _tcAgents[agentId];

        IVector target = agent.TargetNode.GetCoordinate();

        if (IsMovingTowardsTarget(agentId, target))
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Movement);
        }
        else
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Movement);
        }
    }

    private void GathererGatherFC(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        Gatherer agent = _tcAgents[agentId] as Gatherer;
        if (agent is { CurrentFood: <= 0 } or{ CurrentFood: >= 15, ResourceGathering: ResourceType.Food } or { CurrentGold: >= 15 } or { CurrentWood: >= 15 })
        {
            Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId), punishment, BrainType.Gather);
        }
        else
        {
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward, BrainType.Gather);
        }
    }


    private void EatFitnessCalculator(uint agentId)
    {
        const float reward = 10;
        const float punishment = 0.90f;

        AnimalAgent<TVector, TTransform> agent = _animalAgents[agentId];

        if (agent.Food <= 0) return;

        float rewardMod = (float)agent.Food * 2 / agent.FoodLimit;
        Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId), reward * rewardMod, BrainType.Eat);
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
        int id = DataContainer.GetBrainTypeKeyByValue(brainType, neuralNetComponent.Layers[0][0].AgentType);
        neuralNetComponent.FitnessMod[id] = IncreaseFitnessMod(neuralNetComponent.FitnessMod[id]);
        neuralNetComponent.Fitness[id] += reward * neuralNetComponent.FitnessMod[id];
    }

    private void Punish(NeuralNetComponent neuralNetComponent, float punishment, BrainType brainType)
    {
        const float mod = 0.9f;
        int id = DataContainer.GetBrainTypeKeyByValue(brainType, neuralNetComponent.Layers[0][0].AgentType);

        neuralNetComponent.FitnessMod[id] *= mod;
        neuralNetComponent.Fitness[id] /= punishment + 0.05f * neuralNetComponent.FitnessMod[id];
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