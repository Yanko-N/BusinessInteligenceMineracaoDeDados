using Application.Interfaces;
namespace Application.Core
{
    /// <summary>
    /// Default mediator implementation that resolves and executes handlers.
    /// </summary>
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var requestType = request.GetType();
            var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));

            var handler = _serviceProvider.GetService(handlerType);

            if (handler == null)
                throw new InvalidOperationException($"No handler registered for request type '{requestType.Name}'");

            var handleMethod = handlerType.GetMethod("Handle", new[] { requestType, typeof(CancellationToken) });

            if (handleMethod == null)
                throw new InvalidOperationException($"Handler for '{requestType.Name}' does not have a Handle method.");

            var result = await (Task<TResponse>)handleMethod.Invoke(handler, new object[] { request, cancellationToken })!;
            return result;
        }
    }
}