using NeuralNetworkLib.Agents.AnimalAgents;

namespace NeuralNetworkLib.Agents.States.AnimalStates
{
    public class AttackState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            if (parameters == null || parameters.Length < 4)
            {
                return default;
            }
            
            BehaviourActions behaviours = new BehaviourActions();

            Action? onAttack = parameters[0] as Action;
            float[] outputBrain1 = (float[])parameters[1];
            float[] outputBrain2 = (float[])parameters[2];
            float outputBrain3 = (float)parameters[3];

            if (outputBrain1 == null || outputBrain2 == null)
            {
                return default;
            }
            
            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onAttack?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (outputBrain2[0] > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnAttack);
                    return;
                }

                if (outputBrain3 > 0.5f)
                {
                    OnFlag?.Invoke(Flags.OnSearchFood);
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