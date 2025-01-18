using System.Diagnostics;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Entities;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.Utils;
using Pathfinder;

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
        OnBuild,
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

        public int CurrentFood = 3;
        public int CurrentGold = 0;
        public int CurrentWood = 0;
        public bool Retreat;
        public AgentTypes AgentType;
        public TownCenter TownCenter;
        public SimNode<IVector> CurrentNode;
        public Voronoi<SimCoordinate, MyVector> Voronoi;
        public AStarPathfinder<SimNode<IVector>, IVector, SimCoordinate>? Pathfinder;

        protected int speed = 6;
        protected Action OnMove;
        protected Action OnWait;
        protected int LastTimeEat = 0;
        protected const int ResourceLimit = 15;
        protected const int FoodLimit = 15;
        protected int? PathNodeId;
        protected Stopwatch stopwatch;
        protected TTransform transform = new TTransform();
        protected INode<IVector>? adjacentNode;
        protected List<SimNode<IVector>> Path;
        protected FSM<Behaviours, Flags> Fsm;

        protected SimNode<IVector> TargetNode
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

        private SimNode<IVector> targetNode;

        private void Update()
        {
            Fsm.Tick();
        }

        public virtual void Init()
        {
            Fsm = new FSM<Behaviours, Flags>();

            Pathfinder = AgentType switch
            {
                AgentTypes.Gatherer => DataContainer.GathererPathfinder,
                AgentTypes.Cart => DataContainer.CartPathfinder,
                AgentTypes.Builder => DataContainer.BuilderPathfinder,
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
            Fsm.AddBehaviour<WalkState>(Behaviours.Walk, WalkTickParameters, WalkEnterParameters, WalkExitParameters);
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
                () =>
                {
                    TargetNode = TownCenter.Position;
                    TownCenter.RefugeeCount++;
                });
        }


        protected virtual object[] GatherTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, ResourceLimit };
            return objects;
        }

        protected virtual void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = TownCenter.Position; 
                    TownCenter.RefugeeCount++;
                });

            Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
        }

        protected virtual void WaitTransitions()
        {
            Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
                () =>
                {
                    TargetNode = TownCenter.Position;
                    TownCenter.RefugeeCount++;
                });
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
        
        protected virtual object[] WalkExitParameters()
        {
            object[] objects = { PathNodeId };
            return objects;
        }

        protected virtual object[] WaitTickParameters()
        {
            object[] objects = { Retreat, CurrentFood, CurrentGold, CurrentNode, OnWait };
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
            //if (PathNodeId >= Path.Count) PathNodeId = 0;

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            
            if(speed * elapsedSeconds < 1) return;
            
            int distanceToMove = (int)(speed * elapsedSeconds);

            PathNodeId += distanceToMove;
            PathNodeId = Math.Clamp((int)PathNodeId, 0, Path.Count - 1);

            CurrentNode = Path[(int)PathNodeId];

            stopwatch.Restart();
        }

        protected virtual void Wait()
        {
        }
    }
}