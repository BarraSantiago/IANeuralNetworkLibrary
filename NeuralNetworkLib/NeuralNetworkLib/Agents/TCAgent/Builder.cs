using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent;

using Voronoi = VoronoiDiagram<Point2D>;

public class Builder : TcAgent<IVector, ITransform<IVector>>
{
    private Action onBuild;
    private static Voronoi plainsVoronoi;
    private int BuildBrain;
    private int BuildInputCount;

    public override void Init()
    {
        AgentType = AgentTypes.Builder;
        base.Init();
        plainsVoronoi = DataContainer.Voronois[(int)NodeTerrain.Empty];

        IVector node = TownCenter.GetWatchTowerConstruction().GetCoordinate();
        TargetNode = DataContainer.GetNode(node);

        Fsm.ForceTransition(Behaviours.Walk);
        CurrentState = Behaviours.Walk;
        onBuild += Build;

        BuildBrain = GetBrainTypeKeyByValue(BrainType.Build);
        BuildInputCount = GetInputCount(BrainType.Build);
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
        Fsm.AddBehaviour<BuilderWalkState>(Behaviours.Walk, WalkTickParameters);
        Fsm.AddBehaviour<WaitState>(Behaviours.Wait, WaitTickParameters);
        Fsm.AddBehaviour<BuildState>(Behaviours.Build, BuildTickParameters);
    }

    #region Transitions

    protected override void WalkTransitions()
    {
        base.WalkTransitions();
        Fsm.SetTransition(Behaviours.Walk, Flags.OnBuild, Behaviours.Build, () =>
        {
            IVector? coord = TargetNode.GetAdjacentNode();
            if (coord == null)
            {
                throw new Exception("Gatherer: WalkTransitions, adjacent node not found.");
            }

            adjacentNode = DataContainer.GetNode(coord);
            adjacentNode.IsOccupied = true;
            CurrentNode = DataContainer.GetNode(adjacentNode.GetCoordinate());
        });
    }

    private void BuildTransitions()
    {
        Fsm.SetTransition(Behaviours.Build, Flags.OnRetreat, Behaviours.Walk,
            () =>
            {
                CurrentNode.IsOccupied = false;
                TargetNode = GetRetreatNode();
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
            () =>
            {
                CurrentNode.IsOccupied = false;
                IVector node = TownCenter.GetWatchTowerConstruction().GetCoordinate();
                TargetNode = DataContainer.GetNode(node);
            });
    }

    protected override void WaitTransitions()
    {
        base.WaitTransitions();
        Fsm.SetTransition(Behaviours.Wait, Flags.OnTargetLost, Behaviours.Walk,
            () =>
            {
                IVector node = TownCenter.GetWatchTowerConstruction().GetCoordinate();
                TargetNode = DataContainer.GetNode(node);
            });
        Fsm.SetTransition(Behaviours.Wait, Flags.OnBuild, Behaviours.Build);
    }

    #endregion

    #region Params

    protected object[] BuildTickParameters()
    {
        return new object[] { Retreat, onBuild, output[BuildBrain] };
    }

    protected override object[] WaitTickParameters()
    {
        return new object[] { Retreat, CurrentNode, OnWait, output[WaitBrain] };
    }

    protected override object[] WalkTickParameters()
    {
        object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, output[movementBrain] };
        return objects;
    }

    #endregion

    #region Inputs

    protected override void ExtraInputs()
    {
        base.ExtraInputs();
        BuildInputs();
    }

    private void BuildInputs()
    {
        input[BuildBrain] = new float[BuildInputCount];

        input[BuildBrain][0] = CurrentGold;
        input[BuildBrain][1] = CurrentFood;
        input[BuildBrain][2] = CurrentWood;
        input[BuildBrain][3] = TargetNode.Resource >= 100 ? 1 : -1;
        input[BuildBrain][4] = TargetNode.NodeTerrain != NodeTerrain.Construction ? 1 : -1;
    }

    protected override void WaitInputs()
    {
        base.WaitInputs();

        input[WaitBrain][4] = CurrentFood;
        input[WaitBrain][5] = CurrentGold;
        input[WaitBrain][6] = CurrentWood;
    }

    #endregion

    private void Build()
    {
        if (TargetNode.NodeTerrain != NodeTerrain.Construction) return;
        if (CurrentFood <= 0 || CurrentGold < TownCenter.WatchTowerBuildCost.Gold ||
            CurrentWood < TownCenter.WatchTowerBuildCost.Wood) return;

        timer += Time;
        if (timer < 1) return;

        CurrentGold -= TownCenter.WatchTowerBuildCost.Gold;
        CurrentWood -= TownCenter.WatchTowerBuildCost.Wood;
        CurrentFood--;

        lock (TargetNode)
        {
            TargetNode.BuildWatchTower();
        }

        timer -= (float)Math.Floor(timer);
    }
}