using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.ECS.FlockingECS;
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
    public float[][] output;
    public float[][] input;
    public Dictionary<int, BrainType> brainTypes = new Dictionary<int, BrainType>();

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
    protected const int NoTarget = -1;
    protected int movementBrain;
    protected int movementInputCount;
    protected int FlockingBrain;
    protected int FlockingInputCount;
    protected int WaitBrain;
    protected int WaitInputCount;

    float minX;
    float maxX;
    float minY;
    float maxY;

    Sim2DGraph graph = DataContainer.Graph;

    public SimNode<IVector> TargetNode
    {
        get => targetNode;
        protected set
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
        minX = graph.MinX;
        maxX = graph.MaxX;
        minY = graph.MinY;
        maxY = graph.MaxY;
        output = new float[brainTypes.Count][];
        foreach (BrainType brain in brainTypes.Values)
        {
            BrainConfiguration inputsCount = DataContainer.InputCountCache[(brain, AgentType)];
            output[GetBrainTypeKeyByValue(brain)] = new float[inputsCount.OutputCount];
        }
        CalculateInputs();
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

        movementBrain = GetBrainTypeKeyByValue(BrainType.Movement);
        movementInputCount  = GetInputCount(BrainType.Movement);
        WaitBrain = GetBrainTypeKeyByValue(BrainType.Wait);
        WaitInputCount  = GetInputCount(BrainType.Wait);
        FlockingBrain = GetBrainTypeKeyByValue(BrainType.Flocking);
        FlockingInputCount  = GetInputCount(BrainType.Flocking);

    }

    public virtual void Reset()
    {
        CalculateInputs();

        CurrentFood = 3;
        CurrentGold = 0;
        CurrentWood = 0;
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
        object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, output[movementBrain] };
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

    #region NeuralNet

    public virtual void UpdateInputs(ACSComponent acsComponent)
    {
        MovementInputs();
        WaitInputs();
        FlockingInputs(acsComponent);
        ExtraInputs();
    }
    


    protected virtual void MovementInputs()
    {
        input[movementBrain] = new float[movementInputCount];
        input[movementBrain][0] = CurrentNode.GetCoordinate().X;
        input[movementBrain][1] = CurrentNode.GetCoordinate().Y;


        if (targetNode == null)
        {
            input[movementBrain][2] = NoTarget;
            input[movementBrain][3] = NoTarget;
        }
        else
        {
            input[movementBrain][2] = targetNode.X;
            input[movementBrain][3] = targetNode.Y;
        }
    }
    
    protected void FlockingInputs(ACSComponent acsComponent)
    {
        input[FlockingBrain] = new float[FlockingInputCount];
        
        input[FlockingBrain][0] = Transform.position.X;
        input[FlockingBrain][1] = Transform.position.Y;

        if (targetNode != null)
        {
            IVector direction = (targetNode.GetCoordinate() - Transform.position).Normalized();
            input[FlockingBrain][2] = direction.X;
            input[FlockingBrain][3] = direction.Y;
        }
        else
        {
            input[FlockingBrain][2] = NoTarget;
            input[FlockingBrain][3] = NoTarget;
        }


        IVector avgNeighborPosition = acsComponent.Direction;
        input[FlockingBrain][4] = float.IsNaN(avgNeighborPosition.X) ? NoTarget : avgNeighborPosition.X;
        input[FlockingBrain][5] = float.IsNaN(avgNeighborPosition.Y) ? NoTarget : avgNeighborPosition.Y;
        
        IVector separationVector = acsComponent.Separation;
        input[FlockingBrain][8] = float.IsNaN(separationVector.X) ? NoTarget : separationVector.X;
        input[FlockingBrain][9] = float.IsNaN(separationVector.Y) ? NoTarget : separationVector.Y;

        IVector alignmentVector = acsComponent.Alignment;
        if (alignmentVector == null || float.IsNaN(alignmentVector.X) || float.IsNaN(alignmentVector.Y))
        {
            input[FlockingBrain][10] = NoTarget;
            input[FlockingBrain][11] = NoTarget;
        }
        else
        {
            input[FlockingBrain][10] = alignmentVector.X;
            input[FlockingBrain][11] = alignmentVector.Y;
        }

        IVector cohesionVector = acsComponent.Cohesion;
        if (cohesionVector == null || float.IsNaN(cohesionVector.X) || float.IsNaN(cohesionVector.Y))
        {
            input[FlockingBrain][12] = NoTarget;
            input[FlockingBrain][13] = NoTarget;
        }
        else
        {
            input[FlockingBrain][12] = cohesionVector.X;
            input[FlockingBrain][13] = cohesionVector.Y;
        }

        if (targetNode == null)
        {
            input[FlockingBrain][14] = NoTarget;
            input[FlockingBrain][15] = NoTarget;
            return;
        }

        input[FlockingBrain][14] = targetNode.X;
        input[FlockingBrain][15] = targetNode.Y;
    }

    protected static readonly NodeTerrain[] SafeRetreatTerrains = { NodeTerrain.TownCenter, NodeTerrain.WatchTower };

    protected virtual void WaitInputs()
    {
        input[WaitBrain] = new float[WaitInputCount];

        input[WaitBrain][0] = CurrentNode.GetCoordinate().X;
        input[WaitBrain][1] = CurrentNode.GetCoordinate().Y;
        input[WaitBrain][2] = Retreat ? 1 : 0;
        input[WaitBrain][3] = Array.IndexOf(SafeRetreatTerrains, CurrentNode.NodeTerrain) == -1 ? 0 : 1;
    }

    protected virtual void ExtraInputs()
    {
    }

    protected int GetInputCount(BrainType brainType)
    {
        return InputCountCache.GetInputCount(AgentType, brainType);
    }

    protected void CalculateInputs()
    {
        int brainTypesCount = brainTypes.Count;
        input = new float[brainTypesCount][];
        output = new float[brainTypesCount][];

        for (int i = 0; i < brainTypesCount; i++)
        {
            BrainType brainType = brainTypes[i];
            input[i] = new float[GetInputCount(brainType)];
            int outputCount = DataContainer.InputCountCache[(brainType, AgentType)].OutputCount;
            output[i] = new float[outputCount];
        }
    }

    public int GetBrainTypeKeyByValue(BrainType value)
    {
        foreach (KeyValuePair<int, BrainType> kvp in brainTypes)
        {
            if (EqualityComparer<BrainType>.Default.Equals(kvp.Value, value))
            {
                return kvp.Key;
            }
        }

        throw new KeyNotFoundException(
            $"The BrainType value '{value}' is not present in the '{AgentType}' brainTypes dictionary.");
    }

    #endregion

    protected virtual void Move()
    {
        timer += Time;

        if (speed * timer < 1f)
            return;

        IVector currentCoord = CurrentNode.GetCoordinate();
        MyVector currentPos = new MyVector(currentCoord.X, currentCoord.Y);

        float[] brainOutput = output[movementBrain];
        if (brainOutput.Length < 2)
            return;

        currentPos.X += speed * timer * brainOutput[0];
        currentPos.Y += speed * timer * brainOutput[1];

        if (currentPos.X < minX)
            currentPos.X = maxX - 1;
        else if (currentPos.X >= maxX)
            currentPos.X = minX + 1;

        if (currentPos.Y < minY)
            currentPos.Y = maxY - 1;
        else if (currentPos.Y >= maxY)
            currentPos.Y = minY + 1;

        INode<IVector> newPosNode = DataContainer.GetNode(currentPos);
        if (newPosNode != null)
        {
            IVector newCoord = newPosNode.GetCoordinate();
            SetPosition(newCoord);
        }

        timer = 0;
    }

    protected SimNode<IVector> GetRetreatNode()
    {
        Point2D node = alarmVoronoi.GetClosestPointOfInterest(new Point2D(CurrentNode.X, CurrentNode.Y)).Position;
        return DataContainer.GetNode(node);
    }

    protected virtual void Wait()
    {
    }

    public virtual void SetPosition(IVector position)
    {
        if (!DataContainer.Graph.IsWithinGraphBorders(position)) return;
        Transform = (TTransform)new ITransform<IVector>(position);
        CurrentNode = DataContainer.GetNode(position);
    }
}