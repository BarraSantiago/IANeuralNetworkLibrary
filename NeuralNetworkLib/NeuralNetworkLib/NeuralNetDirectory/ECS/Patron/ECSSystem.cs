namespace NeuralNetworkLib.NeuralNetDirectory.ECS.Patron
{
    public abstract class ECSSystem
    {
        public void Run(float deltaTime)
        {
            PreExecute(deltaTime);
            Execute(deltaTime);
            PostExecute(deltaTime);
        }

        public abstract void Initialize();

        public virtual void Deinitialize()
        {
            
        }

        protected abstract void PreExecute(float deltaTime);

        protected abstract void Execute(float deltaTime);

        protected abstract void PostExecute(float deltaTime);
    }
}