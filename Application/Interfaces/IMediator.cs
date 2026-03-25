namespace Application.Interfaces
{
    /// <summary>
    /// Mediator interface for sending requests to handlers.
    /// </summary>
    public interface IMediator
    {
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
    }
}