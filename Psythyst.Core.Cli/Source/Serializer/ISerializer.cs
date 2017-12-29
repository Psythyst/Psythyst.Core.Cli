namespace Psythyst.Core.Cli
{
    /// <summary>
    /// ISerializer Interface.
    /// </summary>
    public interface ISerializer<T>
    {
        string Serialize(T Model);

        T Deserialize(string Value);
    }
}