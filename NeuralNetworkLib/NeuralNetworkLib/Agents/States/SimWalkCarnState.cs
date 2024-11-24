using System;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.States
{
    public class SimWalkCarnState : State
    {
        public override BehaviourActions GetTickBehaviour(params object[] parameters)
        {
            BehaviourActions behaviours = new BehaviourActions();

            SimNode<IVector> currentNode = parameters[0] as SimNode<IVector>;
            IVector target = (IVector)parameters[1];
            SimNodeType foodTarget = (SimNodeType)parameters[2];
            Action onMove = parameters[3] as Action;
            float[] outputBrain1 = parameters[4] as float[];
            float[] outputBrain2 = parameters[5] as float[];

            behaviours.AddMultiThreadableBehaviours(0, () =>
            {
                onMove?.Invoke();
            });

            behaviours.SetTransitionBehaviour(() =>
            {
                if (outputBrain1[0] > 0.5f && currentNode != null && currentNode.NodeType == foodTarget) OnFlag?.Invoke(Flags.OnEat);
                if (outputBrain2[0] > 0.5f && Approximatly(target, currentNode?.GetCoordinate(), 0.2f)) OnFlag?.Invoke(Flags.OnAttack);
            });
            return behaviours;
        }
        private bool Approximatly(IVector coord1, IVector coord2, float tolerance)
        {
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