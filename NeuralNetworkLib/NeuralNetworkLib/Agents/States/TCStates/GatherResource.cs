using System.Numerics;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class GatherResource : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = Convert.ToBoolean(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int gold = Convert.ToInt32(parameters[2]);
            int goldLimit = Convert.ToInt32(parameters[3]);
            Action OnMine = parameters[4] as Action;
            SimNode<IVector> targetNode = parameters[5] as SimNode<IVector>;
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                OnMine?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat) OnFlag?.Invoke(Flags.OnRetreat);
                if (food <= 0) OnFlag?.Invoke(Flags.OnHunger);
                if (gold >= goldLimit) OnFlag?.Invoke(Flags.OnFull);
                if(targetNode.Resource <= 0) OnFlag?.Invoke(Flags.OnTargetLost);
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            
            Action<SimNode<IVector>> onReachMine = parameters[0] as Action<SimNode<IVector>>;
            SimNode<IVector> currentNode = parameters[1] as SimNode<IVector>;
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onReachMine?.Invoke(currentNode);
            });
            
            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            
            Action<SimNode<IVector>> onLeaveMine = parameters[0] as Action<SimNode<IVector>>;
            SimNode<IVector> currentNode = parameters[1] as SimNode<IVector>;
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onLeaveMine?.Invoke(currentNode);
            });
            
            return behaviours;
        }
    }
}