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
            Action OnGather = parameters[1] as Action;
            float[] outputs = parameters[2] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () => { OnGather?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (outputs[1] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnFull);
                    return;
                }
                if (outputs[2] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnTargetLost);
                    return;
                }
                if (outputs[3] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
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
            BehaviourActions behaviours = new BehaviourActions();

            INode<IVector>? adjacentNode = (parameters[0]) as INode<IVector>;
            behaviours.AddMultiThreadableBehaviours(0, () => { adjacentNode.IsOccupied = false; });

            return behaviours;
        }
    }
}