using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
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
        OnWait,
        OnReturnResource
    }

    public enum Behaviours
    {
        Wait,
        Walk,
        GatherResources,
        ReturnResources,
        Build,
        Deliver,
    }

    public enum AgentTypes
    {
        Gatherer,
        Cart,
        Builder
    }

    public enum ResourceType
    {
        None,
        Gold,
        Wood,
        Food
    }

    public class TcAgent<TVector, TTransform>
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<IVector>, new()
    {
        public virtual TTransform Transform
        {
            get => transform;
            set
            {
                transform ??= new TTransform();
                transform.position ??= new MyVector(0, 0);

                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "Transform value cannot be null");
                }

                if (transform.position == null || value.position == null)
                {
                    throw new InvalidOperationException("Transform positions cannot be null");
                }

                transform.forward = (transform.position - value.position).Normalized();
                transform = value;
            }
        }

        public TownCenter TownCenter;
        public static bool Retreat;
        public SimNode<IVector> CurrentNode;
        public Voronoi<SimCoordinate, MyVector> Voronoi;
        public AStarPathfinder<SimNode<IVector>, IVector, SimCoordinate> Pathfinder;

        protected FSM<Behaviours, Flags> Fsm;
        protected List<SimNode<IVector>> Path;
        public AgentTypes AgentType;

        protected SimNode<IVector>? TargetNode
        {
            get => targetNode;
            set
            {
                targetNode = value;
                if (targetNode == null || targetNode.GetCoordinate() == null) return;
                Path = Pathfinder.FindPath(CurrentNode, TargetNode);
                PathNodeId = 0;
            }
        }

        protected Action OnMove;
        protected Action OnWait;

        public int CurrentFood = 3;
        public int CurrentGold = 0;
        public int CurrentWood = 0;
        protected int LastTimeEat = 0;
        protected const int ResourceLimit = 15;
        protected const int FoodLimit = 15;
        protected int PathNodeId;

        protected TTransform transform = new TTransform();
        private SimNode<IVector> targetNode;
        protected INode<IVector>? adjacentNode;

        private void Update()
        {
            Fsm.Tick();
        }

        public virtual void Init()
        {
            Fsm = new FSM<Behaviours, Flags>();

            Pathfinder = AgentType switch
            {
                AgentTypes.Gatherer => DataContainer.gathererPathfinder,
                AgentTypes.Cart => DataContainer.cartPathfinder,
                AgentTypes.Builder => DataContainer.builderPathfinder,
                _ => throw new ArgumentOutOfRangeException()
            };

            OnMove += Move;
            OnWait += Wait;

            FsmBehaviours();

            FsmTransitions();
        }

        protected virtual void FsmBehaviours()
        {
            Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters);
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters, WalkEnterParameters);
        }
        
        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            WaitTransitions();
            GatherTransitions();
            GetResourcesTransitions();
            DeliverTransitions();
        }

        #region Transitions

        protected virtual void GatherTransitions()
        {
            Fsm.SetTransition(Behaviours.GatherResources, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = TownCenter.position; });
        }


        protected virtual object[] GatherTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, ResourceLimit };
            return objects;
        }

        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = TownCenter.position; });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
                () => { TargetNode = TownCenter.position; });
        }

        protected virtual void DeliverTransitions()
        {
            return;
        }

        protected virtual void GetResourcesTransitions()
        {
            return;
        }

        #endregion

        #region Params

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


        protected virtual object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, CurrentNode, OnWait };
            return objects;
        }

        protected virtual object[] GetFoodTickParameters()
        {
            object[] objects = { CurrentFood, FoodLimit };
            return objects;
        }

        #endregion

        protected virtual void Move()
        {
            if (CurrentNode == null || TargetNode == null)
            {
                return;
            }

            if (CurrentNode.GetCoordinate().Adyacent(TargetNode.GetCoordinate())) return;

            if (Path.Count <= 0) return;
            if (PathNodeId > Path.Count) PathNodeId = 0;

            CurrentNode = Path[PathNodeId];
            PathNodeId++;
        }

        protected virtual void Wait()
        {
        }

        protected virtual SimNode<IVector> GetTarget(NodeType nodeType = NodeType.Empty)
        {
            IVector position = CurrentNode.GetCoordinate();
            INode<IVector> target = new SimNode<IVector>();

            switch (nodeType)
            {
                case NodeType.Empty:
                    break;
                case NodeType.Plains:
                    target = DataContainer.GetNearestNode(nodeType, position);
                    break;
                default:
                    break;
            }

            if (target == null)
            {
                // Debug.LogError("No mines with gold.");
                return null;
            }

            return DataContainer.graph.NodesType[(int)target.GetCoordinate().X, (int)target.GetCoordinate().Y];
        }

        protected virtual SimNode<IVector> GetTarget(NodeTerrain nodeTerrain = NodeTerrain.Empty)
        {
            IVector position = CurrentNode.GetCoordinate();
            INode<MyVector> target = new SimNode<MyVector>();

            switch (nodeTerrain)
            {
                case NodeTerrain.Empty:
                case NodeTerrain.Mine:
                    target = new SimNode<MyVector>(Voronoi
                        .GetClosestPointOfInterest(DataContainer.graph.CoordNodes[(int)position.X, (int)position.Y])
                        .GetCoordinate());
                    break;
                case NodeTerrain.Tree:
                case NodeTerrain.Lake:
                case NodeTerrain.Stump:
                case NodeTerrain.TownCenter:
                case NodeTerrain.Construction:
                    target = (DataContainer.GetNearestNode(nodeTerrain, position));
                    break;
                case NodeTerrain.WatchTower:
                default:
                    target = Voronoi.GetClosestPointOfInterest(
                        DataContainer.graph.CoordNodes[(int)position.X, (int)position.Y]);
                    break;
            }

            if (target == null)
            {
                // Debug.LogError("No mines with gold.");
                return null;
            }

            return DataContainer.graph.NodesType[(int)target.GetCoordinate().X, (int)target.GetCoordinate().Y];
        }
    }
}