namespace Iris.Core
{
    public interface IEndPointConnection
    {
        string Id { get; }
        int ConnectionState { get; set; }
    }
}