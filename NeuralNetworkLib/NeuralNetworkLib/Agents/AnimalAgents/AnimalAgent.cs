using System.Diagnostics;
using NeuralNetworkLib.Agents.SimAgents;
using NeuralNetworkLib.Agents.States.AnimalStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.NeuralNetDirectory.NeuralNet;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.AnimalAgents
{
    public enum Flags
    {
        OnEscape,
        OnEat,
        OnSearchFood,
        OnAttack
    }

    public class AnimalAgent<TVector, TTransform>
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
            DataContainer.Graph.NodesType[(int)Transform.position.X, (int)Transform.position.Y];

        public static Action<AnimalAgent<TVector, TTransform>> OnDeath;
        public virtual bool CanReproduce => Food >= FoodLimit;
        public AgentTypes agentType { get; set; }
        public FSM<Behaviours, Flags> Fsm;
        public int FoodLimit { get; protected set; } = 5;
        public int Food { get; protected set; } = 0;


        protected Stopwatch stopwatch = new Stopwatch();
        protected NodeTerrain foodTarget;
        protected int speed = 3;
        protected Action OnMove;
        protected Action OnEat;
        protected float dt;
        protected const int NoTarget = -1;
        protected TTransform transform = new TTransform();

        Genome[] genomes;
        public float[][] output;
        public float[][] input;
        public Dictionary<int, BrainType> brainTypes = new Dictionary<int, BrainType>();

        public AnimalAgent()
        {
        }

        public AnimalAgent(AgentTypes agentType)
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
                output[GetBrainTypeKeyByValue(brain)] = new float[inputsCount.OutputCount];
            }

            stopwatch.Start();

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
                int outputCount = DataContainer.InputCountCache[(brainType, agentType)].OutputCount;
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


        protected virtual void FindFoodInputs()
        {
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
            Fsm.AddBehaviour<AnimalWalkState>(Behaviours.Walk, WalkTickParameters);
            ExtraBehaviours();
        }

        protected virtual void ExtraBehaviours()
        {
        }

        protected virtual object[] WalkTickParameters()
        {
            int extraBrain = agentType == AgentTypes.Carnivore
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
            int extraBrain = agentType == AgentTypes.Carnivore
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
                if (currNode.Resource <= 0) return;
                Food++;
                currNode.Resource--;
                if (currNode.Resource <= 0) currNode.NodeType = NodeType.Empty;
            }
        }


        protected virtual void Move()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Movement);

            double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
            if (speed*elapsedSeconds < 1) return;

            IVector currentPos = new MyVector(CurrentNode.GetCoordinate().X, CurrentNode.GetCoordinate().Y);
            currentPos = CalculateNewPosition(currentPos, output[brain], (float)elapsedSeconds);

            if (!DataContainer.Graph.IsWithinGraphBorders(currentPos))
            {
                if (currentPos.X <= DataContainer.Graph.MinX)
                {
                    currentPos.X = DataContainer.Graph.MaxX - 1;
                }

                if (currentPos.X >= DataContainer.Graph.MaxX)
                {
                    currentPos.X = DataContainer.Graph.MinX + 1;
                }

                if (currentPos.Y <= DataContainer.Graph.MinY)
                {
                    currentPos.Y = DataContainer.Graph.MaxY - 1;
                }

                if (currentPos.Y >= DataContainer.Graph.MaxY)
                {
                    currentPos.Y = DataContainer.Graph.MinY + 1;
                }
            }

            INode<IVector> newPos = DataContainer.CoordinateToNode(currentPos);
            if (newPos != null) SetPosition(newPos.GetCoordinate());

            stopwatch.Restart();
        }

        private IVector CalculateNewPosition(IVector targetPos, float[] brainOutput, float time)
        {
            if (brainOutput.Length < 2) return default;

            targetPos.X += speed * time * brainOutput[0];
            targetPos.Y += speed * time * brainOutput[1];

            return targetPos;
        }

        protected int GetInputCount(BrainType brainType)
        {
            return InputCountCache.GetInputCount(agentType, brainType);
        }

        public virtual void SetPosition(IVector position)
        {
            if (!DataContainer.Graph.IsWithinGraphBorders(position)) return;
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

            throw new KeyNotFoundException(
                $"The BrainType value '{value}' is not present in the '{agentType}' brainTypes dictionary.");
        }
    }
}