using NeuralNetworkLib.Agents.AnimalAgents;
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

            Fsm.SetTransition(Behaviours.Wait, Flags.OnReturnResource, Behaviours.ReturnResources);
        }

        protected override void GetResourcesTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnWait, Behaviours.Wait);
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnReturnResource, Behaviours.ReturnResources);
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
                () => { TargetNode = GetTarget(); });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetReach, Behaviours.Deliver);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources,
                () => { TargetNode = GetTarget(); });
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
                    TargetNode = GetTarget();
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


        #region Inputs

        protected override void ExtraInputs()
        {
            base.ExtraInputs();
            ReturnResourcesInputs();
            DeliverInputs();
            GetResourcesInputs();
            WaitInputs();
        }

        private void ReturnResourcesInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.ReturnResources);
            int inputCount = GetInputCount(BrainType.ReturnResources);
            input[brain] = new float[inputCount];

            input[brain][0] = CurrentGold;
            input[brain][1] = CurrentFood;
            input[brain][2] = CurrentWood;
        }

        private void DeliverInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Deliver);
            int inputCount = GetInputCount(BrainType.Deliver);
            input[brain] = new float[inputCount];

            input[brain][0] = CurrentGold;
            input[brain][1] = CurrentFood;
            input[brain][2] = CurrentWood;
        }

        private void GetResourcesInputs()
        {
            const int minResourceAmount = 3;
            const int maxResourceAmount = 30;
            int brain = GetBrainTypeKeyByValue(BrainType.GetResources);
            int inputCount = GetInputCount(BrainType.GetResources);
            input[brain] = new float[inputCount];

            input[brain][0] = CurrentGold;
            input[brain][1] = CurrentFood;
            input[brain][2] = CurrentWood;
            input[brain][3] = TownCenter.Gold;
            input[brain][4] = TownCenter.Food;
            input[brain][5] = TownCenter.Wood;
            input[brain][6] = minResourceAmount;
            input[brain][7] = maxResourceAmount;
            input[brain][8] = resourceCarrying == ResourceType.None ? 0 : 1;
        }


        protected override void WaitInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Wait);
            int inputCount = GetInputCount(BrainType.Wait);
            input[brain] = new float[inputCount];

            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;
            input[brain][2] = Retreat ? 1 : 0;
            input[brain][3] = Array.IndexOf(SafeRetreatTerrains, CurrentNode.NodeTerrain) == -1 ? 0 : 1;
        }

        protected override void MovementInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Movement);
            int inputCount = GetInputCount(BrainType.Movement);

            input[brain] = new float[inputCount];
            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;

            AnimalAgent<IVector, ITransform<IVector>>? target =
                DataContainer.GetNearestEntity(AgentTypes.Carnivore, Transform.position);
            if (target == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
            }
            else
            {
                input[brain][2] = target.CurrentNode.GetCoordinate().X;
                input[brain][3] = target.CurrentNode.GetCoordinate().Y;
            }

            if (TargetNode == null)
            {
                input[brain][4] = NoTarget;
                input[brain][5] = NoTarget;
            }
            else
            {
                input[brain][4] = TargetNode.GetCoordinate().X;
                input[brain][5] = TargetNode.GetCoordinate().Y;
            }
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

        protected override void Wait()
        {
            base.Wait();

            if (Retreat) return;

            if (TownCenter.AgentsResources.Count < 1) return;

            TargetNode = GetTarget();

            Fsm.ForceTransition(Behaviours.Walk);
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