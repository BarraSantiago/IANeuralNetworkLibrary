namespace NeuralNetworkLib.ECS.Patron;

[Flags]
public enum FlagType
{
    None = 0,
    Cart = 1 << 0,
    Gatherer = 1 << 1,
    Builder = 1 << 2,
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