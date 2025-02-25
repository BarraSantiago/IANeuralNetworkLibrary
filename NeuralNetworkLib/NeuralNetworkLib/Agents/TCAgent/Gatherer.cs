using System.Diagnostics;
using NeuralNetworkLib.Agents.States.TCStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.GraphDirectory.Voronoi;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.TCAgent;

using Voronoi = VoronoiDiagram<Point2D>;

public class Gatherer : TcAgent<IVector, ITransform<IVector>>
{
    public ResourceType ResourceGathering = ResourceType.Gold;

    protected const int GoldPerFood = 3;
    protected const int FoodPerFood = 3;
    protected const int WoodPerFood = 5;
    private Action onGather;
    private static Voronoi FoodVoronoi;
    private static Voronoi WoodVoronoi;
    private static Voronoi GoldVoronoi;
    private int GatherBrain = 0;
    private int GatherInputCount = 0;
    public override void Init()
    {
        AgentType = AgentTypes.Gatherer;
        base.Init();
        FoodVoronoi = DataContainer.Voronois[(int)NodeTerrain.Lake];
        WoodVoronoi = DataContainer.Voronois[(int)NodeTerrain.Tree];
        GoldVoronoi = DataContainer.Voronois[(int)NodeTerrain.Mine];

        ResourceGathering = TownCenter.GetResourceNeeded();
        TargetNode = GetTarget(ResourceGathering);
        Fsm.ForceTransition(Behaviours.Walk);

        onGather += Gather;

        GatherBrain = GetBrainTypeKeyByValue(BrainType.Gather);
        GatherInputCount = GetInputCount(BrainType.Gather);
    }

    protected override void FsmTransitions()
    {
        base.FsmTransitions();
        GatherTransitions();
        WalkTransitions();
    }

    protected override void FsmBehaviours()
    {
        Fsm.AddBehaviour<GathererWaitState>(Behaviours.Wait, WaitTickParameters);
        Fsm.AddBehaviour<GathererWalkState>(Behaviours.Walk, WalkTickParameters);
        Fsm.AddBehaviour<GatherResourceState>(Behaviours.GatherResources, GatherTickParameters, default,
            GatherExitParameters);
    }

    #region Transition

