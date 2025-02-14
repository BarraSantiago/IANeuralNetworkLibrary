using System.Diagnostics;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Entities;
using NeuralNetworkLib.GraphDirectory;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent;

using Voronoi = VoronoiDiagram<Point2D>;

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

    public Behaviours CurrentState;
    public TVector AcsVector;
    public int CurrentFood = 3;
    public int CurrentGold = 0;
    public int CurrentWood = 0;
    public bool Retreat;
    public AgentTypes AgentType;
    public FSM<Behaviours, Flags> Fsm;
    public TownCenter TownCenter;
    public SimNode<IVector> CurrentNode;
    public AStarPathfinder<SimNode<IVector>, IVector, CoordinateNode>? Pathfinder;
    public static float Time;

    protected float timer = 0;
    protected int speed = 6;
    protected Action OnMove;
    protected Action OnWait;
    protected int LastTimeEat = 0;
    protected int ResourceLimit = 15;
    protected int? PathNodeId;
    protected TTransform transform = new TTransform();
    protected INode<IVector>? adjacentNode;
    protected List<SimNode<IVector>> Path;
    protected static Voronoi alarmVoronoi;

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

    public virtual void Init()
    {
        Fsm = new FSM<Behaviours, Flags>();
        Fsm.OnStateChange += state =>
            CurrentState = (Behaviours)Math.Clamp(state, 0, Enum.GetValues(typeof(Behaviours)).Length);
        Time = 0;
        Transform.position = TownCenter.Position.GetCoordinate();
        CurrentNode = TownCenter.Position;
        alarmVoronoi = DataContainer.Voronois[(int)NodeTerrain.TownCenter];

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
        Fsm.AddBehaviour<GathererWalkState>(Behaviours.Walk, WalkTickParameters);
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
                TargetNode = GetRetreatNode();
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
                TargetNode = GetRetreatNode();
                TownCenter.RefugeeCount++;
            });

        Fsm.SetTransition(Behaviours.Walk, Flags.OnWait, Behaviours.Wait);
    }

    protected virtual void WaitTransitions()
    {
        Fsm.SetTransition(Behaviours.Wait, Flags.OnRetreat, Behaviours.Walk,
            () =>
            {
                TargetNode = GetRetreatNode();
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
        object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, Path };
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
        if (CurrentNode == null || TargetNode == null || Path == null)
        {
            return;
        }

        if (CurrentNode.GetCoordinate().Adyacent(TargetNode.GetCoordinate()) ||
            Approximately(CurrentNode.GetCoordinate(), TargetNode.GetCoordinate(), 0.001f)) return;

        if (Path.Count <= 0) return;
        //if (PathNodeId >= Path.Count) PathNodeId = 0;

        timer += Time;

        float relativeSpeed = speed * timer;
        if (relativeSpeed < 1) return;


        int distanceToMove = (int)(relativeSpeed);

        PathNodeId += distanceToMove;
        PathNodeId = Math.Clamp((int)PathNodeId, 0, Path.Count - 1);

        CurrentNode = Path[(int)PathNodeId];
        Transform.position = CurrentNode.GetCoordinate();
        Transform.position += AcsVector;
        timer = (float)((relativeSpeed - Math.Truncate(relativeSpeed)) / speed);
    }

    protected SimNode<IVector> GetRetreatNode()
    {
        Point2D node = alarmVoronoi.GetClosestPointOfInterest(new Point2D(CurrentNode.X, CurrentNode.Y)).Position;
        return DataContainer.GetNode(node);
    }

    protected virtual void Wait()
    {
    }

    private bool Approximately(IVector coord1, IVector coord2, float tolerance)
    {
        return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
    }
}