using System.Numerics;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.GraphDirectory;
using NeuralNetworkLib.Utils;
using Pathfinder;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class WalkState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });


            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat && (targetNode is null || targetNode.NodeTerrain != NodeTerrain.TownCenter))
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }


                if (currentNode == null || targetNode == null ||
                    targetNode is { NodeTerrain: NodeTerrain.Mine, Resource: <= 0 } ||
                    targetNode.NodeTerrain == NodeTerrain.Empty)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (!currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()))
                {
                    return;
                }

                switch (currentNode.NodeTerrain)
                {
                    case NodeTerrain.Mine:
                    case NodeTerrain.Lake:
                    case NodeTerrain.Tree:
                        OnFlag?.Invoke(Flags.OnGather);
                        break;
                    case NodeTerrain.TownCenter:
                        OnFlag?.Invoke(Flags.OnWait);
                        break;
                    case NodeTerrain.Empty:
                    default:
                        OnFlag?.Invoke(Flags.OnTargetLost);
                        break;
                }
            });

            return behaviours;
        }

        public override BehaviourActions GetOnEnterBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            List<SimNode<IVector>> path = (List<SimNode<IVector>>)parameters[2];
            Pathfinder<SimNode<IVector>, IVector, CoordinateNode> pathfinder =
                parameters[3] as Pathfinder<SimNode<IVector>, IVector, CoordinateNode>;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode != null && targetNode != null)
                    path = pathfinder.FindPath(currentNode, targetNode);
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            int? pathNodeId = (int?)(parameters[0]);
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                pathNodeId = 0;
            });
            
            return behaviours;
        }
    }

    public class CartWalkState : WalkState
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            SimNode<IVector> targetNode = parameters[1] as SimNode<IVector>;
            bool retreat = (bool)parameters[2];
            Action onMove = parameters[3] as Action;
            bool returnResource = (bool)parameters[4];

            behaviours.AddMultiThreadableBehaviours(0, () => { onMove?.Invoke(); });


            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat && (targetNode is null || targetNode.NodeTerrain != NodeTerrain.TownCenter))
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (currentNode == null || targetNode == null ||
                    targetNode is { NodeTerrain: NodeTerrain.Mine, Resource: <= 0 } ||
                    targetNode.NodeTerrain == NodeTerrain.Empty)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }

                if (currentNode.GetCoordinate().Adyacent(targetNode.GetCoordinate()))
                {
                    return;
                }
                
                if(returnResource && targetNode.NodeTerrain == NodeTerrain.TownCenter)
                {
                    OnFlag?.Invoke(Flags.OnReturnResource);
                    return;
                }
                
                if(currentNode.NodeTerrain == NodeTerrain.TownCenter)
                {
                    OnFlag?.Invoke(Flags.OnGather);
                    return;
                }
                
                OnFlag?.Invoke(Flags.OnTargetReach);
               
            });

            return behaviours;
        }
    }
}