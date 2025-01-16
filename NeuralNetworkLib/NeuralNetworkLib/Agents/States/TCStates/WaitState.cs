using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class WaitState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            const int goldCost = 2;
            const int woodCost = 4;
            bool retreat = (bool)parameters[0];
            int? food = Convert.ToInt32(parameters[1]);
            int? gold = Convert.ToInt32(parameters[2]);
            int? wood = Convert.ToInt32(parameters[3]);
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[4];
            SimNode<IVector> targetNode = (SimNode<IVector>)parameters[5];
            Action OnWait = parameters[6] as Action;


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

                if (food <= 0 || gold < goldCost || wood < woodCost)
                {
                    return;
                }
                
                if (targetNode.NodeTerrain != NodeTerrain.Construction)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                OnFlag?.Invoke(Flags.OnBuild);
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

    public class GathererWaitState : State
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