using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.ECS.NeuralNetECS;
using NeuralNetworkLib.NeuralNetDirectory.ECS;
using NeuralNetworkLib.NeuralNetDirectory.ECS.Patron;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.NeuralNetDirectory
{
    public class FitnessManager<TVector, TTransform>
        where TTransform : ITransform<IVector>, new()
        where TVector : IVector, IEquatable<TVector>
    {
        private static Dictionary<uint, AnimalAgent<TVector, TTransform>> _agents;

        public FitnessManager(Dictionary<uint, AnimalAgent<TVector, TTransform>> agents)
        {
            _agents = agents;
        }

        public void Tick()
        {
            foreach (KeyValuePair<uint, AnimalAgent<TVector, TTransform>> agent in _agents)
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
            foreach (KeyValuePair<int, BrainType> brainType in _agents[agentId].brainTypes)
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

            Herbivore<IVector, ITransform<IVector>> agent = _agents[agentId] as Herbivore<IVector, ITransform<IVector>>;
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

            AnimalAgent<TVector, TTransform> agent = _agents[agentId];
            AnimalAgent<IVector, ITransform<IVector>> nearestPredatorNode =
                DataContainer.GetNearestEntity(AgentTypes.Carnivore, agent.Transform.position);

            if (nearestPredatorNode?.CurrentNode?.GetCoordinate() == null) return;
            IVector targetPosition = nearestPredatorNode.CurrentNode.GetCoordinate();

            if (!IsMovingTowardsTarget(agentId, targetPosition))
            {
                Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId),reward, BrainType.Movement);
            }
            else
            {
                Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId),punishment, BrainType.Movement);
            }
        }

        private void CarnivoreFitnessCalculator(uint agentId)
        {
            foreach (KeyValuePair<int, BrainType> brainType in _agents[agentId].brainTypes)
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

            Carnivore<TVector, TTransform> agent = (Carnivore<TVector, TTransform>)_agents[agentId];
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

                Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId),reward * rewardMod, BrainType.Attack);
            }
            else
            {
                Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId),punishment, BrainType.Attack);
            }
        }

        private void CarnivoreMovementFC(uint agentId)
        {
            const float reward = 10;
            const float punishment = 0.90f;

            AnimalAgent<TVector, TTransform> agent = _agents[agentId];
            (uint, bool) nearestPrey = DataContainer.GetNearestPrey(agent.Transform.position);
            
            IVector herbPosition = DataContainer.GetPosition(nearestPrey.Item1, nearestPrey.Item2);

            bool movingToPrey = IsMovingTowardsTarget(agentId, herbPosition);
            
            if (movingToPrey)
            {
                float rewardMod = movingToPrey ? 1.15f : 0.9f;
                
                Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId),reward * rewardMod, BrainType.Movement);
            }
            else
            {
                Punish(ECSManager.GetComponent<NeuralNetComponent>(agentId),punishment, BrainType.Movement);
            }
        }
        
        
        private void EatFitnessCalculator(uint agentId)
        {
            const float reward = 10;
            const float punishment = 0.90f;

            AnimalAgent<TVector, TTransform> agent = _agents[agentId];

            if (agent.Food <= 0) return;

            float rewardMod = (float)agent.Food * 2 / agent.FoodLimit;
            Reward(ECSManager.GetComponent<NeuralNetComponent>(agentId),reward * rewardMod, BrainType.Eat);
        }

        private bool IsMovingTowardsTarget(uint agentId, IVector targetPosition)
        {
            AnimalAgent<TVector, TTransform> agent = _agents[agentId];
            IVector currentPosition = agent.Transform.position;
            IVector agentDirection = agent.Transform.forward;

            if (targetPosition == null || currentPosition == null) return false;
            IVector directionToTarget = (targetPosition - currentPosition).Normalized();
            if(directionToTarget == null || agentDirection == null) return false;
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
}