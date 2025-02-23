using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class WaitState : State
    {
        private static readonly int GoldCost = 2;
        private static readonly int WoodCost = 4;

        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            bool retreat = (bool)parameters[0];
            int? food = Convert.ToInt32(parameters[1]);
            int? gold = Convert.ToInt32(parameters[2]);
            int? wood = Convert.ToInt32(parameters[3]);
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[4];
            SimNode<IVector> targetNode = (SimNode<IVector>)parameters[5];
            Action onWait = parameters[6] as Action;

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, food, gold, wood, currentNode, targetNode));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, int? food, int? gold, int? wood, SimNode<IVector> currentNode,
            SimNode<IVector> targetNode)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                    OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (food > 0 && gold >= GoldCost && wood >= WoodCost)
            {
                OnFlag?.Invoke(targetNode.NodeTerrain == NodeTerrain.Construction ? Flags.OnBuild : Flags.OnTargetLost);
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

    public class GathererWaitState : State
    {
        private const int MinFood = 5;

        private static readonly NodeTerrain[] SafeRetreatTerrains =
            { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = (bool)parameters[0];
            SimNode<IVector> currentNode = (SimNode<IVector>)parameters[1];
            Action onWait = parameters[2] as Action;
            int currentFood = Convert.ToInt32(parameters[3]);

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, currentNode, currentFood));

            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode, int currentFood)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1)
                    OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (currentFood >= MinFood) OnFlag?.Invoke(Flags.OnGather);
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

            behaviours.AddMultiThreadableBehaviours(0, onWait);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(retreat, currentNode));
            
            return behaviours;
        }

        private void ProcessTransitions(bool retreat, SimNode<IVector> currentNode)
        {
            if (retreat)
            {
                if (Array.IndexOf(SafeRetreatTerrains, currentNode.NodeTerrain) == -1) OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            OnFlag?.Invoke(Flags.OnReturnResource);
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