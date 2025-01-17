using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Builder : TcAgent<IVector, ITransform<IVector>>
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
            BuildTransitions();
        }


        protected override void FsmBehaviours()
        {
            base.FsmBehaviours();
            Fsm.AddBehaviour<BuildState>(Behaviours.Build, BuildTickParameters);
        }

        #region Transitions

        protected override void WalkTransitions()
        {
            base.WalkTransitions();
            Fsm.SetTransition(Behaviours.Walk, Flags.OnBuild, Behaviours.Build);
        }

        private void BuildTransitions()
        {
            Fsm.SetTransition(Behaviours.Build, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = TownCenter.Position;
                    TownCenter.RefugeeCount++;
                });
            Fsm.SetTransition(Behaviours.Build, Flags.OnHunger, Behaviours.Wait, () =>
            {
                if (CurrentFood <= 0)
                {
                    TownCenter.AskForResources(this, ResourceType.Food);
                }

                if (CurrentGold < TownCenter.WatchTowerBuildCost.Gold)
                {
                    TownCenter.AskForResources(this, ResourceType.Gold);
                }

                if (CurrentWood < TownCenter.WatchTowerBuildCost.Wood)
                {
                    TownCenter.AskForResources(this, ResourceType.Wood);
                }
            });
            Fsm.SetTransition(Behaviours.Build, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = (SimNode<IVector>?)TownCenter.GetWatchTowerConstruction(); });
        }

        protected override void WaitTransitions()
        {
            base.WaitTransitions();
            Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = (SimNode<IVector>?)TownCenter.GetWatchTowerConstruction(); });
            Fsm.SetTransition(Behaviours.Wait, Flags.OnBuild, Behaviours.Build);
        }

        #endregion


        protected object[] BuildTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, CurrentWood, onBuild, TargetNode };
        }

        protected override object[] WaitTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, CurrentWood, CurrentNode, TargetNode, OnWait };
        }

        private void Build()
        {
            if (TargetNode.NodeTerrain != NodeTerrain.Construction) return;
            if (CurrentFood <= 0 || CurrentGold < TownCenter.WatchTowerBuildCost.Gold ||
                CurrentWood < TownCenter.WatchTowerBuildCost.Wood) return;

            if (stopwatch.Elapsed.TotalSeconds < 1) return;

            CurrentGold -= TownCenter.WatchTowerBuildCost.Gold;
            CurrentWood -= TownCenter.WatchTowerBuildCost.Wood;
            CurrentFood--;

            lock (TargetNode)
            {
                TargetNode.BuildWatchTower();
            }

            stopwatch.Restart();
        }
    }
}