﻿using NeuralNetworkLib.ECS.Patron;

namespace NeuralNetworkLib.ECS.FlockingECS;

public class BoidConfigComponent : EcsComponent
{
    public BoidConfigComponent(float detectionRadius, float alignmentOffset, float cohesionOffset,
        float separationOffset, float directionOffset)
    {
        this.detectionRadius = detectionRadius;
        this.alignmentOffset = alignmentOffset;
        this.cohesionOffset = cohesionOffset;
        this.separationOffset = separationOffset;
        this.directionOffset = directionOffset;
    }
    public float detectionRadius = 6.0f;
    public float alignmentOffset;
    public float cohesionOffset;
    public float separationOffset;
    public float directionOffset;
}