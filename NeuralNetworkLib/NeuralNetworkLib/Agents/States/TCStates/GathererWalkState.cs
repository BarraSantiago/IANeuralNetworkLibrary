using System.Numerics;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.GraphDirectory;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class GathererWalkState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            List<SimNode<IVector>> Path = parameters[4] as List<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });


            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat && (targetNode is null || targetNode.NodeTerrain != NodeTerrain.TownCenter))
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }


                if (currentNode == null || targetNode == null || Path is { Count: <= 0 } ||
                    targetNode is { NodeTerrain: NodeTerrain.Mine, Resource: <= 0 } or
                        { NodeTerrain: NodeTerrain.Tree, Resource: <= 0 } or
                        { NodeTerrain: NodeTerrain.Lake, Resource: <= 0 } ||
                    targetNode.NodeTerrain == NodeTerrain.Empty)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (!currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()) &&
                    !Approximately(currentNode.GetCoordinate(), targetNode.GetCoordinate(), 0.001f)) return;
                switch (targetNode.NodeTerrain)
                {
                    case NodeTerrain.Mine:
                    case NodeTerrain.Lake:
                    case NodeTerrain.Tree:
                        OnFlag?.Invoke(Flags.OnGather);
                        return;
                        break;
                    case NodeTerrain.TownCenter:
                    case NodeTerrain.WatchTower:
                        OnFlag?.Invoke(Flags.OnWait);
                        break;
                    case NodeTerrain.Empty:
                    case NodeTerrain.Stump:
                    case NodeTerrain.Construction:
                    default:
                        OnFlag?.Invoke(Flags.OnTargetLost);
                        break;
                }
            });

            return behaviours;
        }

        protected bool Approximately(IVector coord1, IVector coord2, float tolerance)
        {
            return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
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

    public class CartGathererWalkState : GathererWalkState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            bool returnResource = (bool)parameters[4];
            List<SimNode<IVector>> Path = parameters[5] as List<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });


            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat && (targetNode is null || targetNode.NodeTerrain != NodeTerrain.TownCenter
                                                   || targetNode.NodeTerrain != NodeTerrain.WatchTower))
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (currentNode == null || targetNode == null || Path is { Count: <= 0 } ||
                    targetNode is { NodeTerrain: NodeTerrain.Mine, Resource: <= 0 } ||
                    targetNode.NodeTerrain == NodeTerrain.Empty)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (!currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()) &&
                    !Approximately(currentNode.GetCoordinate(), targetNode.GetCoordinate(), 0.001f)) return;

                if (retreat && targetNode.NodeTerrain == NodeTerrain.TownCenter
                    || targetNode.NodeTerrain != NodeTerrain.WatchTower)
                {
                    OnFlag?.Invoke(Flags.OnWait);
                    return;
                }

                if (returnResource && targetNode.NodeTerrain == NodeTerrain.TownCenter)
                {
                    OnFlag?.Invoke(Flags.OnReturnResource);
                    return;
                }

                if (currentNode.NodeTerrain == NodeTerrain.TownCenter)
                {
                    OnFlag?.Invoke(Flags.OnGather);
                    return;
                }

                OnFlag?.Invoke(Flags.OnTargetReach);
                return;
            });

            return behaviours;
        }
    }

    public class BuilderGathererWalkState : GathererWalkState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            List<SimNode<IVector>> Path = parameters[4] as List<SimNode<IVector>>;

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });


            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat && (targetNode is null || targetNode.NodeTerrain != NodeTerrain.TownCenter
                                                   || targetNode.NodeTerrain != NodeTerrain.TownCenter))
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }


                if (currentNode == null || targetNode == null || Path is { Count: <= 0 } ||
                    targetNode is not
                    {
                        NodeTerrain: NodeTerrain.Empty or NodeTerrain.Construction or
                        NodeTerrain.WatchTower or NodeTerrain.TownCenter
                    })
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (!currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()) &&
                    !Approximately(currentNode.GetCoordinate(), targetNode.GetCoordinate(), 0.001f)) return;

                if (targetNode.NodeTerrain is NodeTerrain.TownCenter or NodeTerrain.WatchTower)
                {
                    OnFlag?.Invoke(Flags.OnWait);
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
}