using NeuralNetworkLib.Agents.TCAgent;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    // Cart state that gets resources from the town center 
    public class GetResourcesState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            Action onGatherResources = parameters[0] as Action;
            bool retreat = Convert.ToBoolean(parameters[1]);
            float[] outputs = parameters[2] as float[];
           


            behaviours.AddMultiThreadableBehaviours(0, () => { onGatherResources?.Invoke(); });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
                    return;
                }

                if (outputs[0] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnFull);
                    return;
                }
                if (outputs[1] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnReturnResource);
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