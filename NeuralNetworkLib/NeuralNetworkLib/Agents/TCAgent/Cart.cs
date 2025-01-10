using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Cart : TcAgent<IVector, ITransform<IVector>>
    {
        private TcAgent<IVector, ITransform<IVector>> _target;
        private Action onGather;
        private Action onDeliver;

        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Cart;
            Fsm.ForceTransition(Behaviours.GatherResources);
            onGather += Gather;
            onDeliver += DeliverFood;
        }

        private void Gather()
        {
            CurrentFood++;
        }

        private void DeliverFood()
        {
            if (CurrentFood <= 0) return;

            CurrentFood--;
            _target.CurrentFood++;
        }

        protected override void FsmBehaviours()
        {
            base.FsmBehaviours();
            Fsm.AddBehaviour<GetFoodState>(Behaviours.GatherResources, GetFoodTickParameters);
            Fsm.AddBehaviour<DeliverFoodState>(Behaviours.Deliver, DeliverTickParameters);
        }

        protected override void FsmTransitions()
        {
            base.FsmTransitions();
            GetFoodTransitions();
            WalkTransitions();
            DeliverTransitions();
        }

        protected override void GetFoodTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
                () =>
                {
                    // TODO set target Node or agent
                });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.TownCenter); });
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.TownCenter); });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    // TODO set target Node or agent
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.Deliver);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.GatherResources);
        }

        protected override object[] GetFoodTickParameters()
        {
            return new object[] { CurrentFood, FoodLimit, onGather, Retreat };
        }

        protected override object[] GatherTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, ResourceLimit, onGather };
        }

        protected override void DeliverTransitions()
        {
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnHunger, Behaviours.Walk,
                () =>
                {
                    TargetNode = DataContainer.graph.NodesType[(int)TownCenter.position.GetCoordinate().X,
                        (int)TownCenter.position.GetCoordinate().Y];
                });
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = DataContainer.graph.NodesType[(int)TownCenter.position.GetCoordinate().X,
                        (int)TownCenter.position.GetCoordinate().Y];
                });
        }

        private object[] DeliverTickParameters()
        {
            return new object[] { CurrentFood, onDeliver, Retreat };
        }

        protected SimNode<IVector> GetTarget()
        {
            // TODO get target
            return null;
        }
    }
}