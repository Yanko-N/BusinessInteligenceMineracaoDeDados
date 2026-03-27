using Application.Classes;
using Application.Core;
using Application.Interfaces;
using Application.Models;

namespace Application.Features.Queries
{
    public class PredictDayTypeQuery : IRequest<Result<DayTypePredictionResult>>
    {
        public int HourOfDay { get; set; }
        public string PeriodDay { get; set; } = "morning";
        public string Item { get; set; } = "Coffee";

        public class Handler : IRequestHandler<PredictDayTypeQuery, Result<DayTypePredictionResult>>
        {
            private readonly SalesTrainer _trainer;

            public Handler(SalesTrainer trainer)
            {
                _trainer = trainer;
            }

            public Task<Result<DayTypePredictionResult>> Handle(PredictDayTypeQuery request, CancellationToken cancellationToken)
            {
                if (!_trainer.IsTrained)
                {
                    return Task.FromResult(Result<DayTypePredictionResult>.Failure("Modelo ainda não treinado. Por favor, treine o modelo primeiro."));
                }

                try
                {
                    var result = _trainer.PredictDayType(request.HourOfDay, request.PeriodDay, request.Item);
                    return Task.FromResult(Result<DayTypePredictionResult>.Success(result));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(Result<DayTypePredictionResult>.Failure($"Erro na classificação: {ex.Message}"));
                }
            }
        }
    }
}
