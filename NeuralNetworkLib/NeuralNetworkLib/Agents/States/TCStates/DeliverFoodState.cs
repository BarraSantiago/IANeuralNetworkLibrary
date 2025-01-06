using NeuralNetworkLib.Agents.TCAgent;

namespace NeuralNetworkLib.Agents.States.TCStates
{
    public class DeliverFoodState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();
            int food = Convert.ToInt32(parameters[0]);
            Action onDeliverFood = parameters[1] as Action;
            bool retreat = Convert.ToBoolean(parameters[2]);
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onDeliverFood?.Invoke();    
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (food <= 0) OnFlag?.Invoke(Flags.OnHunger);
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