using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class GathererWalkState : State
    {
        private static readonly NodeTerrain[] InvalidTargetTerrains =
        {
            NodeTerrain.Empty,
            NodeTerrain.Stump,
            NodeTerrain.Construction
        };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            float[] outputs = parameters[4] as float[];
            float gatherOutput = (float)parameters[5];
            float waitOutput = (float)parameters[6];


            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() => ProcessTransitions(currentNode, targetNode, retreat, outputs, gatherOutput, waitOutput));
            return behaviours;
        }

        private void ProcessTransitions(SimNode<IVector> currentNode, SimNode<IVector> targetNode, bool retreat,
            float[] outputs, float gatherOutput, float waitOutput)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidState(currentNode, targetNode))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            if (gatherOutput > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnGather);
                return;
            }

            if (waitOutput > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnWait);
                return;
            }

            if (outputs[2] > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            HandleValidTarget(targetNode);
        }

        protected bool CheckRetreat(bool retreat, SimNode<IVector> targetNode) =>
            retreat && (targetNode?.NodeTerrain != NodeTerrain.TownCenter);

        protected virtual bool IsInvalidState(SimNode<IVector> currentNode, SimNode<IVector> targetNode) =>
            currentNode == null || targetNode == null || (targetNode.Resource <= 0 && IsResourceNode(targetNode)) ||
            Array.IndexOf(InvalidTargetTerrains, targetNode.NodeTerrain) >= 0;

        private bool IsResourceNode(SimNode<IVector> node) =>
            node.NodeTerrain is NodeTerrain.Mine or NodeTerrain.Tree or NodeTerrain.Lake;

        protected bool IsAdjacentOrNear(SimNode<IVector> currentNode, SimNode<IVector> targetNode) =>
            currentNode.GetCoordinate().Adjacent(targetNode.GetCoordinate()) ||
            Approximately(currentNode.GetCoordinate(), targetNode.GetCoordinate(), 0.001f);

        private void HandleValidTarget(SimNode<IVector> targetNode)
        {
            switch (targetNode.NodeTerrain)
            {
                case NodeTerrain.Mine:
                case NodeTerrain.Lake:
                case NodeTerrain.Tree:
                    OnFlag?.Invoke(Flags.OnGather);
                    break;
                case NodeTerrain.TownCenter:
                case NodeTerrain.WatchTower:
                    OnFlag?.Invoke(Flags.OnWait);
                    break;
                default:
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    break;
            }
        }

        protected static bool Approximately(IVector a, IVector b, float tolerance) =>
            Math.Abs(a.X - b.X) <= tolerance &&
            Math.Abs(a.Y - b.Y) <= tolerance;


        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            return default;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }

    public class CartWalkState : GathererWalkState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            float[] outputs = parameters[4] as float[];

            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() =>
                ProcessCartTransitions(currentNode, targetNode, retreat, outputs));
            return behaviours;
        }

        private void ProcessCartTransitions(SimNode<IVector> currentNode, SimNode<IVector> targetNode, bool retreat,
            float[] outputs)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidState(currentNode, targetNode))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            if (outputs[0] > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnReturnResource);
                return;
            }

            if (outputs[1] > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnGather);
                return;
            }

            if (outputs[2] > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnTargetReach);
                return;
            }
        }

        protected bool IsInvalidState(SimNode<IVector> currentNode, SimNode<IVector> targetNode) =>
            currentNode == null || targetNode == null;

        protected bool CheckRetreat(bool retreat, SimNode<IVector> targetNode) =>
            retreat && (targetNode?.NodeTerrain != NodeTerrain.TownCenter &&
                        targetNode?.NodeTerrain != NodeTerrain.WatchTower);
    }

    public class BuilderWalkState : GathererWalkState
    {
        private static readonly NodeTerrain[] ValidBuilderTerrains =
        {
            NodeTerrain.Empty,
            NodeTerrain.Construction,
            NodeTerrain.WatchTower,
            NodeTerrain.TownCenter
        };

        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            float waitOutput = (float)parameters[4];
            float buildOutput = (float)parameters[5];

            behaviours.AddMultiThreadableBehaviours(0, onMove);

            behaviours.SetTransitionBehaviour(() =>
                ProcessBuilderTransitions(currentNode, targetNode, retreat, waitOutput, buildOutput));
            return behaviours;
        }

        private void ProcessBuilderTransitions(SimNode<IVector> currentNode, SimNode<IVector> targetNode, bool retreat,
            float waitOutput, float buildOutput)
        {
            if (CheckRetreat(retreat, targetNode))
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (IsInvalidBuilderState(currentNode, targetNode))
            {
                OnFlag?.Invoke(Flags.OnTargetLost);
                return;
            }

            if (!IsAdjacentOrNear(currentNode, targetNode)) return;

            if (waitOutput > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnWait);
                return;
            }

            if (buildOutput > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnBuild);
                return;
            }
        }

        private bool IsInvalidBuilderState(SimNode<IVector> currentNode, SimNode<IVector> targetNode) =>
            currentNode == null || targetNode == null ||
            Array.IndexOf(ValidBuilderTerrains, targetNode.NodeTerrain) == -1;
    }
}