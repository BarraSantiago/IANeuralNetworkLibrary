﻿using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.Entities;
using NeuralNetworkLib.Utils;
using Pathfinder;
using Pathfinder.Voronoi;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public enum Flags
    {
        OnTargetReach,
        OnTargetLost,
        OnHunger,
        OnRetreat,
        OnFull,
        OnGather,
        OnWait
    }

    public enum Behaviours
    {
        Wait,
        Walk,
        GatherResources,
        Build,
        Deliver
    }

    public enum AgentTypes
    {
        Gatherer,
        Cart,
        Builder
    }

    public class TcAgent
    {
        public static TownCenter TownCenter;
        public static bool Retreat;
        public SimNode<IVector> CurrentNode;
        public Voronoi<SimCoordinate, MyVector> Voronoi;
        public AStarPathfinder<SimNode<IVector>, IVector, SimCoordinate> Pathfinder;

        protected FSM<Behaviours, Flags> Fsm;
        protected List<SimNode<IVector>> Path;
        protected AgentTypes AgentType;

        protected SimNode<IVector> TargetNode
        {
            get => targetNode;
            set
            {
                targetNode = value;
                Path = Pathfinder.FindPath(CurrentNode, TargetNode);
                PathNodeId = 0;
            }
        }

        protected Action OnMove;
        protected Action OnWait;

        public int CurrentFood = 3;
        protected int CurrentGold = 0;
        protected int CurrentWood = 0;
        protected int LastTimeEat = 0;
        protected const int ResourceLimit = 15;
        protected const int FoodLimit = 15;
        protected int PathNodeId;

        private SimNode<IVector> targetNode;

        private void Update()
        {
            Fsm.Tick();
        }

        public virtual void Init()
        {
            Fsm = new FSM<Behaviours, Flags>();

            Pathfinder = GameManager.MinerPathfinder;

            OnMove += Move;
            OnWait += Wait;

            FsmBehaviours();

            FsmTransitions();
        }


        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            WaitTransitions();
            GatherTransitions();
            GetFoodTransitions();
            DeliverTransitions();
        }


        protected virtual void FsmBehaviours()
        {
            Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters, WaitEnterParameters, WaitExitParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters, WalkEnterParameters);
        }

        protected virtual void GatherTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.TownCenter); });
        }


        protected virtual object[] GatherTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, ResourceLimit };
            return objects;
        }


        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.TownCenter); });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.Mine); });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
        }

        protected virtual object[] WalkTickParameters()
        {
            object[] objects = { CurrentNode, TargetNode, Retreat, OnMove };
            return objects;
        }

        protected virtual object[] WalkEnterParameters()
        {
            object[] objects = { CurrentNode, TargetNode, Path, Pathfinder, AgentType };
            return objects;
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnGather, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.Mine); });
            Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetLost, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.Mine); });
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = GetTarget(NodeType.Empty, NodeTerrain.TownCenter); });
        }


        protected virtual object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, CurrentNode, OnWait };
            return objects;
        }

        protected virtual object[] WaitEnterParameters()
        {
            return null;
        }

        protected virtual object[] WaitExitParameters()
        {
            return null;
        }


        protected virtual void GetFoodTransitions()
        {
            return;
        }

        protected virtual object[] GetFoodTickParameters()
        {
            object[] objects = { CurrentFood, FoodLimit };
            return objects;
        }

        protected virtual void DeliverTransitions()
        {
            return;
        }

        protected virtual void Move()
        {
            if (CurrentNode == null || TargetNode == null)
            {
                return;
            }

            if (CurrentNode.GetCoordinate().Equals(TargetNode.GetCoordinate())) return;

            if (Path.Count <= 0) return;
            if (PathNodeId > Path.Count) PathNodeId = 0;

            CurrentNode = Path[PathNodeId];
            PathNodeId++;
        }

        protected virtual void Wait()
        {
        }

        protected virtual SimNode<IVector> GetTarget(NodeType nodeType = NodeType.Empty,
            NodeTerrain nodeTerrain = NodeTerrain.Empty)
        {
            IVector position = CurrentNode.GetCoordinate();
            SimNode<IVector> target;

            switch (nodeType)
            {
                case NodeType.Empty:
                    break;
                case NodeType.Plains:
                    break;
                default:
                    target = Voronoi.GetMineCloser(GameManager.Graph.CoordNodes.Find(nodeVoronoi =>
                        nodeVoronoi.GetCoordinate() == position));
                    break;
            }

            switch (nodeTerrain)
            { 
                case NodeTerrain.TownCenter:
                target = TownCenter.position;
                    break;
            }

            if (target == null)
            {
                // Debug.LogError("No mines with gold.");
                return null;
            }

            return GameManager.Graph.NodesType.Find(node => node.GetCoordinate() == target.GetCoordinate());
        }
    }
}