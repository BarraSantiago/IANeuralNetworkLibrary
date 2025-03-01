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
            ResourceLimit = 30;
            base.Init();
            CurrentFood = 0;


            TargetNode = GetTarget();
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

            Fsm.SetTransition(Behaviours.Wait, Flags.OnReturnResource, Behaviours.ReturnResources, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: Wait > OnReturnResource > ReturnResources.");
            });
        }

        protected override void GetResourcesTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: GatherResources > OnFull > Walk.");
            });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnWait, Behaviours.Wait, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: GatherResources > OnWait > Wait.");
            });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnReturnResource, Behaviours.ReturnResources, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: GatherResources > OnReturnResource > ReturnResources.");
            });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk, () =>
            {
                TargetNode = GetRetreatNode();
                TownCenter.RefugeeCount++;
                ConsoleLogger.StateTransition(AgentType + " Transition: GatherResources > OnRetreat > Walk.");
            });
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                    ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnRetreat > Walk.");
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget();
                    ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnTargetLost > Walk.");
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetReach, Behaviours.Deliver, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnTargetReach > Deliver.");
            });
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources,
                () =>
                {
                    TargetNode = GetTarget();
                    ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnGather > GatherResources.");
                });
            Fsm.SetTransition(Behaviours.Walk, Flags.OnReturnResource, Behaviours.ReturnResources, () =>
            {
                ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnReturnResource > ReturnResources.");
            });
            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait, () =>
            {
                returnResource = true;
                ConsoleLogger.StateTransition(AgentType + " Transition: Walk > OnWait > Wait.");
            });
        }


        protected override void DeliverTransitions()
        {
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnHunger, Behaviours.Walk,
                () =>
                {
                    TargetNode = TownCenter.Position; 
                    ConsoleLogger.StateTransition(AgentType + " Transition: Deliver > OnHunger > Walk.");
                });
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                    ConsoleLogger.StateTransition(AgentType + " Transition: Deliver > OnRetreat > Walk.");
                });
        }

        private void ReturnResourceTransition()
        {
            Fsm.SetTransition(Behaviours.ReturnResources, Flags.OnHunger, Behaviours.GatherResources,
                () =>
                {
                    returnResource = false;
                    TargetNode = GetTarget();
                    ConsoleLogger.StateTransition(AgentType + " Transition: ReturnResources > OnHunger > GatherResources.");
                });
            Fsm.SetTransition(Behaviours.ReturnResources, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetRetreatNode();
                    TownCenter.RefugeeCount++;
                    ConsoleLogger.StateTransition(AgentType + " Transition: ReturnResources > OnRetreat > Walk.");
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
                resourceCarrying, ResourceLimit, onGather, Retreat, TownCenter.Gold, TownCenter.Food, TownCenter.Wood
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
                if (CurrentFood > 0)
                {
                    CurrentFood--;
                    TownCenter.Food++;
                }

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
            }
            
            ConsoleLogger.ActionDone( AgentType + " Action: Return Resource.");
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
            ConsoleLogger.ActionDone( AgentType + " Action: Gather Resource.");
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
            ConsoleLogger.ActionDone( AgentType + " Action: Deliver Resource.");
        }

        public void Attacked()
        {
            if (resourceCarrying != ResourceType.Food) return;

            CurrentFood = 0;

            TownCenter.SoundAlarm();
            
            ConsoleLogger.ActionDone( AgentType + " Action: Cart Attacked.");
        }

        protected override void Wait()
        {
            base.Wait();

            if (Retreat) return;

            if (TownCenter.AgentsResources.Count < 1) return;

            TargetNode = GetTarget();

            Fsm.ForceTransition(Behaviours.Walk);
            
            ConsoleLogger.ActionDone( AgentType + " Action: Wait.");
        }

        private SimNode<IVector> GetTarget()
        {
            lock (TownCenter.AgentsResources)
            {
                if (TownCenter.AgentsResources.Count < 1)
                {
                    returnResource = true;
                    return TownCenter.Position;
                }

                (TcAgent<IVector, ITransform<IVector>>, ResourceType) agentResource = TownCenter.AgentsResources[0];
                if (agentResource.Item1 == null || agentResource.Item2 == null)
                {
                    returnResource = true;
                    return TownCenter.Position;
                }

                switch (agentResource.Item2)
                {
                    case ResourceType.Gold:
                        if (TownCenter.Gold <= 0)
                        {
                            returnResource = true;
                            TownCenter.AgentsResources.Remove(agentResource);
                            TownCenter.AskForResources(agentResource.Item1, ResourceType.Gold);
                            return TownCenter.Position;
                        }

                        break;
                    case ResourceType.Wood:
                        if (TownCenter.Wood <= 0)
                        {
                            returnResource = true;
                            TownCenter.AgentsResources.Remove(agentResource);
                            TownCenter.AskForResources(agentResource.Item1, ResourceType.Wood);
                            return TownCenter.Position;
                        }

                        break;
                    case ResourceType.Food:
                        if (TownCenter.Food <= 0)
                        {
                            returnResource = true;
                            TownCenter.AgentsResources.Remove(agentResource);
                            TownCenter.AskForResources(agentResource.Item1, ResourceType.Food);
                            return TownCenter.Position;
                        }

                        break;
                    case ResourceType.None:
                    default:
                        break;
                }

                _target = agentResource.Item1;
                resourceCarrying = agentResource.Item2;
                return _target.CurrentNode;
            }
        }
    }
}