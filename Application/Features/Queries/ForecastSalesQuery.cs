using Application.Classes;
using Application.Core;
using Application.Interfaces;
using Application.Models;

namespace Application.Features.Queries
{
    public class ForecastSalesQuery : IRequest<Result<ForecastResult>>
    {
        public int Horizon { get; set; } = 30;

        public class Handler : IRequestHandler<ForecastSalesQuery, Result<ForecastResult>>
        {
            private readonly SalesTrainer _trainer;

            public Handler(SalesTrainer trainer)
            {
                _trainer = trainer;
            }

            public Task<Result<ForecastResult>> Handle(ForecastSalesQuery request, CancellationToken cancellationToken)
            {
                if (!_trainer.IsTrained)
                {
                    return Task.FromResult(Result<ForecastResult>.Failure("Modelo ainda não treinado. Por favor, treine o modelo primeiro."));
                }

                try
                {
                    var result = _trainer.Forecast(request.Horizon);
                    return Task.FromResult(Result<ForecastResult>.Success(result));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(Result<ForecastResult>.Failure($"Erro na previsão temporal: {ex.Message}"));
                }
            }
        }
    }
}
