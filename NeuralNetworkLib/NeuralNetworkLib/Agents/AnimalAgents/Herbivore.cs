using NeuralNetworkLib.Agents.States.AnimalStates;
using NeuralNetworkLib.DataManagement;
using NeuralNetworkLib.Utils;

namespace NeuralNetworkLib.Agents.AnimalAgents
{
    public class Herbivore<TVector, TTransform> : AnimalAgent<TVector, TTransform>
        where TTransform : ITransform<IVector>, new()
        where TVector : IVector, IEquatable<TVector>
    {
        public int Hp
        {
            get => hp;
            set
            {
                hp = value;
                if (hp <= 0) Die();
            }
        }

        private int hp;
        private const int FoodDropped = 1;
        private const int InitialHp = 1;
        private INode<IVector> FoodPosition;
        
        public override void Init()
        {
            base.Init();
            foodTarget = NodeTerrain.Stump;

            CalculateInputs();

            hp = InitialHp;
        }

        public override void Reset()
        {
            base.Reset();
            hp = InitialHp;
        }

        public override void UpdateInputs()
        {
            FoodPosition = GetTarget(foodTarget);
            base.UpdateInputs();
        }

        protected override void ExtraInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Escape);
            int inputCount = GetInputCount(BrainType.Escape);

            input[brain] = new float[inputCount];
            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;
            AnimalAgent<IVector, ITransform<IVector>> target =
                DataContainer.GetNearestEntity(AnimalAgentTypes.Carnivore, Transform.position);
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
        }

        protected override void FindFoodInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Eat);
            int inputCount = GetInputCount(BrainType.Eat);
            input[brain] = new float[inputCount];

            input[brain][0] = Transform.position.X;
            input[brain][1] = Transform.position.Y;
            
            if (FoodPosition == null)
            {
                input[brain][2] = NoTarget;
                input[brain][3] = NoTarget;
                return;
            }

            input[brain][2] = FoodPosition.GetCoordinate().X;
            input[brain][3] = FoodPosition.GetCoordinate().Y;
        }

        protected override void MovementInputs()
        {
            int brain = GetBrainTypeKeyByValue(BrainType.Movement);
            int inputCount = GetInputCount(BrainType.Movement);

            input[brain] = new float[inputCount];
            input[brain][0] = CurrentNode.GetCoordinate().X;
            input[brain][1] = CurrentNode.GetCoordinate().Y;

            AnimalAgent<IVector, ITransform<IVector>> target =
                DataContainer.GetNearestEntity(AnimalAgentTypes.Carnivore, Transform.position);
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
            
            if (FoodPosition == null)
            {
                input[brain][4] = NoTarget;
                input[brain][5] = NoTarget;
            }
            else
            {
                input[brain][4] = FoodPosition.GetCoordinate().X;
                input[brain][5] = FoodPosition.GetCoordinate().Y;
            }

            input[brain][6] = Food;
            input[brain][7] = Hp;
        }
        
        public virtual INode<IVector> GetTarget(NodeTerrain nodeType = NodeTerrain.Stump)
        {
            return DataContainer.GetNearestNode(nodeType, transform.position);
        }


        private void Die()
        {
            OnDeath?.Invoke(this);
        }

        protected override void EatTransitions()
        {
            Fsm.SetTransition(Behaviours.Eat, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnSearchFood, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnEscape, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Eat, Flags.OnAttack, Behaviours.Walk);
        }

        protected override void WalkTransitions()
        {
            Fsm.SetTransition(Behaviours.Walk, Flags.OnEat, Behaviours.Eat);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnEscape, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnAttack, Behaviours.Walk);
            Fsm.SetTransition(Behaviours.Walk, Flags.OnSearchFood, Behaviours.Walk);
        }

        protected override void ExtraTransitions()
        {
        }

        protected override void FsmBehaviours()
        {
            ExtraBehaviours();
        }

        protected override void ExtraBehaviours()
        {
            Fsm.AddBehaviour<AnimalEatState>(Behaviours.Eat, EatTickParameters);
            Fsm.AddBehaviour<AnimalWalkHerbState>(Behaviours.Walk, WalkTickParameters);
        }
    }
}