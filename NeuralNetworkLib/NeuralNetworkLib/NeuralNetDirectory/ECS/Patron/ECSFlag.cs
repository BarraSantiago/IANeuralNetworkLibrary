namespace NeuralNetworkLib.NeuralNetDirectory.ECS.Patron
{
    [Flags]
    public enum FlagType
    {
        None = 0,
        Miner = 1 << 0,
        Caravan = 1 << 1,
        Target = 1 << 2,
        TypeD = 1 << 3,
        TypeE = 1 << 4
    }

    public class ECSFlag
    {
        protected ECSFlag(FlagType flagType)
        {
            Flag = flagType;
        }

        public uint EntityOwnerID { get; set; } = 0;

        public FlagType Flag { get; set; }

        public virtual void Dispose()
        {
        }
    }
}