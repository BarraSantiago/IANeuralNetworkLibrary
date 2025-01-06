using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Gatherer : TcAgent
    {
        private Action onMine;
        public static Action OnEmptyMine;
        public static Action<SimNode<IVector>> OnReachMine;
        public static Action<SimNode<IVector>> OnLeaveMine;
        protected const int GoldPerFood = 3;
        protected const int FoodPerFood = 3;
        protected const int WoodPerFood = 5;

        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Gatherer;
            Fsm.ForceTransition(Behaviours.Walk);
            onMine += Gather;
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
            Fsm.AddBehaviour<GatherResource>(Behaviours.GatherResources, GatherTickParameters, GatherEnterParameters, GatherLeaveParameters);
        }

        protected override void GatherTransitions()
        {
            base.GatherTransitions();
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Wait);

            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.TownCenter);
                });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget();
                });
        }

        protected override object[] GatherTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, ResourceLimit, onMine, CurrentNode };
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
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources);
        }
        
        protected override object[] WaitEnterParameters()
        {
            return new object[] { CurrentNode, OnReachMine };
        }
        
        protected override object[] WaitExitParameters()
        {
            return new object[] { CurrentNode, OnLeaveMine };
        }

        protected override void Wait()
        {
            base.Wait();
            if (CurrentNode.NodeTerrain != NodeTerrain.TownCenter) return;

            if (CurrentGold > 0)
            {
                CurrentGold--;
                TownCenter.Gold++;
            }
            if (CurrentWood > 0)
            {
                CurrentWood--;
                TownCenter.Wood++;
            }
            if (CurrentFood > 3)
            {
                CurrentFood--;
                TownCenter.Food++;
            }
        }

        private void Gather()
        {
            if (CurrentFood <= 0 || CurrentNode.Resource <= 0) return;

            CurrentGold++;

            LastTimeEat++;
            CurrentNode.Resource--;
            if (CurrentNode.Resource <= 0) OnEmptyMine?.Invoke();

            if (LastTimeEat < GoldPerFood) return;

            CurrentFood--;
            LastTimeEat = 0;

            if (CurrentFood > 0 || CurrentNode.food <= 0) return;

            CurrentFood++;
            CurrentNode.food--;
        }
    }
}