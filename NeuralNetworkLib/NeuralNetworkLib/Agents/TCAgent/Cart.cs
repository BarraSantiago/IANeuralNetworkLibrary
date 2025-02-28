using NeuralNetworkLib.Agents.AnimalAgents;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Cart : TcAgent<IVector, ITransform<IVector>>
    {
        public ResourceType resourceCarrying;
        public TcAgent<IVector, ITransform<IVector>> _target;
        private Action onGather;
        private Action onDeliver;
        private Action onReturnResource;
        private bool returnResource;

        private int movementBrain;
        private int movementInputCount;
        private int WaitBrain;
        private int WaitInputCount;
        private int GetResourcesBrain;
        private int GetResourcesInputCount;
        private int DeliverBrain;
        private int DeliverInputCount;
        private int ReturnResourcesBrain;
        private int ReturnResourcesInputCount;
        
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
            
            movementBrain = GetBrainTypeKeyByValue(BrainType.Movement);
            movementInputCount = GetInputCount(BrainType.Movement);
            WaitBrain = GetBrainTypeKeyByValue(BrainType.Wait);
            WaitInputCount = GetInputCount(BrainType.Wait);
            GetResourcesBrain = GetBrainTypeKeyByValue(BrainType.GetResources);
            GetResourcesInputCount = GetInputCount(BrainType.GetResources);
            DeliverBrain = GetBrainTypeKeyByValue(BrainType.Deliver);
            DeliverInputCount = GetInputCount(BrainType.Deliver);
            ReturnResourcesBrain = GetBrainTypeKeyByValue(BrainType.ReturnResources);
            ReturnResourcesInputCount = GetInputCount(BrainType.ReturnResources);
        }

        public override void Reset()
        {
            base.Reset();
            TargetNode = GetTarget();
            Fsm.ForceTransition(Behaviours.GatherResources);
        }

        protected override void FsmBehaviours()
        {
            Fsm.AddBehaviour<CartWaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<CartWalkState>(Behaviours.Walk, WalkTickParameters);
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
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = TownCenter.Position; });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Walk,
                () => { TargetNode = TownCenter.Position; });
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
            Fsm.SetTransition(Behaviours.Deliver, Flags.OnTargetLost, Behaviours.Walk,
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

        protected override object[] GatherTickParameters()
        {
            return new object[] { Retreat, onGather, output[movementBrain] };
        }

        private object[] DeliverTickParameters()
        {
            return new object[] { onDeliver, Retreat, output[movementBrain] };
        }

        private object[] ReturnTickParameters()
        {
            return new object[] { onReturnResource, Retreat, output[GetResourcesBrain] };
        }

        protected override object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentNode, OnWait, output[ReturnResourcesBrain] };
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
            input[ReturnResourcesBrain] = new float[ReturnResourcesInputCount];

            input[ReturnResourcesBrain][0] = CurrentGold;
            input[ReturnResourcesBrain][1] = CurrentFood;
            input[ReturnResourcesBrain][2] = CurrentWood;
        }
      
        private void DeliverInputs()
        {
            input[DeliverBrain] = new float[DeliverInputCount];

            input[DeliverBrain][0] = CurrentGold;
            input[DeliverBrain][1] = CurrentFood;
            input[DeliverBrain][2] = CurrentWood;
            input[DeliverBrain][3] = !CurrentNode.GetCoordinate().Adjacent(TargetNode.GetCoordinate()) ? -1 : 1;
        }
       
        private void GetResourcesInputs()
        {
            const int minResourceAmount = 3;
            const int maxResourceAmount = 30;
            input[GetResourcesBrain] = new float[GetResourcesInputCount];

            input[GetResourcesBrain][0] = CurrentGold + CurrentFood + CurrentWood;
            input[GetResourcesBrain][1] = TownCenter.Gold;
            input[GetResourcesBrain][2] = TownCenter.Food;
            input[GetResourcesBrain][3] = TownCenter.Wood;
            input[GetResourcesBrain][4] = minResourceAmount;
            input[GetResourcesBrain][5] = maxResourceAmount;
            input[GetResourcesBrain][6] = resourceCarrying == ResourceType.None ? -1 : 1;
        }
      
        protected override void WaitInputs()
        {
            input[WaitBrain] = new float[WaitInputCount];

            input[WaitBrain][0] = CurrentNode.GetCoordinate().X;
            input[WaitBrain][1] = CurrentNode.GetCoordinate().Y;
            input[WaitBrain][2] = Retreat ? 1 : 0;
            input[WaitBrain][3] = Array.IndexOf(SafeRetreatTerrains, CurrentNode.NodeTerrain) == -1 ? 0 : 1;
        }

       
        protected override void MovementInputs()
        {

            input[movementBrain] = new float[movementInputCount];
            input[movementBrain][0] = CurrentNode.GetCoordinate().X;
            input[movementBrain][1] = CurrentNode.GetCoordinate().Y;

            AnimalAgent<IVector, ITransform<IVector>>? target =
                DataContainer.GetNearestEntity(AgentTypes.Carnivore, Transform.position);
            if (target == null)
            {
                input[movementBrain][2] = NoTarget;
                input[movementBrain][3] = NoTarget;
            }
            else
            {
                input[movementBrain][2] = target.CurrentNode.GetCoordinate().X;
                input[movementBrain][3] = target.CurrentNode.GetCoordinate().Y;
            }

            if (TargetNode == null)
            {
                input[movementBrain][4] = NoTarget;
                input[movementBrain][5] = NoTarget;
            }
            else
            {
                input[movementBrain][4] = TargetNode.GetCoordinate().X;
                input[movementBrain][5] = TargetNode.GetCoordinate().Y;
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
            if(!CurrentNode.GetCoordinate().Adjacent(_target.CurrentNode.GetCoordinate())) return;
            
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
                    case ResourceType.None:
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