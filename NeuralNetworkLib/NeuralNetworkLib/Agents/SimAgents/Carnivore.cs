using NeuralNetworkLib.Agents.States;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.SimAgents
{
    public class Carnivore<TVector, TTransform> : SimAgent<TVector, TTransform>
        where TTransform : ITransform<IVector>, new()
        where TVector : IVector, IEquatable<TVector>
    {
        public Action OnAttack { get; set; }
        public bool HasAttacked { get; private set; }
        public bool HasKilled { get; private set; }

        public int DamageDealt { get; private set; } = 0;

        private SimAgent<IVector, ITransform<IVector>> target;

        public override void Init()
        {
            base.Init();
            foodTarget = SimNodeType.Corpse;
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
            target = DataContainer.GetNearestEntity(SimAgentTypes.Herbivore, Transform.position);
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

            input[brain][2] = target.CurrentNode.GetCoordinate().X;
            input[brain][3] = target.CurrentNode.GetCoordinate().Y;
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
                input[brain][2] = target.CurrentNode.GetCoordinate().X;
                input[brain][3] = target.CurrentNode.GetCoordinate().Y;
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
            Fsm.AddBehaviour<SimEatCarnState>(Behaviours.Eat, EatTickParameters);

            Fsm.AddBehaviour<SimWalkCarnState>(Behaviours.Walk, WalkTickParameters);

            Fsm.AddBehaviour<SimAttackState>(Behaviours.Attack, AttackTickParameters);
        }

        private object[] AttackTickParameters()
        {
            if (output.Length < 3 || output[GetBrainTypeKeyByValue(BrainType.Movement)].Length < 2)
            {
                return Array.Empty<object>();
            }

            int extraBrain = GetBrainTypeKeyByValue(BrainType.Attack);
            object[] objects =
            {
                CurrentNode,
                foodTarget,
                OnMove,
                output[GetBrainTypeKeyByValue(BrainType.Eat)],
                output[extraBrain],
                OnAttack,
                output[GetBrainTypeKeyByValue(BrainType.Eat)],
                output[GetBrainTypeKeyByValue(BrainType.Attack)],
                output[GetBrainTypeKeyByValue(BrainType.Movement)][2]
            };
            return objects;
        }

        protected override object[] WalkTickParameters()
        {
            object[] objects =
            {
                CurrentNode, target?.CurrentNode.GetCoordinate(), foodTarget, OnMove,
                output[GetBrainTypeKeyByValue(BrainType.Eat)], output[GetBrainTypeKeyByValue(BrainType.Attack)]
            };
            return objects;
        }


        private void Attack()
        {
            if (target.agentType != SimAgentTypes.Herbivore ||
                !Approximatly(target.Transform.position, transform.position, 0.2f)) return;
            Herbivore<IVector, ITransform<IVector>> herbivore = (Herbivore<IVector, ITransform<IVector>>)target;
            herbivore.Hp--;
            HasAttacked = true;
            DamageDealt++;
            if (herbivore.Hp <= 0)
            {
                HasKilled = true;
            }
        }

        protected override void Eat()
        {
            INode<IVector> node = CurrentNode;

            lock (node)
            {
                if (node.Food <= 0) return;
                Food++;
                node.Food--;

                if (node.Food > 0) return;

                node.NodeType = SimNodeType.Carrion;
                node.Food = 30;
            }
        }

        private bool Approximatly(IVector coord1, IVector coord2, float tolerance)
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