using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States
{
    public class SimEatState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 5)
            {
                throw new ArgumentException("Invalid parameters for GetTickBehaviour");
            }

            BehaviourActions behaviours = new BehaviourActions();
            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNodeType foodTarget = (SimNodeType)parameters[1];
            Action onEat = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];
            float[] outputBrain2 = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (foodTarget == null || currentNode == null || onEat == null) return;
                if (currentNode.Food <= 0 || foodTarget != currentNode.NodeType) return;

                onEat?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (currentNode == null || currentNode.Food <= 0 || foodTarget != currentNode.NodeType)
                    OnFlag?.Invoke(Flags.OnSearchFood);

                if (outputBrain1 != null && outputBrain1[0] > 0.5f && currentNode != null &&
                    currentNode.NodeType == foodTarget)
                    OnFlag?.Invoke(Flags.OnEat);

                SpecialAction(outputBrain2);
            });

            return behaviours;
        }

        protected virtual void SpecialAction(float[] outputBrain2)
        {
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            return default;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }

    public class SimEatScavState : SimEatState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 4)
            {
                throw new ArgumentException("Invalid parameters for GetTickBehaviour");
            }

            BehaviourActions behaviours = new BehaviourActions();
            IVector currentPos = parameters[0] as IVector;
            SimNode<IVector> foodNode = parameters[1] as SimNode<IVector>;
            Action onEat = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];

            IVector distanceToFood = new MyVector();
            IVector maxDistance = new MyVector(4, 4);

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentPos == null || foodNode == null || onEat == null || outputBrain1 == null)
                {
                    return;
                }

                distanceToFood = new MyVector(foodNode.GetCoordinate().X - currentPos.X,
                    foodNode.GetCoordinate().Y - currentPos.Y);

                if (foodNode.Food <= 0 || foodNode.NodeType != SimNodeType.Carrion ||
                    distanceToFood.Magnitude() > maxDistance.Magnitude()) return;

                onEat?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (foodNode == null || foodNode.Food <= 0 || distanceToFood.Magnitude() > maxDistance.Magnitude())
                    OnFlag?.Invoke(Flags.OnSearchFood);

                if (outputBrain1 != null && outputBrain1[0] > 0.5f && currentPos != null &&
                    distanceToFood.Magnitude() <= maxDistance.Magnitude()) OnFlag?.Invoke(Flags.OnEat);
            });

            return behaviours;
        }
    }

    public class SimEatHerbState : SimEatState
    {
        protected override void SpecialAction(float[] outputs)
        {
            if (outputs != null && outputs[0] > 0.5f) OnFlag?.Invoke(Flags.OnEscape);
        }
    }

    public class SimEatCarnState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 5)
            {
                throw new ArgumentException("Invalid parameters for GetTickBehaviour");
            }

            BehaviourActions behaviours = new BehaviourActions();
            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNodeType foodTarget = (SimNodeType)parameters[1];
            Action onEat = parameters[2] as Action;
            float[] outputBrain1 = parameters[3] as float[];
            float[] outputBrain2 = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (foodTarget == null || currentNode == null || onEat == null) return;
                if (currentNode.Food <= 0 || foodTarget != currentNode.NodeType) return;

                onEat?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (currentNode == null || currentNode.Food <= 0 || foodTarget != currentNode.NodeType)
                {
                    OnFlag?.Invoke(Flags.OnSearchFood);
                }

                if (outputBrain1 != null && outputBrain1[0] > 0.5f && currentNode != null &&
                    currentNode.NodeType == foodTarget)
                {
                    OnFlag?.Invoke(Flags.OnEat);
                }

                SpecialAction(outputBrain2);
            });

            return behaviours;
        }

        protected void SpecialAction(float[] outputs)
        {
            if (outputs != null && outputs[0] > 0.5f) OnFlag?.Invoke(Flags.OnAttack);
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            return default;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}