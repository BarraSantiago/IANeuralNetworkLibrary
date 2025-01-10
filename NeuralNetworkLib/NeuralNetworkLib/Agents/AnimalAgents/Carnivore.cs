using NeuralNetworkLib.Agents.States.AnimalStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.AnimalAgents
{
    public class Carnivore<TVector, TTransform> : AnimalAgent<TVector, TTransform>
        where TTransform : ITransform<IVector>, new()
        where TVector : IVector, IEquatable<TVector>
    {
        public Action OnAttack { get; set; }
        public bool HasAttacked { get; private set; }
        public bool HasKilled { get; private set; }
        public int DamageDealt { get; private set; } = 0;

        private uint target;
        private bool isTargetAnimal;
        private IVector targetPosition;

        public override void Init()
        {
            base.Init();
            FoodLimit = 1;
            movement = 2;
            HasAttacked = false;
            HasKilled = false;

            CalculateInputs();
            OnAttack += Attack;
        }

        public override void Uninit()
        {
            base.Uninit();
            OnAttack -= Attack;
        }

        public override void Reset()
        {
            base.Reset();
            HasAttacked = false;
            HasKilled = false;
            DamageDealt = 0;
        }

        public override void UpdateInputs()
        {
            (uint, bool) nearestPrey = DataContainer.GetNearestPrey(Transform.position);
            target = nearestPrey.Item1;
            isTargetAnimal = nearestPrey.Item2;
            targetPosition = DataContainer.GetPosition(target, isTargetAnimal);
            FindFoodInputs();
            MovementInputs();
            ExtraInputs();
        }

        protected override void ExtraInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Attack);
            int inputCount = GetInputCount(BrainType.Attack);
            input[brain] = new float[inputCount];

            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;

            if (target == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
                return;
            }

            input[brain][2] = targetPosition.X;
            input[brain][3] = targetPosition.Y;
        }

        protected override void MovementInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Movement);
            int inputCount = GetInputCount(BrainType.Movement);

            input[brain] = new float[inputCount];
            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;

            INode<IVector> nodeTarget = GetTarget(foodTarget);


            if (target == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
            }
            else
            {
                input[brain][2] = targetPosition.X;
                input[brain][3] = targetPosition.Y;
            }

            if (nodeTarget == null)
            {
                input[brain][4] = NoTarget;
                input[brain][5] = NoTarget;
            }
            else
            {
                input[brain][4] = nodeTarget.GetCoordinate().X;
                input[brain][5] = nodeTarget.GetCoordinate().Y;
            }

            input[brain][6] = Food;
        }

        protected override void ExtraBehaviours()
        {
            Fsm.AddBehaviour<WalkCarnState>(Behaviours.Walk, WalkTickParameters);

            Fsm.AddBehaviour<AttackState>(Behaviours.Attack, AttackTickParameters);
        }

        private object[] AttackTickParameters()
        {
            if (output.Length < 3 || output[GetBrainTypeKeyByValue(BrainType.Movement)].Length < 2)
            {
                return Array.Empty<object>();
            }

            object[] objects =
            {
                OnAttack,
                output[GetBrainTypeKeyByValue(BrainType.Attack)],
                output[GetBrainTypeKeyByValue(BrainType.Movement)][2]
            };
            return objects;
        }

        protected override object[] WalkTickParameters()
        {
            object[] objects =
            {
                CurrentNode, targetPosition, OnMove,
                output[GetBrainTypeKeyByValue(BrainType.Attack)]
            };
            return objects;
        }


        private void Attack()
        {
            if (target <= 0) return;
            if (!Approximately(targetPosition, transform.position, 0.2f)) return;

            DataContainer.Attack(target, isTargetAnimal);
            HasAttacked = true;
            DamageDealt++;

            HasKilled = true;
            Eat();
        }

        protected override void Eat()
        {
            Food++;
        }

        private bool Approximately(IVector coord1, IVector coord2, float tolerance)
        {
            return Math.Abs(coord1.X - coord2.X) <= tolerance && Math.Abs(coord1.Y - coord2.Y) <= tolerance;
        }

        protected override void FsmBehaviours()
        {
            ExtraBehaviours();
        }

        protected override void EatTransitions()
        {
            Fsm.SetTransition(Behaviours.Eat, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnSearchFood, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnAttack, Behaviours.Attack);
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnAttack, Behaviours.Attack);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnSearchFood, Behaviours.Walk);
        }

        protected override void ExtraTransitions()
        {
            Fsm.SetTransition(Behaviours.Attack, Flags.OnAttack, Behaviours.Attack);
            Fsm.SetTransition(Behaviours.Attack, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Attack, Flags.OnSearchFood, Behaviours.Walk);
        }
    }
}