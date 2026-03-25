using System.Diagnostics;
using Application.Features.Commands;
using Application.Features.Queries;
using Application.Interfaces;
using BusinessInteligence.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusinessInteligence.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMediator _mediator;

        public HomeController(ILogger<HomeController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Predict()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Train()
        {
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Persistence", "Docs", "bakery_sales_revised.csv");
            csvPath = Path.GetFullPath(csvPath);

            var command = new TrainSalesPredictionCommand { CsvFilePath = csvPath };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                TempData["TrainSuccess"] = $"Modelo treinado! {result.Value.TotalRecords} registos, " +
                    $"{result.Value.UniqueDays} dias, {result.Value.UniqueProducts} produtos, " +
                    $"média de {result.Value.AverageDailySales} artigos/dia. " +
                    $"R²={result.Value.RSquared}, MAE={result.Value.MAE}, RMSE={result.Value.RMSE}";
            }
            else
            {
                TempData["TrainError"] = result.Error;
            }

            return RedirectToAction(nameof(Predict));
        }

        [HttpPost]
        [ActionName("PredictResult")]
        public async Task<IActionResult> PredictPost(int dayOfWeek, string periodDay, string typeOfDay)
        {
            var query = new PredictDailySalesQuery
            {
                DayOfWeek = dayOfWeek,
                PeriodDay = periodDay,
                TypeOfDay = typeOfDay
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                ViewBag.Prediction = result.Value;
            }
            else
            {
                ViewBag.Error = result.Error;
            }

            return View("Predict");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
