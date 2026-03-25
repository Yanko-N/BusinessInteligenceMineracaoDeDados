using Application.Classes;
using Application.Core;
using Application.Interfaces;
using Application.Models;

namespace Application.Features.Queries
{
    public class PredictDailySalesQuery : IRequest<Result<PredictionResult>>
    {
        public int DayOfWeek { get; set; }
        public string PeriodDay { get; set; } = "morning";
        public string TypeOfDay { get; set; } = "weekday";

        public class Handler : IRequestHandler<PredictDailySalesQuery, Result<PredictionResult>>
        {
            private readonly SalesTrainer _trainer;

            public Handler(SalesTrainer trainer)
            {
                _trainer = trainer;
            }

            public Task<Result<PredictionResult>> Handle(PredictDailySalesQuery request, CancellationToken cancellationToken)
            {
                if (!_trainer.IsTrained){
                    return Task.FromResult(Result<PredictionResult>.Failure("Modelo ainda não treinado. Por favor, treine o modelo primeiro."));
                }

                // Validate weekend/weekday consistency
                var isWeekendDay = request.DayOfWeek == 0 || request.DayOfWeek == 6; // Sunday or Saturday
                var isWeekendType = string.Equals(request.TypeOfDay, "weekend", StringComparison.OrdinalIgnoreCase);

                if (isWeekendDay && !isWeekendType)
                {
                    return Task.FromResult(Result<PredictionResult>.Failure(
                        $"{(DayOfWeek)request.DayOfWeek} é um dia de fim de semana. Por favor, selecione 'Fim de semana' como tipo de dia."));
                }

                if (!isWeekendDay && isWeekendType)
                {
                    return Task.FromResult(Result<PredictionResult>.Failure(
                        $"{(DayOfWeek)request.DayOfWeek} é um dia útil. Por favor, selecione 'Dia útil' como tipo de dia."));
                }

                var result = _trainer.Predict(request.DayOfWeek, request.PeriodDay, request.TypeOfDay);

                if(result == null)
                {
                    return Task.FromResult(Result<PredictionResult>.Failure("O resultado é nulo"));
                }

                return Task.FromResult(Result<PredictionResult>.Success(result));
            }
        }
    }
}
