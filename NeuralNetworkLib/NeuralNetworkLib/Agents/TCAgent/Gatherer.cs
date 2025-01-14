﻿using System.Diagnostics;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;
using Pathfinder.Voronoi;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class Gatherer : TcAgent<IVector, ITransform<IVector>>
    {
        public static Action OnEmptyMine;
        public static Action OnEmptyLake;
        public static Action OnEmptyTree;
        public ResourceType ResourceGathering = ResourceType.None;

        protected const int GoldPerFood = 3;
        protected const int FoodPerFood = 3;
        protected const int WoodPerFood = 5;
        private Action onGather;
        private Stopwatch stopwatch;
        private static Voronoi<SimCoordinate, MyVector> FoodVoronoi;
        private static Voronoi<SimCoordinate, MyVector> WoodVoronoi;
        private static Voronoi<SimCoordinate, MyVector> GoldVoronoi;

        public override void Init()
        {
            base.Init();
            AgentType = AgentTypes.Gatherer;
            Fsm.ForceTransition(Behaviours.Walk);
            onGather += Gather;
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
            Fsm.AddBehaviour<GathererWait>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters, WalkEnterParameters);
            Fsm.AddBehaviour<GatherResourceState>(Behaviours.GatherResources, GatherTickParameters, default, GatherExitParameters );
        }

        protected override void WaitTransitions()
        {
            base.WaitTransitions();
            Fsm.SetTransition(Behaviours.Wait, Flags.OnGather, Behaviours.Walk,
                () =>
                {
                    if(CurrentNode.NodeTerrain == NodeTerrain.TownCenter)
                    {
                        ResourceGathering = TownCenter.GetResourceNeeded();
                        TargetNode = GetTarget(ResourceGathering);
                        return;
                    }
                    
                    Fsm.ForceTransition(Behaviours.GatherResources);
                });
        }

        protected override void GatherTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    ResourceGathering = TownCenter.RemoveFromResource(ResourceGathering);
                    TargetNode = TownCenter.position; 
                });            
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Wait);

            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
                () =>
                {
                    ResourceGathering = TownCenter.RemoveFromResource(ResourceGathering);
                    TargetNode = TownCenter.position;
                });
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    ResourceGathering = TownCenter.GetResourceNeeded();
                    TargetNode = GetTarget(ResourceGathering);
                });
        }
        
        protected override void WalkTransitions()
        {
            base.WalkTransitions();
            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(ResourceGathering);

                    if (TargetNode == null)
                    {
                    }
                });
            Fsm.SetTransition(Behaviours.Walk, Flags.OnGather, Behaviours.GatherResources,
                () =>
                {
                    adjacentNode = TargetNode.GetAdjacentNode();
                    if (adjacentNode == null)
                    {
                        throw new Exception("Gatherer: WalkTransitions, adjacent node not found.");
                    }
                    adjacentNode.IsOccupied = true;
                    CurrentNode = DataContainer.graph.NodesType[(int)adjacentNode.GetCoordinate().X, (int)adjacentNode.GetCoordinate().Y];
                });
        }
        
        protected override object[] GatherTickParameters()
        {
            return new object[]
            {
                Retreat, CurrentFood, CurrentGold, CurrentWood, ResourceLimit, ResourceGathering, onGather, TargetNode
            };
        }
        
        protected object[] GatherExitParameters()
        {
            return new object[] { adjacentNode };
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

            if (CurrentFood < ResourceLimit && TownCenter.Food > 0)
            {
                CurrentFood++;
                TownCenter.Food--;
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
                default:
                    throw new Exception("Gatherer: Gather, resource type not found");
            }
        }

        private void GatherFood()
        {
            if (stopwatch.Elapsed.TotalSeconds < 1) return;

            CurrentFood++;
            LastTimeEat++;
            CurrentNode.Resource--;

            stopwatch.Restart();

            if (TargetNode.Resource <= 0) OnEmptyLake?.Invoke();

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

            if (TargetNode.Resource <= 0) OnEmptyTree?.Invoke();

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

            if (TargetNode.Resource <= 0) OnEmptyMine?.Invoke();

            if (LastTimeEat < GoldPerFood) return;

            CurrentFood--;
            LastTimeEat = 0;
        }

        protected SimNode<IVector> GetTarget(ResourceType resourceType = ResourceType.None)
        {
            IVector position = CurrentNode.GetCoordinate();
            SimNode<MyVector> target = new SimNode<MyVector>();

            switch (resourceType)
            {
                case ResourceType.None:
                    break;
                case ResourceType.Food:
                    target = FoodVoronoi.GetClosestPointOfInterest(
                        DataContainer.graph.CoordNodes[(int)position.X, (int)position.Y]);
                    break;
                case ResourceType.Gold:
                    target = GoldVoronoi.GetClosestPointOfInterest(
                        DataContainer.graph.CoordNodes[(int)position.X, (int)position.Y]);
                    break;
                case ResourceType.Wood:
                    target = WoodVoronoi.GetClosestPointOfInterest(
                        DataContainer.graph.CoordNodes[(int)position.X, (int)position.Y]);
                    break;
                default:
                    break;
            }

            if (target == null)
            {
                // Debug.LogError("No resourceType available.");
                return null;
            }

            return DataContainer.graph.NodesType[(int)target.GetCoordinate().X, (int)target.GetCoordinate().Y];
        }
    }
}