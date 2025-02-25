using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class DeliverResourceState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action onDeliverResource = parameters[0] as Action;
            bool retreat = Convert.ToBoolean(parameters[1]);
            float[] outputs = parameters[2] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onDeliverResource?.Invoke();    
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (outputs[2] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnHunger);
                    return;
                }
                
                if (outputs[3] > 0.5f)
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