    protected override void WaitTransitions()
    {
        base.WaitTransitions();
        Fsm.SetTransition(Behaviours.Wait, Flags.OnGather, Behaviours.Walk,
            () =>
            {
                if (CurrentNode.NodeTerrain == NodeTerrain.TownCenter)
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
                TargetNode = GetRetreatNode();
                TownCenter.RefugeeCount++;
            });
        Fsm.SetTransition(Behaviours.GatherResources, Flags.OnHunger, Behaviours.Wait,
            () => { TownCenter.AskForResources(this, ResourceType.Food); });

        Fsm.SetTransition(Behaviours.GatherResources, Flags.OnFull, Behaviours.Walk,
            () =>
            {
                ResourceGathering = TownCenter.RemoveFromResource(ResourceGathering);
                TargetNode = TownCenter.Position;
            });
        Fsm.SetTransition(Behaviours.GatherResources, Flags.OnTargetLost, Behaviours.Walk,
            () =>
            {
                ResourceGathering = TownCenter.GetResourceNeeded();
                TargetNode = GetTarget(ResourceGathering);
            });
        Fsm.SetTransition(Behaviours.GatherResources, Flags.OnGather, Behaviours.GatherResources);
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
                IVector coord = TargetNode.GetAdjacentNode();
                adjacentNode = DataContainer.GetNode(coord);
                if (adjacentNode == null)
                {
                    throw new Exception("Gatherer: WalkTransitions, adjacent node not found.");
                }

                adjacentNode.IsOccupied = true;
                CurrentNode = DataContainer.GetNode(adjacentNode.GetCoordinate());
            });
    }

    #endregion


    #region Params

    protected override object[] WaitTickParameters()
    {
        object[] objects = { Retreat, CurrentNode, OnWait, output[GatherBrain] };
        return objects;
    }

    protected override object[] GatherTickParameters()
    {
        return new object[] { Retreat, onGather, output[movementBrain] };
    }

    protected override object[] WalkTickParameters()
    {
        object[] objects = { CurrentNode, TargetNode, Retreat, OnMove, output[movementBrain],
                             output[GatherBrain][0], output[WaitBrain][0] };
        return objects;
    }

    protected object[] GatherExitParameters()
    {
        return new object[] { adjacentNode };
    }

    #endregion

    #region Inputs

    protected override void ExtraInputs()
    {
        base.ExtraInputs();
        GatherInputs();
    }

    protected void GatherInputs()
    {
        input[GatherBrain] = new float[GatherInputCount];
        
        input[GatherBrain][0] = CurrentFood;
        input[GatherBrain][1] = CurrentGold;
        input[GatherBrain][2] = CurrentWood;
        input[GatherBrain][3] = (int)ResourceGathering;
        input[GatherBrain][4] = ResourceLimit;
        input[GatherBrain][5] = TargetNode.Resource;
        input[GatherBrain][6] = ValidGatherTarget()? 1 : -1;

    }

    private bool ValidGatherTarget()
    {
        return !(TargetNode.Resource <= 0 || TargetNode.NodeTerrain != NodeTerrain.Tree ||
                TargetNode.NodeTerrain != NodeTerrain.Mine || TargetNode.NodeTerrain != NodeTerrain.Lake ||
                ResourceGathering == ResourceType.None);
    }
    protected override void WaitInputs()
    {
        base.WaitInputs();

        input[WaitBrain][4] = CurrentFood;

    }

    #endregion
    
    protected override void Wait()
    {
        base.Wait();
        const int minFood = 5;
        if (CurrentNode.NodeTerrain != NodeTerrain.TownCenter) return;

        if (CurrentGold > 0)
        {
            CurrentGold--;
            lock (TownCenter)
            {
                TownCenter.Gold++;
            }
        }

        if (CurrentWood > 0)
        {
            CurrentWood--;
            lock (TownCenter)
            {
                TownCenter.Wood++;
            }
        }


        if (CurrentFood < minFood && TownCenter.Food > 0)
        {
            CurrentFood++;
            lock (TownCenter)
            {
                TownCenter.Food--;
            }
        }
        else if (CurrentFood > minFood)
        {
            CurrentFood--;
            lock (TownCenter)
            {
                TownCenter.Food++;
            }
        }
    }

    private void Gather()
    {
        if (CurrentFood <= 0 || TargetNode.Resource <= 0 || TargetNode.NodeTerrain != NodeTerrain.Tree ||
            TargetNode.NodeTerrain != NodeTerrain.Mine || TargetNode.NodeTerrain != NodeTerrain.Lake) return;

        switch (TargetNode.NodeTerrain)
        {
            case NodeTerrain.Mine:
                GatherResource(ResourceType.Gold);
                break;
            case NodeTerrain.Tree:
                GatherResource(ResourceType.Wood);
                break;
            case NodeTerrain.Lake:
                GatherResource(ResourceType.Food);
                break;
            default:
                throw new Exception("Gatherer: Gather, resource type not found");
        }
    }

    private void GatherResource(ResourceType resourceType)
    {
        timer += Time;
        if (timer < 1) return;

        LastTimeEat++;
        int foodCost = 3;
        if (TargetNode.Resource > 0) TargetNode.Resource--;

        lock (TargetNode)
        {
            switch (resourceType)
            {
                case ResourceType.Gold:
                    foodCost = GoldPerFood;
                    CurrentGold++;
                    break;

                case ResourceType.Wood:
                    foodCost = WoodPerFood;
                    CurrentWood++;
                    break;

                case ResourceType.Food:
                    foodCost = FoodPerFood;
                    CurrentFood++;
                    break;

                case ResourceType.None:
                default:
                    throw new ArgumentOutOfRangeException(nameof(resourceType), resourceType, null);
            }
        }

        timer--;

        if (LastTimeEat < foodCost) return;

        CurrentFood--;
        LastTimeEat = 0;
    }

    protected SimNode<IVector> GetTarget(ResourceType resourceType = ResourceType.None)
    {
        IVector position = CurrentNode.GetCoordinate();
        Point2D target = new Point2D(-1, -1);
        Point2D wrongPoint = new Point2D(-1, -1);
        switch (resourceType)
        {
            case ResourceType.Food:
                target = FoodVoronoi.GetClosestPointOfInterest(new Point2D(position.X, position.Y)).Position;
                break;
            case ResourceType.Gold:
                target = GoldVoronoi.GetClosestPointOfInterest(new Point2D(position.X, position.Y)).Position;
                break;
            case ResourceType.Wood:
                target = WoodVoronoi.GetClosestPointOfInterest(new Point2D(position.X, position.Y)).Position;
                break;
            case ResourceType.None:
            default:
                break;
        }

        if (target.Equals(wrongPoint))
        {
            // Debug.LogError("No resourceType available.");
            return null;
        }

        return DataContainer.GetNode(target);
    }
}