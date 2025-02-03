using NeuralNetworkLib.Agents.TCAgent;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    // Cart state that gets resources from the town center 
    public class GetResourcesState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int gold = Convert.ToInt32(parameters[0]);
            int food = Convert.ToInt32(parameters[1]);
            int wood = Convert.ToInt32(parameters[2]);
            ResourceType currentResource = (ResourceType) parameters[3];
            int resourceLimit = Convert.ToInt32(parameters[4]);
            Action onGatherResources = parameters[5] as Action;
            bool retreat = Convert.ToBoolean(parameters[6]);
            
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onGatherResources?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (retreat)
                {
                    OnFlag?.Invoke(Flags.OnRetreat);
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
                    case ResourceType.None:
                        return;
                    default:
                        break;
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