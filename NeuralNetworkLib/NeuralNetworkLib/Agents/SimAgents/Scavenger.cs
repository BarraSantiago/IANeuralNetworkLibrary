using NeuralNetworkLib.Agents.Flocking;
using NeuralNetworkLib.Agents.States;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.SimAgents
{
    public class Scavenger<TVector, TTransform> : SimAgent<TVector, TTransform>
        where TTransform : ITransform<IVector>, new()
        where TVector : IVector, IEquatable<TVector>
    {
        public Boid<IVector, ITransform<IVector>> boid = new Boid<IVector, ITransform<IVector>>();
        private IVector targetPosition = new MyVector();
        public IVector target;
    
        public override TTransform Transform
        {
            get => transform;
            set
            {
                transform ??= new TTransform();
                transform.position ??= new MyVector(0, 0);
                value ??= new TTransform();
                value.position ??= new MyVector(0, 0);

                transform.forward = (transform.position - value.position).Normalized();
                transform = value;
                boid.transform.position = value.position;
                boid.transform.forward = (targetPosition - value.position).Normalized();
            }
        }

        public override void Init()
        {
            targetPosition = GetTargetPosition();
            Transform.forward = (targetPosition - Transform.position).Normalized();
            boid = new Boid<IVector, ITransform<IVector>>
            {
                transform = Transform,
                target = targetPosition,
            };

            boid.transform.forward ??= MyVector.zero();

            base.Init();
            foodTarget = SimNodeType.Carrion;
            FoodLimit = 20;
            movement = 5;

            CalculateInputs();
        }

        public override void Reset()
        {
            base.Reset();
            boid = new Boid<IVector, ITransform<IVector>>
            {
                transform = Transform,
                target = targetPosition,
            };
            boid.Init(DataContainer.flockingManager.Alignment, DataContainer.flockingManager.Cohesion,
                DataContainer.flockingManager.Separation, DataContainer.flockingManager.Direction);
        }

        public override void UpdateInputs()
        {
            boid.NearBoids = DataContainer.GetBoidsInsideRadius(boid);
            base.UpdateInputs();
        }

        protected override void MovementInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.ScavengerMovement);
            int inputCount = GetInputCount(BrainType.ScavengerMovement);

            input[brain] = new float[inputCount];
            input[brain][0] = Transform.position.X;
            input[brain][1] = Transform.position.Y;

            SimAgent<IVector, ITransform<IVector>> target =
                DataContainer.GetNearestEntity(SimAgentTypes.Carnivore, Transform.position);
            if (target == null || target.CurrentNode == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
            }
            else
            {
                input[brain][2] = target.Transform.position.X;
                input[brain][3] = target.Transform.position.Y;
            }

            targetPosition = GetTargetPosition();
            if (targetPosition == null)
            {
                input[brain][4] = NoTarget;
                input[brain][5] = NoTarget;
            }
            else
            {
                input[brain][4] = targetPosition.X;
                input[brain][5] = targetPosition.Y;
            }

            input[brain][6] = Food;
        }

        protected override void ExtraInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Flocking);
            int inputCount = GetInputCount(BrainType.Flocking);
            input[brain] = new float[inputCount];

            targetPosition = GetTargetPosition();

            input[brain][0] = Transform.position.X;
            input[brain][1] = Transform.position.Y;

            if (targetPosition != null)
            {
                IVector direction = (targetPosition - Transform.position).Normalized();
                input[brain][2] = direction.X;
                input[brain][3] = direction.Y;
            }
            else
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
            }

            IVector avgNeighborPosition = GetAverageNeighborPosition();
            input[brain][4] = float.IsNaN(avgNeighborPosition.X) ? NoTarget : avgNeighborPosition.X;
            input[brain][5] = float.IsNaN(avgNeighborPosition.Y) ? NoTarget : avgNeighborPosition.Y;

            IVector avgNeighborVelocity = GetAverageNeighborDirection();
            input[brain][6] = float.IsNaN(avgNeighborVelocity.X) ? NoTarget : avgNeighborVelocity.X;
            input[brain][7] = float.IsNaN(avgNeighborVelocity.Y) ? NoTarget : avgNeighborVelocity.Y;

            IVector separationVector = GetSeparationVector();
            input[brain][8] = float.IsNaN(separationVector.X) ? NoTarget : separationVector.X;
            input[brain][9] = float.IsNaN(separationVector.Y) ? NoTarget : separationVector.Y;

            IVector alignmentVector = GetAlignmentVector();
            if (alignmentVector == null || float.IsNaN(alignmentVector.X) || float.IsNaN(alignmentVector.Y))
            {
                input[brain][10] = NoTarget;
                input[brain][11] = NoTarget;
            }
            else
            {
                input[brain][10] = alignmentVector.X;
                input[brain][11] = alignmentVector.Y;
            }

            IVector cohesionVector = GetCohesionVector();
            if (cohesionVector == null || float.IsNaN(cohesionVector.X) || float.IsNaN(cohesionVector.Y))
            {
                input[brain][12] = NoTarget;
                input[brain][13] = NoTarget;
            }
            else
            {
                input[brain][12] = cohesionVector.X;
                input[brain][13] = cohesionVector.Y;
            }

            if (targetPosition == null)
            {
                input[brain][14] = NoTarget;
                input[brain][15] = NoTarget;
                return;
            }

            input[brain][14] = targetPosition.X;
            input[brain][15] = targetPosition.Y;
            boid.target = targetPosition;
        }


        private IVector GetAverageNeighborPosition()
        {
            IVector averagePosition = new MyVector(0, 0);
            int neighborCount = 0;

            foreach (ITransform<IVector>? neighbor in boid.NearBoids)
            {
                if (neighbor?.position == null) continue;

                averagePosition += neighbor.position;
                neighborCount++;
            }

            if (neighborCount > 0)
            {
                averagePosition /= neighborCount;
            }

            return averagePosition;
        }

        private IVector GetAverageNeighborDirection()
        {
            if (boid.NearBoids.Count == 0)
            {
                return MyVector.zero();
            }

            MyVector avg = MyVector.zero();
            foreach (ITransform<IVector> boid1 in boid.NearBoids)
            {
                avg += boid1.forward;
            }

            avg /= boid.NearBoids.Count;
            return avg;
        }

        private IVector GetSeparationVector()
        {
            return boid.GetSeparation();
        }

        private IVector GetAlignmentVector()
        {
            return boid.GetAlignment();
        }

        private IVector GetCohesionVector()
        {
            return boid.GetCohesion();
        }

        private IVector GetTargetPosition()
        {
            target = GetTarget(foodTarget)?.GetCoordinate();

            return target == null ? MyVector.NoTarget() : target;
        }

        protected override void Eat()
        {
            if (!DataContainer.graph.IsWithinGraphBorders(targetPosition)) return;

            SimNode<IVector> node = DataContainer.graph.NodesType[(int)targetPosition.X, (int)targetPosition.Y];

            lock (node)
            {
                if (node.Food <= 0) return;
                Food++;
                node.Food--;
                if (node.Food <= 0) node.NodeType = SimNodeType.Empty;
            }
        }

        protected override void Move()
        {
            int index = GetBrainTypeKeyByValue(BrainType.ScavengerMovement);
            int flockIndex = GetBrainTypeKeyByValue(BrainType.Flocking);

            if (output[index].Length != 2 || output[flockIndex].Length != 4) return;

            boid.alignmentOffset = output[flockIndex][0];
            boid.cohesionOffset = output[flockIndex][1];
            boid.separationOffset = output[flockIndex][2];
            boid.directionOffset = output[flockIndex][3];

            IVector flocking = boid.ACS();

            if (float.IsNaN(flocking.X) || float.IsNaN(flocking.Y))
            {
                flocking = MyVector.zero();
            }

            float leftForce = output[index][0];
            float rightForce = output[index][1];

            MyVector currentPos = new MyVector(Transform.position.X, Transform.position.Y);
            currentPos.X += rightForce * movement;
            currentPos.Y += leftForce * movement;
            currentPos += flocking;

            if (!DataContainer.graph.IsWithinGraphBorders(currentPos))
            {
                if (currentPos.X <= DataContainer.graph.MinX)
                {
                    currentPos.X = DataContainer.graph.MaxX - 1;
                }

                if (currentPos.X >= DataContainer.graph.MaxX)
                {
                    currentPos.X = DataContainer.graph.MinX + 1;
                }

                if (currentPos.Y <= DataContainer.graph.MinY)
                {
                    currentPos.Y = DataContainer.graph.MaxY - 1;
                }

                if (currentPos.Y >= DataContainer.graph.MaxY)
                {
                    currentPos.Y = DataContainer.graph.MinY + 1;
                }
            }

            SetPosition(currentPos);
        }

        public override void SetPosition(IVector position)
        {
            Transform = (TTransform)new ITransform<IVector>(position);
        }

        public override INode<IVector> GetTarget(SimNodeType nodeType = SimNodeType.Empty)
        {
            INode<IVector> target = DataContainer.GetNearestNode(nodeType, Transform.position) ??
                                    DataContainer.GetNearestNode(SimNodeType.Corpse, Transform.position);


            if (target != null) return target;

            SimAgent<IVector, ITransform<IVector>> nearestEntity =
                DataContainer.GetNearestEntity(SimAgentTypes.Carnivore, Transform.position);
            if (nearestEntity != null)
            {
                target = nearestEntity.CurrentNode;
            }

            return target;
        }

        protected override void FsmBehaviours()
        {
            Fsm.AddBehaviour<SimWalkScavState>(Behaviours.Walk, WalkTickParameters);
            ExtraBehaviours();
        }

        protected override void ExtraBehaviours()
        {
            Fsm.AddBehaviour<SimEatScavState>(Behaviours.Eat, EatTickParameters);
        }

        protected override object[] EatTickParameters()
        {
            object[] objects = new object[5];
            objects[0] = Transform.position;

            if (targetPosition.X >= 0 && targetPosition.X < DataContainer.graph.NodesType.GetLength(0) &&
                targetPosition.Y >= 0 && targetPosition.Y < DataContainer.graph.NodesType.GetLength(1))
            {
                objects[1] = DataContainer.graph.NodesType[(int)targetPosition.X, (int)targetPosition.Y];
            }
            else
            {
                objects[1] = null;
            }

            objects[2] = OnEat;
            objects[3] = OnMove;

            int brainKey = GetBrainTypeKeyByValue(BrainType.Eat);
            if (brainKey >= 0 && brainKey < output.Length)
            {
                objects[4] = output[brainKey];
            }
            else
            {
                objects[4] = null;
            }

            return objects;
        }

        protected override object[] WalkTickParameters()
        {
            object[] objects =
                { Transform.position, targetPosition, OnMove, output[GetBrainTypeKeyByValue(BrainType.Eat)] };

            return objects;
        }

        protected override void EatTransitions()
        {
            Fsm.SetTransition(Behaviours.Eat, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnSearchFood, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnAttack, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnEscape, Behaviours.Walk);
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnSearchFood, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnAttack, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnEscape, Behaviours.Walk);
        }
    }
}