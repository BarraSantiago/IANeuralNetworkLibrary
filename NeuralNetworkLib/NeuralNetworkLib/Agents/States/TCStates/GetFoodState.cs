using NeuralNetworkLib.Agents.TCAgent;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class GetFoodState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int food = Convert.ToInt32(parameters[0]);
            int foodLimit = Convert.ToInt32(parameters[1]);
            Action onGatherFood = parameters[2] as Action;
            bool retreat = Convert.ToBoolean(parameters[3]);

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onGatherFood?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (food >= foodLimit) OnFlag?.Invoke(Flags.OnFull);
                if (retreat) OnFlag?.Invoke(Flags.OnRetreat);
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