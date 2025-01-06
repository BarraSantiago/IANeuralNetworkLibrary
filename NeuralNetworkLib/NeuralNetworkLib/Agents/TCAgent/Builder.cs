using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Builder : TcAgent
    {
        private Action onBuild;
        
        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Gatherer;
            Fsm.ForceTransition(Behaviours.Walk);
            onBuild += Build;
        }

        protected override void FsmTransitions()
        {
            base.FsmTransitions();
            GatherTransitions();
            WalkTransitions();
        }

        protected override void FsmBehaviours()
        {
            base.FsmBehaviours();
            Fsm.AddBehaviour<BuildState>(Behaviours.Build, GatherTickParameters);
        }

        protected object[] BuildTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, CurrentWood, onBuild, CurrentNode };
        }

        protected override void WalkTransitions()
        {
            base.WalkTransitions();
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources);
        }

        protected override object[] WaitEnterParameters()
        {
            return new object[] { CurrentNode };
        }

        protected override object[] WaitExitParameters()
        {
            return new object[] { CurrentNode };
        }

        // TODO update this
        private void Build()
        {
            if (CurrentFood <= 0 || CurrentGold <= 0 || CurrentWood <= 0) return;

            CurrentGold--;
            CurrentWood--;

            LastTimeEat++;

            if (LastTimeEat < 30) return;

            CurrentFood--;
            LastTimeEat = 0;
        }
    }
}