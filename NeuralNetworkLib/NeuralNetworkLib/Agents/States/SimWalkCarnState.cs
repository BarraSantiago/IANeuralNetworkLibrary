using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States
{
    public class SimWalkCarnState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            if (parameters.Length < 6) return default;

            if (parameters[0] is not SimNode<IVector> currentNode) return default;
            if (parameters[1] is not IVector target) return default;
            if (parameters[2] is not SimNodeType foodTarget) return default;
            if (parameters[3] is not Action onMove) return default;
            if (parameters[4] is not float[] outputBrain1) return default;
            if (parameters[5] is not float[] outputBrain2) return default;

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onMove?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (outputBrain1[0] > 0.5f && currentNode != null && currentNode.NodeType == foodTarget)
                {
                    OnFlag?.Invoke(Flags.OnEat);
                    return;
                }

                if (outputBrain2[0] > 0.5f && Approximatly(target, currentNode?.GetCoordinate(), 0.2f))
                {
                    OnFlag?.Invoke(Flags.OnAttack);
                    return;
                }
            });

            return behaviours;
        }

        private bool Approximatly(IVector coord1, IVector coord2, float tolerance)
        {
            if (coord1 == null || coord2 == null) return false;
            return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
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