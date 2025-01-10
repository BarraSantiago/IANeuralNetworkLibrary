using System.Diagnostics;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Gatherer : TcAgent<IVector, ITransform<IVector>>
    {
        public static Action OnEmptyMine;
        public static Action OnEmptyLake;
        public static Action OnEmptyTree;

        private Action onMine;
        protected const int GoldPerFood = 3;
        protected const int FoodPerFood = 3;
        protected const int WoodPerFood = 5;
        private Stopwatch stopwatch;

        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Gatherer;
            Fsm.ForceTransition(Behaviours.Walk);
            onMine += Gather;
            stopwatch = new Stopwatch();
            stopwatch.Start();
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
            Fsm.AddBehaviour<GatherResource>(Behaviours.GatherResources, GatherTickParameters);
        }

        protected override void GatherTransitions()
        {
            base.GatherTransitions();
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Wait);

            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
                () =>
                {
                    TargetNode = DataContainer.graph.NodesType[(int)TownCenter.position.GetCoordinate().X,
                        (int)TownCenter.position.GetCoordinate().Y];
                });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = GetTarget(); });
        }

        protected override object[] GatherTickParameters()
        {
            return new object[] { Retreat, CurrentFood, CurrentGold, ResourceLimit, onMine, CurrentNode };
        }
        
        protected override void WalkTransitions()
        {
            base.WalkTransitions();
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources);
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
            if (CurrentFood <= 0 || TargetNode.Resource <= 0) return;

            switch (TargetNode.NodeTerrain)
            {
                case NodeTerrain.Mine:
                    GatherGold();
                    break;
                case NodeTerrain.Tree:
                    GatherWood();
                    break;
                case NodeTerrain.Lake:
                    GatherFood();
                    break;
            }
        }

        private void GatherFood()
        {
            if (stopwatch.Elapsed.TotalSeconds < 1) return;

            CurrentFood++;
            LastTimeEat++;
            CurrentNode.Resource--;

            stopwatch.Restart();

            if (CurrentNode.Resource <= 0) OnEmptyLake?.Invoke();

            if (LastTimeEat < FoodPerFood) return;

            CurrentFood--;
            LastTimeEat = 0;
        }

        private void GatherWood()
        {
            if (stopwatch.Elapsed.TotalSeconds < 1) return;

            CurrentWood++;
            LastTimeEat++;
            CurrentNode.Resource--;
            stopwatch.Restart();

            if (CurrentNode.Resource <= 0) OnEmptyTree?.Invoke();

            if (LastTimeEat < WoodPerFood) return;

            CurrentFood--;
            LastTimeEat = 0;
        }

        private void GatherGold()
        {
            if (stopwatch.Elapsed.TotalSeconds < 1) return;

            CurrentGold++;
            LastTimeEat++;
            CurrentNode.Resource--;
            stopwatch.Restart();

            if (CurrentNode.Resource <= 0) OnEmptyMine?.Invoke();

            if (LastTimeEat < GoldPerFood) return;

            CurrentFood--;
            LastTimeEat = 0;
        }
    }
}