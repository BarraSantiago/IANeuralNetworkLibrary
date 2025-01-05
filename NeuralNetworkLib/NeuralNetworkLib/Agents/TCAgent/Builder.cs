using System.Diagnostics;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Builder : TCAgent
    {
        private Action onMine;
        public static Action OnEmptyMine;
        public static Action<SimNode<IVector>> OnReachMine;
        public static Action<SimNode<IVector>> OnLeaveMine;

        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Gatherer;
            Fsm.ForceTransition(Behaviours.Walk);
            onMine += Mine;
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
            Fsm.AddBehaviour<GatherGoldState>(Behaviours.GatherResources, GatherTickParameters, GatherEnterParameters, GatherLeaveParameters);
        }

        protected override void GatherTransitions()
        {
            base.GatherTransitions();
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Wait,
                () => Debug.Log("Wait"));

            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.TownCenter);
                    
                    if(TargetNode == null) return;
                    
                    Debug.Log("Gold full. Walk to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget();
                    if(TargetNode == null) return;
                    Debug.Log("Mine empty. Walk to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });
        }

        protected override object[] GatherTickParameters()
        {
            return new object[] { Retreat, Food, CurrentGold, GoldLimit, onMine, CurrentNode };
        }
        
        protected object[] GatherEnterParameters()
        {
            return new object[] { OnReachMine, CurrentNode };
        }
        
        protected object[] GatherLeaveParameters()
        {
            return new object[] {OnLeaveMine, CurrentNode };
        }

        protected override void WalkTransitions()
        {
            base.WalkTransitions();
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources,
                () => Debug.Log("Gather gold"));
        }
        
        protected override object[] WaitEnterParameters()
        {
            return new object[] { CurrentNode, OnReachMine };
        }
        
        protected override object[] WaitExitParameters()
        {
            return new object[] { CurrentNode, OnLeaveMine };
        }
        
        private void Mine()
        {
            if (Food <= 0 || CurrentNode.gold <= 0) return;

            CurrentGold++;

            LastTimeEat++;
            CurrentNode.gold--;
            if (CurrentNode.gold <= 0) OnEmptyMine?.Invoke();

            if (LastTimeEat < GoldPerFood) return;

            Food--;
            LastTimeEat = 0;

            if (Food > 0 || CurrentNode.food <= 0) return;

            Food++;
            CurrentNode.food--;
        }
    }
}