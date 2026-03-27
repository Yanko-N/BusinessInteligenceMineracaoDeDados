using Application.Classes;
using Application.Core;
using Application.Interfaces;
using Application.Models;
using Microsoft.Extensions.Logging;

namespace Application.Features.Commands
{
    public class TrainSalesPredictionCommand : IRequest<Result<TrainResult>>
    {
        public string CsvFilePath { get; set; } = string.Empty;
        public bool TrainRegression { get; set; } = true;
        public bool TrainClassification { get; set; } = true;
        public bool TrainForecasting { get; set; } = true;


        public class Handler : IRequestHandler<TrainSalesPredictionCommand, Result<TrainResult>>
        {
            private readonly SalesTrainer _trainer;
            private readonly ILogger<Handler> _logger;

            public Handler(SalesTrainer trainer, ILogger<Handler> logger)
            {
                _trainer = trainer;
                _logger = logger;
            }

            public Task<Result<TrainResult>> Handle(TrainSalesPredictionCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    

                    _logger.LogInformation("Parsing CSV from {Path}", request.CsvFilePath);
                    var sales = CsvParser.ParseCsv(request.CsvFilePath);

                    if (sales.Count == 0)
                    {
                        return Task.FromResult(Result<TrainResult>.Failure("Nenhum dado de vendas encontrado no CSV"));
                    }

                    _logger.LogInformation("Training model with {Count} records...", sales.Count);
                    var result = _trainer.Train(sales,request.TrainRegression,request.TrainClassification,request.TrainForecasting);

                    _logger.LogInformation("Training complete: {Records} records, {Days} days, avg {Avg}/day",
                        result.TotalRecords, result.UniqueDays, result.AverageDailySales);

                    return Task.FromResult(Result<TrainResult>.Success(result));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error training sales prediction model");
                    return Task.FromResult(Result<TrainResult>.Failure($"Erro de treino: {ex.Message}"));
                }
            }
        }
    }
}
