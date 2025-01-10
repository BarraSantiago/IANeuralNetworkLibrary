using System.Numerics;
using NeuralNetworkLib.Agents.TCAgent;
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

                // TODO change to be 1 node away from target
                if (currentNode.GetCoordinate() != targetNode.GetCoordinate())
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
            Pathfinder<SimNode<IVector>, IVector, SimCoordinate> pathfinder =
                parameters[3] as Pathfinder<SimNode<IVector>, IVector, SimCoordinate>;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                if (currentNode != null && targetNode != null)
                    path = pathfinder.FindPath(currentNode, targetNode);
            });

            return behaviours;
        }

        public override BehaviourActions GetOnExitBehaviour(params object[] parameters)
        {
            return default;
        }
    }
}