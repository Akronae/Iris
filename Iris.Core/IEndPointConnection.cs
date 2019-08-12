namespace Iris.Core
{
    public interface IEndPointConnection
    {
        string Id { get; }
        byte ConnectionState { get; set; }
    }
}