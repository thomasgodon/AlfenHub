namespace AlfenHub.Domain.ValueObjects;

/// <summary>Identifies a socket on the charger. Alfen sockets are numbered from 1.</summary>
public readonly record struct SocketId
{
    public int Value { get; }

    public SocketId(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Socket id must be 1 or greater.");
        }

        Value = value;
    }

    public override string ToString() => Value.ToString();
}
