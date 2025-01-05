using NeuralNetworkLib.Utils;
using Pathfinder;
using Pathfinder.Voronoi;

namespace NeuralNetworkLib.Agents.TCAgent
{
    public class TCAgent
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
            Deliver
        }

        public enum AgentTypes
        {
            Gatherer,
            Cart,
            Builder
        }

        public static SimNode<IVector> TownCenter;

        public static bool Retreat;
        public SimNode<IVector> CurrentNode;
        public Voronoi<INode<>, IVector> Voronoi;
        public AStarPathfinder<SimNode<IVector>, IVector, NodeVoronoi> Pathfinder;

        protected FSM<Behaviours, Flags> Fsm;
        protected List<SimNode<IVector>> Path;
        protected AgentTypes AgentType;
        protected SimNode<IVector> TargetNode
        {
            get => targetNode;
            set
            {
                targetNode = value;
                Path = Pathfinder.FindPath(CurrentNode, TargetNode, AgentType);
                PathNodeId = 0;
            }
        }

        protected Action OnMove;
        protected Action OnWait;

        protected int Food = 3;
        protected int CurrentGold = 0;
        protected int LastTimeEat = 0;
        protected const int GoldPerFood = 3;
        protected const int GoldLimit = 15;
        protected const int FoodLimit = 10;
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
                () =>
                {
                    TargetNode = GetTarget(NodeType.TownCenter);
                    if(TargetNode == null) return;

                    Debug.Log("Retreat to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });
        }


        protected virtual object[] GatherTickParameters()
        {
            object[] objects = { Retreat, Food, CurrentGold, GoldLimit };
            return objects;
        }


        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.TownCenter);
                    if(TargetNode == null) return;

                    Debug.Log("Retreat. Walk to " + TargetNode.GetCoordinate().x + " - " +
                              TargetNode.GetCoordinate().y);
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.Mine);
                    if(TargetNode == null) return;

                    Debug.Log("Walk to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait, () => Debug.Log("Wait"));
        }

        protected virtual object[] WalkTickParameters()
        {
            object[] objects = { CurrentNode, TargetNode, Retreat, transform, OnMove };
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
                () =>
                {
                    TargetNode = GetTarget(NodeType.Mine);
                    if(TargetNode == null) return;

                    Debug.Log("walk to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });
            Fsm.SetTransition(Behaviours.Wait, Units.Flags.OnTargetLost, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.Mine);
                    if(TargetNode == null) return;

                    Debug.Log("walk to " + TargetNode.GetCoordinate().x + " - " + TargetNode.GetCoordinate().y);
                });
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = GetTarget(NodeType.TownCenter);
                    if(TargetNode == null) return;

                    Debug.Log("Retreat. Walk to " + TargetNode.GetCoordinate().x + " - " +
                              TargetNode.GetCoordinate().y);
                });
        }

        
        

        protected virtual object[] WaitTickParameters()
        {
            object[] objects = { Retreat, Food, CurrentGold, CurrentNode, OnWait };
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
            object[] objects = { Food, FoodLimit };
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

            CurrentNode =  Path[PathNodeId];
            PathNodeId++;
        }

        private void Wait()
        {
            if (CurrentNode.NodeType == NodeType.Mine && CurrentNode.food > 0)
            {
                if (Food > 1) return;
                Food++;
                CurrentNode.food--;
            }

            if (CurrentNode.NodeType != NodeType.TownCenter || CurrentGold < 1) return;

            CurrentNode.gold++;
            CurrentGold--;
        }
        
        protected SimNode<IVector> GetTarget(NodeType nodeType = NodeType.Mine)
        {
            IVector position = transform.position;
            SimNode<IVector> target;
            
            switch (nodeType)
            {
                case NodeType.Mine:
                    target = Voronoi.GetMineCloser(GameManager.Graph.CoordNodes.Find(nodeVoronoi =>
                        nodeVoronoi.GetCoordinate() == position));
                    break;
                
                case NodeType.TownCenter:
                    target = TownCenter;
                    break;
                
                default:
                    target = Voronoi.GetMineCloser(GameManager.Graph.CoordNodes.Find(nodeVoronoi =>
                        nodeVoronoi.GetCoordinate() == position));
                    break;
            }
            
            if (target == null)
            {
                Debug.LogError("No mines with gold.");
                return null;
            }

            return GameManager.Graph.NodesType.Find(node => node.GetCoordinate() == target.GetCoordinate());
        }

    }
}