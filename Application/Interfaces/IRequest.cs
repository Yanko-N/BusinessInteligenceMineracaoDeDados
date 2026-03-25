namespace Application.Interfaces
{
    /// <summary>
    /// Marks a request that should be handled by a handler.
    /// </summary>
    /// <typeparam name="TResponse">The response type returned by the handler.</typeparam>
    public interface IRequest<TResponse>
    {
    }
}