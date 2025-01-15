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
                    if (currentNode.NodeTerrain != NodeTerrain.TownCenter)
                    {
                        OnFlag?.Invoke(Flags.OnRetreat);
                    }
                    return;
                }

                if (currentNode.NodeType == NodeType.Empty || 
                    (currentNode.NodeTerrain is NodeTerrain.Mine or NodeTerrain.Lake or NodeTerrain.Tree 
                     && currentNode.Resource <= 0))
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (food > 0 && currentNode.NodeTerrain == NodeTerrain.Mine 
                    || currentNode.NodeTerrain == NodeTerrain.Lake 
                    || currentNode.NodeTerrain == NodeTerrain.Tree)
                {
                    OnFlag?.Invoke(Flags.OnGather);
                    return;
                }

                if (!(gold <= 0) || currentNode.NodeTerrain != NodeTerrain.TownCenter) return;
                
                OnFlag?.Invoke(Flags.OnGather);
                return;
            });

            return behaviours;
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
    public class GathererWait : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action OnWait = parameters[2] as Action;


            behaviours.AddMultiThreadableBehaviours(0, () => { OnWait?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    if (currentNode.NodeTerrain != NodeTerrain.TownCenter)
                    {
                        OnFlag?.Invoke(Flags.OnRetreat);
                    }
                    return;
                }

                OnFlag?.Invoke(Flags.OnGather);
                return;
            });

            return behaviours;
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

    public class CartWaitState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action OnWait = parameters[2] as Action;


            behaviours.AddMultiThreadableBehaviours(0, () => { OnWait?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    if (currentNode.NodeTerrain != NodeTerrain.TownCenter)
                    {
                        OnFlag?.Invoke(Flags.OnRetreat);
                    }
                    return;
                }

                OnFlag?.Invoke(Flags.OnReturnResource);
                return;
            });

            return behaviours;
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