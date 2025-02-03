using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Cart : TcAgent<IVector, ITransform<IVector>>
    {
        public ResourceType resourceCarrying;
        private TcAgent<IVector, ITransform<IVector>> _target;
        private Action onGather;
        private Action onDeliver;
        private Action onReturnResource;
        private bool returnResource;

        public override void Init()
        {
            AgentType = AgentTypes.Cart;
            base.Init();
            CurrentFood = 0;
            Fsm.ForceTransition(Behaviours.GatherResources);
            onGather += Gather;
            onDeliver += DeliverResource;
            onReturnResource += ReturnResource;
        }


        protected override void FsmBehaviours()
        {
            Fsm.AddBehaviour<CartWaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<CartWalkState>(Behaviours.Walk, WalkTickParameters, WalkEnterParameters);
            Fsm.AddBehaviour<GetResourcesState>(Behaviours.GatherResources, GatherTickParameters);
            Fsm.AddBehaviour<DeliverResourceState>(Behaviours.Deliver, DeliverTickParameters);
            Fsm.AddBehaviour<ReturnResourceState>(Behaviours.ReturnResources, ReturnTickParameters);
        }

        protected override void FsmTransitions()
        {
            base.FsmTransitions();
            GetResourcesTransitions();
            WalkTransitions();
            DeliverTransitions();
            ReturnResourceTransition();
        }

        #region Transitions

        protected override void WaitTransitions()
        {
            base.WaitTransitions();

            Fsm.SetTransition(Behaviours.Wait, Flags.OnReturnResource, Behaviours.ReturnResources);
        }

        protected override void GetResourcesTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                });
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    (TcAgent<IVector, ITransform<IVector>>, ResourceType) agentResource =
                        TownCenter.AgentsResources.First(agent => agent.Item2 == resourceCarrying);
                    if (agentResource.Item1 == null)
                    {
                        TargetNode = TownCenter.Position;
                        returnResource = true;
                    }
                    else
                    {
                        TownCenter.AgentsResources.Remove(agentResource);
                        _target = agentResource.Item1;
                        resourceCarrying = agentResource.Item2;
                        TargetNode = _target.CurrentNode;
                    }
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetReach, Behaviours.Deliver);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources, () =>
            {
                (TcAgent<IVector, ITransform<IVector>>, ResourceType) agentResource = TownCenter.AgentsResources[0];
                TownCenter.AgentsResources.Remove(agentResource);
                _target = agentResource.Item1;
                resourceCarrying = agentResource.Item2;
                TargetNode = _target.CurrentNode;
            });
            Fsm.SetTransition(Behaviours.Walk, Flags.OnReturnResource, Behaviours.ReturnResources);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait, () => { returnResource = true; });
        }


        protected override void DeliverTransitions()
        {
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnHunger, Behaviours.Walk,
                () => { TargetNode = TownCenter.Position; });
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                });
        }

        private void ReturnResourceTransition()
        {
            Fsm.SetTransition(Behaviours.ReturnResources, Flags.OnHunger, Behaviours.GatherResources,
                () =>
                {
                    returnResource = false;
                    (TcAgent<IVector, ITransform<IVector>>, ResourceType) agentResource = TownCenter.AgentsResources[0];
                    TownCenter.AgentsResources.Remove(agentResource);
                    _target = agentResource.Item1;
                    resourceCarrying = agentResource.Item2;
                    TargetNode = _target.CurrentNode;
                });
            Fsm.SetTransition(Behaviours.ReturnResources, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                });
        }

        #endregion

        #region Params

        protected override object[] WalkTickParameters()
        {
            object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, returnResource, Path };
            return objects;
        }

        protected override object[] GatherTickParameters()
        {
            return new object[]
            {
                CurrentGold, CurrentFood, CurrentWood,
                resourceCarrying, ResourceLimit, onGather, Retreat
            };
        }

        private object[] DeliverTickParameters()
        {
            return new object[]
            {
                CurrentFood, CurrentGold, CurrentWood, resourceCarrying, onDeliver, Retreat, CurrentNode,
                _target.CurrentNode
            };
        }

        private object[] ReturnTickParameters()
        {
            return new object[] { CurrentFood, CurrentGold, CurrentWood, onReturnResource, Retreat };
        }

        protected override object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentNode, OnWait, returnResource };
            return objects;
        }

        #endregion

        private void ReturnResource()
        {
            lock (TownCenter)
            {
                switch (resourceCarrying)
                {
                    case ResourceType.Food:
                        if (CurrentFood <= 0) return;
                        CurrentFood--;
                        TownCenter.Food++;
                        break;
                    case ResourceType.Gold:
                        if (CurrentGold <= 0) return;
                        CurrentGold--;
                        TownCenter.Gold++;
                        break;
                    case ResourceType.Wood:
                        if (CurrentWood <= 0) return;
                        CurrentWood--;
                        TownCenter.Wood++;
                        break;
                    default:
                        return;
                }
            }
        }

        private void Gather()
        {
            lock (TownCenter)
            {
                switch (resourceCarrying)
                {
                    case ResourceType.Food:
                        if (TownCenter.Food <= 0) return;
                        CurrentFood++;
                        TownCenter.Food--;
                        break;
                    case ResourceType.Gold:
                        if (TownCenter.Gold <= 0) return;
                        CurrentGold++;
                        TownCenter.Gold--;
                        break;
                    case ResourceType.Wood:
                        if (TownCenter.Wood <= 0) return;
                        CurrentWood++;
                        TownCenter.Wood--;
                        break;
                    default:
                    return;
                }
            }
        }

        private void DeliverResource()
        {
            lock (_target)
            {
                switch (resourceCarrying)
                {
                    case ResourceType.Food:
                        if (CurrentFood <= 0) return;
                        CurrentFood--;
                        _target.CurrentFood++;
                        break;
                    case ResourceType.Gold:
                        if (CurrentGold <= 0) return;
                        CurrentGold--;
                        _target.CurrentGold++;
                        break;
                    case ResourceType.Wood:
                        if (CurrentWood <= 0) return;
                        CurrentWood--;
                        _target.CurrentWood++;
                        break;
                    default:
                        return;
                }
            }
        }

        public void Attacked()
        {
            if (resourceCarrying != ResourceType.Food) return;

            CurrentFood = 0;

            TownCenter.SoundAlarm();
        }
    }
}