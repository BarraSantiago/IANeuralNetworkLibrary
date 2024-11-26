using NeuralNetworkLib.Agents.States;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.SimAgents
{
    public enum Flags
    {
        OnEscape,
        OnEat,
        OnSearchFood,
        OnAttack
    }

    public class SimAgent<TVector, TTransform>
        where TVector : IVector, IEquatable<TVector>
        where TTransform : ITransform<IVector>, new()
    {
        public enum Behaviours
        {
            Walk,
            Escape,
            Eat,
            Attack
        }

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

        public virtual INode<IVector> CurrentNode =>
            DataContainer.graph.NodesType[(int)Transform.position.X, (int)Transform.position.Y];

        public static Action<SimAgent<TVector, TTransform>> OnDeath;
        protected TTransform transform = new TTransform();
        public virtual bool CanReproduce => Food >= FoodLimit;
        public SimAgentTypes agentType { get; set; }
        public FSM<Behaviours, Flags> Fsm;

        protected int movement = 3;
        protected SimNodeType foodTarget;
        public int FoodLimit { get; protected set; } = 5;
        public int Food { get; protected set; } = 0;
        protected Action OnMove;
        protected Action OnEat;
        protected float dt;
        protected const int NoTarget = -1;

        Genome[] genomes;
        public float[][] output;
        public float[][] input;
        public Dictionary<int, BrainType> brainTypes = new Dictionary<int, BrainType>();

        public SimAgent()
        {
        }

        public SimAgent(SimAgentTypes agentType)
        {
            this.agentType = agentType;
        }

        public virtual void Init()
        {
            Food = 0;
            Fsm = new FSM<Behaviours, Flags>();
            output = new float[brainTypes.Count][];
            foreach (BrainType brain in brainTypes.Values)
            {
                NeuronInputCount inputsCount =
                    DataContainer.InputCountCache[(brain, agentType)];
                output[GetBrainTypeKeyByValue(brain)] = new float[inputsCount.outputCount];
            }

            OnMove += Move;
            OnEat += Eat;

            FsmBehaviours();

            FsmTransitions();
            Fsm.ForceTransition(Behaviours.Walk);
        }

        public virtual void Reset()
        {
            Food = 0;
            Fsm.ForceTransition(Behaviours.Walk);
            CalculateInputs();
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
                int outputCount = DataContainer.InputCountCache[(brainType, agentType)].outputCount;
                output[i] = new float[outputCount];
            }
        }

        public virtual void Uninit()
        {
            OnMove -= Move;
            OnEat -= Eat;
        }

        public void Tick(float deltaTime)
        {
            dt = deltaTime;
            Fsm.Tick();
        }

        public virtual void UpdateInputs()
        {
            FindFoodInputs();
            MovementInputs();
            ExtraInputs();
        }


        protected void FindFoodInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Eat);
            int inputCount = GetInputCount(BrainType.Eat);
            input[brain] = new float[inputCount];

            input[brain][0] = Transform.position.X;
            input[brain][1] = Transform.position.Y;
            INode<IVector> target = GetTarget(foodTarget);

            if (target == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
                return;
            }

            input[brain][2] = target.GetCoordinate().X;
            input[brain][3] = target.GetCoordinate().Y;
        }

        protected virtual void MovementInputs()
        {
        }

        protected virtual void ExtraInputs()
        {
        }

        protected virtual void FsmTransitions()
        {
            WalkTransitions();
            EatTransitions();
            ExtraTransitions();
        }

        protected virtual void WalkTransitions()
        {
        }

        protected virtual void EatTransitions()
        {
        }

        protected virtual void ExtraTransitions()
        {
        }

        protected virtual void FsmBehaviours()
        {
            Fsm.AddBehaviour<SimWalkState>(Behaviours.Walk, WalkTickParameters);
            ExtraBehaviours();
        }

        protected virtual void ExtraBehaviours()
        {
        }

        protected virtual object[] WalkTickParameters()
        {
            int extraBrain = agentType == SimAgentTypes.Carnivore
                ? GetBrainTypeKeyByValue(BrainType.Attack)
                : GetBrainTypeKeyByValue(BrainType.Escape);
            object[] objects =
            {
                CurrentNode, foodTarget, OnMove, output[GetBrainTypeKeyByValue(BrainType.Eat)],
                output[extraBrain]
            };
            return objects;
        }
        
        protected virtual object[] EatTickParameters()
        {
            int extraBrain = agentType == SimAgentTypes.Carnivore
                ? GetBrainTypeKeyByValue(BrainType.Attack)
                : GetBrainTypeKeyByValue(BrainType.Escape);

            object[] objects =
                { CurrentNode, foodTarget, OnEat, output[GetBrainTypeKeyByValue(BrainType.Eat)], output[extraBrain] };
            return objects;
        }

        protected virtual void Eat()
        {
            INode<IVector> currNode = CurrentNode;
            lock (currNode)
            {
                if (currNode.Food <= 0) return;
                Food++;
                currNode.Food--;
                if (currNode.Food <= 0) currNode.NodeType = SimNodeType.Empty;
            }
        }

        protected virtual void Move()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Movement);

            IVector currentPos = new MyVector(CurrentNode.GetCoordinate().X, CurrentNode.GetCoordinate().Y);
            currentPos = CalculateNewPosition(currentPos, output[brain]);

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

            INode<IVector> newPos = DataContainer.CoordinateToNode(currentPos);
            if (newPos != null) SetPosition(newPos.GetCoordinate());
        }

        private IVector CalculateNewPosition(IVector targetPos, float[] brainOutput)
        {
            if (brainOutput.Length < 2) return default;

            targetPos.X += movement * brainOutput[0];
            targetPos.Y += movement * brainOutput[1];

            return targetPos;
        }

        public virtual INode<IVector> GetTarget(SimNodeType nodeType = SimNodeType.Empty)
        {
            return DataContainer.GetNearestNode(nodeType, transform.position);
        }

        protected int GetInputCount(BrainType brainType)
        {
            return InputCountCache.GetInputCount(agentType, brainType);
        }

        public virtual void SetPosition(IVector position)
        {
            if (!DataContainer.graph.IsWithinGraphBorders(position)) return;
            Transform = (TTransform)new ITransform<IVector>(position);
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

            throw new KeyNotFoundException("The value is not present in the brainTypes dictionary.");
        }
    }
}