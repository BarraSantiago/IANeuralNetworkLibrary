using NeuralNetworkLib.DataManagement;

namespace NeuralNetworkLib.Utils;

public class NodeUpdater
{
    public void UpdateNode(IVector nodeCoord, NodeTerrain newTerrain, NodeType newType)
    {
        SimNode<IVector> oldNode = DataContainer.GetNode(nodeCoord);
        NodeTerrain oldTerrain = oldNode.NodeTerrain;

        // Update node properties
        lock (oldNode)
        {
            oldNode.NodeTerrain = newTerrain;
            oldNode.NodeType = newType;
        }

        // Create a thread-safe copy of agent IDs to iterate through
        uint[] tcAgentIds = DataContainer.TcAgents.Keys.ToArray();

        foreach (uint agentId in tcAgentIds)
        {
            // Use thread-safe modification for each agent
            if (!DataContainer.TcAgents.TryGetValue(agentId, out var tcAgent))
                continue;

            lock (tcAgent)
            {
                int cost = 5;
                bool blocked = newType == NodeType.Lake;

                switch (tcAgent.AgentType)
                {
                    case AgentTypes.Gatherer:
                        break;
                    case AgentTypes.Cart:
                        if (newType == NodeType.Mountain)
                        {
                            blocked = true;
                            cost += 5;
                        }
                        else if (newType == NodeType.Sand)
                        {
                            cost += 1;
                        }

                        break;
                    case AgentTypes.Builder:
                        if (newType == NodeType.Mountain)
                        {
                            blocked = true;
                            cost += 5;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // Safely update pathfinder within the lock
                tcAgent.Pathfinder.UpdateNode(nodeCoord, cost, blocked);

                // Check if current path contains the updated node
                bool needsPathRecalculation = false;
                if (tcAgent.Path != null)
                {
                    needsPathRecalculation = tcAgent.Path.Any(node => node.GetCoordinate().Equals(nodeCoord));
                }

                // Recalculate path if needed
                if (needsPathRecalculation && tcAgent.CurrentNode != null && tcAgent.TargetNode != null)
                {
                    tcAgent.Pathfinder.FindPath(tcAgent.CurrentNode, tcAgent.TargetNode);
                }
            }
        }

        // Update Voronoi diagrams
        lock (DataContainer.Voronois)
        {
            DataContainer.UpdateVoronoi(oldTerrain);
            DataContainer.UpdateVoronoi(newTerrain);
        }
    }
}