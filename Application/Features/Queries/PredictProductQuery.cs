using Application.Classes;
using Application.Core;
using Application.Interfaces;
using Application.Models;

namespace Application.Features.Queries
{
    public class PredictProductQuery : IRequest<Result<ClassificationResult>>
    {
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }
        public int Month { get; set; }
        public string PeriodDay { get; set; } = "morning";
        public string TypeOfDay { get; set; } = "weekday";

        public class Handler : IRequestHandler<PredictProductQuery, Result<ClassificationResult>>
        {
            private readonly SalesTrainer _trainer;

            public Handler(SalesTrainer trainer)
            {
                _trainer = trainer;
            }

            public Task<Result<ClassificationResult>> Handle(PredictProductQuery request, CancellationToken cancellationToken)
            {
                if (!_trainer.IsTrained)
                {
                    return Task.FromResult(Result<ClassificationResult>.Failure("Modelo ainda não treinado. Por favor, treine o modelo primeiro."));
                }

                try
                {
                    var result = _trainer.PredictProduct(
                        request.HourOfDay, request.DayOfWeek, request.Month,
                        request.PeriodDay, request.TypeOfDay);
                    return Task.FromResult(Result<ClassificationResult>.Success(result));
                }
                catch (Exception ex)
                {
                    return Task.FromResult(Result<ClassificationResult>.Failure($"Erro na classificação: {ex.Message}"));
                }
            }
        }
    }
}
