using System.Numerics;
using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    // Gatherer state that gathers resources from the map
    public class GatherResourceState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            bool retreat = Convert.ToBoolean(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int gold = Convert.ToInt32(parameters[2]);
            int wood = Convert.ToInt32(parameters[2]);
            int resourceLimit = Convert.ToInt32(parameters[3]);
            ResourceType currentResource = (ResourceType)parameters[3];
            Action OnGather = parameters[4] as Action;
            SimNode<IVector> targetNode = parameters[5] as SimNode<IVector>;

            behaviours.AddMultiThreadableBehaviours(0, () => { OnGather?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (food <= 0)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
                    return;
                }


                switch (currentResource)
                {
                    case ResourceType.Gold:
                        if (gold >= resourceLimit)
                        {
                            OnFlag?.Invoke(Flags.OnFull);
                            return;
                        }

                        break;
                    case ResourceType.Wood:
                        if (wood >= resourceLimit)
                        {
                            OnFlag?.Invoke(Flags.OnFull);
                            return;
                        }

                        break;
                    case ResourceType.Food:
                        if (food >= resourceLimit)
                        {
                            OnFlag?.Invoke(Flags.OnFull);
                            return;
                        }

                        break;
                    default:
                        throw new Exception("Gatherer: GatherResourceState, resource type not found");
                }


                if (targetNode.Resource <= 0)
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
}