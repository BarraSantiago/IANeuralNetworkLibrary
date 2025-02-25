using NeuralNetworkLib.Agents.TCAgent;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States.TCStates;

public class BuildState : State
{
    public override BehaviourActions GetTickBehaviour(params object[] parameters)
    {
        BehaviourActions behaviours = new BehaviourActions();

        bool retreat = Convert.ToBoolean(parameters[0]);
        Action OnBuild = parameters[1] as Action;
        float waitOutput = (float)parameters[2];
        float moveOutput = (float)parameters[3];


        behaviours.AddMultiThreadableBehaviours(0, () =>
        {
            OnBuild?.Invoke();
        });

        behaviours.SetTransitionBehaviour(() =>
        {
            if (retreat)
            {
                OnFlag?.Invoke(Flags.OnRetreat);
                return;
            }

            if (waitOutput > 0.5f)
            {
                OnFlag?.Invoke(Flags.OnHunger);
                return;
            }
            if (moveOutput > 0.5f)
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