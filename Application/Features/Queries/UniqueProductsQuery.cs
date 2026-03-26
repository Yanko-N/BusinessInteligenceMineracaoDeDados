using Application.Classes;
using Application.Core;
using Application.Interfaces;

namespace Application.Features.Queries
{
    public class UniqueProductsQuery : IRequest<Result<List<string>>>
    {
        public class Handler : IRequestHandler<UniqueProductsQuery, Result<List<string>>>
        {
            private readonly SalesTrainer _trainer;
            public Handler(SalesTrainer salesTrainer)
            {
                _trainer = salesTrainer;
            }
            public Task<Result<List<string>>> Handle(UniqueProductsQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    return Task.FromResult(Result<List<string>>.Success(_trainer.UniqueProducts));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(Result<List<string>>.Failure($"Erro ao obter produtos únicos: {ex.Message}"));
                }
            }
        }
    }
}
