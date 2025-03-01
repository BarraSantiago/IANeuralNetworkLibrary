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

        public virtual INode<IVector> CurrentNode => DataContainer.GetNode(transform.position);

        public static Action<AnimalAgent<TVector, TTransform>> OnDeath;
        public virtual bool CanReproduce => Food >= FoodLimit;
        public AgentTypes agentType { get; set; }
        public FSM<Behaviours, Flags> Fsm;
        public int FoodLimit { get; protected set; } = 5;
        public int Food { get; protected set; } = 0;
        public float[][] output;
        public float[][] input;
        public Dictionary<int, BrainType> brainTypes = new Dictionary<int, BrainType>();

        public static float Time = 0;
        protected float timer = 0;
        protected NodeTerrain foodTarget;
        protected int speed = 3;
        protected Action OnMove;
        protected Action OnEat;
        protected const int NoTarget = -1;
        protected TTransform transform = new TTransform();
        private Genome[] genomes;
        
        public AnimalAgent()
        {
        }

        public AnimalAgent(AgentTypes agentType)
        {
            this.agentType = agentType;
        }

        Sim2DGraph graph = DataContainer.Graph;
        float minX;
        float maxX;
        float minY;
        float maxY;

        public virtual void Init()
        {
            minX = graph.MinX;
            maxX = graph.MaxX;
            minY = graph.MinY;
            maxY = graph.MaxY;
            Food = 0;
            Fsm = new FSM<Behaviours, Flags>();
            output = new float[brainTypes.Count][];
            foreach (BrainType brain in brainTypes.Values)
            {
                BrainConfiguration inputsCount =
                    DataContainer.InputCountCache[(brain, agentType)];
                output[GetBrainTypeKeyByValue(brain)] = new float[inputsCount.OutputCount];
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
                int outputCount = DataContainer.InputCountCache[(brainType, agentType)].OutputCount;
                output[i] = new float[outputCount];
            }
        }

        public virtual void Uninit()
        {
            OnMove -= Move;
            OnEat -= Eat;
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
            }
            
            ConsoleLogger.ActionDone( agentType + " Action: Eat.");
        }


        protected virtual void Move()
        {
            int movementBrainIndex = GetBrainTypeKeyByValue(BrainType.Movement);

            timer += Time;

            if (speed * timer < 1f)
                return;

            IVector currentCoord = CurrentNode.GetCoordinate();
            MyVector currentPos = new MyVector(currentCoord.X, currentCoord.Y);

            float[] brainOutput = output[movementBrainIndex];
            if (brainOutput.Length < 2)
                return;

            currentPos.X += speed * timer * brainOutput[0];
            currentPos.Y += speed * timer * brainOutput[1];

            // Ensure the new position is within graph borders.
            if (currentPos.X < minX)
                currentPos.X = maxX - 1;
            else if (currentPos.X >= maxX)
                currentPos.X = minX + 1;

            if (currentPos.Y < minY)
                currentPos.Y = maxY - 1;
            else if (currentPos.Y >= maxY)
                currentPos.Y = minY + 1;

            // Get the new node from the graph.
            INode<IVector> newPosNode = DataContainer.GetNode(currentPos);
            if (newPosNode != null)
            {
                // Retrieve the coordinate once.
                IVector newCoord = newPosNode.GetCoordinate();
                SetPosition(newCoord);
            }

            timer = 0;
            
            ConsoleLogger.ActionDone( agentType + " Action: Move.");
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

            ConsoleLogger.Warning($"The BrainType value '{value}' is not present in the '{agentType}' brainTypes dictionary.");
            return -1;
        }
    }
}