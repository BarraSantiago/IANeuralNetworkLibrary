using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class WaitState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            int? food = Convert.ToInt32(parameters[1]);
            int? gold = Convert.ToInt32(parameters[2]);
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[3];
            Action OnWait = parameters[4] as Action;


            behaviours.AddMultiThreadableBehaviours(0, () => { OnWait?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    if (currentNode.NodeType != NodeType.TownCenter)
                    {
                        OnFlag?.Invoke(Flags.OnRetreat);
                    }
                    return;
                }

                if (currentNode.NodeType == NodeType.Empty || 
                    (currentNode.NodeType == NodeType.Mine && currentNode.gold <= 0))
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (food > 0 && currentNode.NodeType == NodeType.Mine)
                {
                    OnFlag?.Invoke(Flags.OnGather);
                    return;
                }

                if (gold <= 0 && currentNode.NodeType == NodeType.TownCenter)
                {
                    OnFlag?.Invoke(Flags.OnGather);
                    return;
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            Action<SimNode<IVector>> onReachMine = parameters[1] as Action<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode.NodeType == NodeType.Mine) onReachMine?.Invoke(currentNode);
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            if(parameters == null) return default;
            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            Action<SimNode<IVector>> onLeaveMine = parameters[1] as Action<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode.NodeType == NodeType.Mine) onLeaveMine?.Invoke(currentNode);
            });

            return behaviours;
        }
    }
}