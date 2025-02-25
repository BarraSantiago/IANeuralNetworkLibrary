using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class WaitState : State
    {
        private static readonly int GoldCost = 2;
        private static readonly int WoodCost = 4;

        private static readonly NodeTerrain[] SafeRetreatTerrains = { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;
            float buildOutput = (float)parameters[3];
            float walkOutput = (float)parameters[4];


            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                        OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (buildOutput > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnBuild);
                    return;
                }

                if (walkOutput > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }
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
        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;
            float outputs = (float)parameters[3];


            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, currentNode, outputs));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode, float outputs)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                    OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (outputs > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnGather);
                return;
            }
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
        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;
            float[] outputs = parameters[3] as float[];

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, currentNode, outputs));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode, float[] outputs)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1) OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (outputs[0] > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnReturnResource);
            }
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