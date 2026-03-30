public interface IHackable
{
    bool IsHackable { get; }
    bool TryHack();
}
