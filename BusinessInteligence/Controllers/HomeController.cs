using System.Diagnostics;
using Application.Enums;
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
        public async Task<IActionResult> Train(string? returnUrl = null,string? typeOfTraining = null)
        {

            if(typeOfTraining == null)
            {
                TempData["TrainError"] = "Tipo de treino inválido.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Predict));
            }

            if(!int.TryParse(typeOfTraining,out int typeInInt))
            {
                TempData["TrainError"] = "Tipo de treino inválido.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction(nameof(Predict));

            }

            TypeOfTraining? type = Enum.GetValues<TypeOfTraining>().FirstOrDefault(t => (int)t == typeInInt);

            if (type == null)
            {
                TempData["TrainError"] = "Tipo de treino inválido.";
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction(nameof(Predict));
            }

            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Persistence", "Docs", "bakery_sales_revised.csv");
            csvPath = Path.GetFullPath(csvPath);

            var command = new TrainSalesPredictionCommand 
            {
                CsvFilePath = csvPath,
                TrainClassification = (type == TypeOfTraining.ALL || type == TypeOfTraining.CLASSIFICATION),
                TrainForecasting = (type == TypeOfTraining.ALL || type == TypeOfTraining.FORECASTING),
                TrainRegression = (type == TypeOfTraining.ALL || type == TypeOfTraining.REGRESSION)
            };
            var result = await _mediator.Send(command);

            if (result.IsSuccess)
            {
                TempData["TrainSuccess"] = $"Modelos treinados com sucesso! {result.Value.TotalRecords} registos, " +
                    $"{result.Value.UniqueDays} dias, {result.Value.UniqueProducts} produtos, " +
                    $"média de {result.Value.AverageDailySales} artigos/dia.";
            }
            else
            {
                TempData["TrainError"] = result.Error;
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction(nameof(Predict));
        }

        [HttpPost]
        [ActionName("PredictResult")]
        public async Task<IActionResult> PredictPost(int dayOfWeek, int month, string periodDay, string typeOfDay)
        {
            var query = new PredictDailySalesQuery
            {
                DayOfWeek = dayOfWeek,
                Month = month,
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

            ViewBag.SelectedDayOfWeek = dayOfWeek;
            ViewBag.SelectedMonth = month;
            ViewBag.SelectedPeriodDay = periodDay;
            ViewBag.SelectedTypeOfDay = typeOfDay;
            return View("Predict");
        }


        public async Task<IActionResult> Classify()
        {
            var uniqueProductsQuery = new UniqueProductsQuery();

            var resultUniqueProducts = await _mediator.Send(uniqueProductsQuery);


            ViewBag.UniqueProducts = resultUniqueProducts.IsSuccess ? resultUniqueProducts.Value : new List<string>();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ClassifyProduct(int hourOfDay, int dayOfWeek, int month, string periodDay, string typeOfDay)
        {
            var query = new PredictProductQuery
            {
                HourOfDay = hourOfDay,
                DayOfWeek = dayOfWeek,
                Month = month,
                PeriodDay = periodDay,
                TypeOfDay = typeOfDay
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
            {
                ViewBag.ProductPrediction = result.Value;
            }
            else
            {
                ViewBag.ProductError = result.Error;
            }

            var uniqueProductsQuery = new UniqueProductsQuery();

            var resultUniqueProducts = await _mediator.Send(uniqueProductsQuery);


            ViewBag.UniqueProducts = resultUniqueProducts.IsSuccess ? resultUniqueProducts.Value : new List<string>();
            ViewBag.HourOfDay = hourOfDay;
            ViewBag.DayOfWeek = dayOfWeek;
            ViewBag.Month = month;
            ViewBag.PeriodDay = periodDay;
            ViewBag.TypeOfDay = typeOfDay;

            return View("Classify");
        }

        [HttpPost]
        public async Task<IActionResult> ClassifyDayType(int hourOfDay, string periodDay, string item)
        {
            var query = new PredictDayTypeQuery
            {
                HourOfDay = hourOfDay,
                PeriodDay = periodDay,
                Item = item
            };

            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                ViewBag.DayTypePrediction = result.Value;
            else
                ViewBag.DayTypeError = result.Error;

            var uniqueProductsQuery = new UniqueProductsQuery();

            var resultUniqueProducts = await _mediator.Send(uniqueProductsQuery);


            ViewBag.UniqueProducts = resultUniqueProducts.IsSuccess ? resultUniqueProducts.Value : new List<string>();
            ViewBag.DayTypeHourOfDay = hourOfDay;
            ViewBag.DayTypePeriodDay = periodDay;
            ViewBag.DayTypeItem = item;
            return View("Classify");
        }

        public IActionResult Forecast()
        {
            return View();
        }

        public IActionResult Analytics()
        {
            var chartsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "charts");
            var pngFiles = new List<string>();
            var htmlFiles = new List<string>();

            if (Directory.Exists(chartsDir))
            {
                pngFiles = Directory.GetFiles(chartsDir, "*.png")
                    .Select(f => "/charts/" + Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToList();

                htmlFiles = Directory.GetFiles(chartsDir, "*.html")
                    .Where(f => !Path.GetFileName(f).Equals("notebook_report.html", StringComparison.OrdinalIgnoreCase))
                    .Select(f => "/charts/" + Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToList();
            }

            ViewBag.PngCharts = pngFiles;
            ViewBag.HtmlCharts = htmlFiles;
            return View();
        }

        public IActionResult Notebook()
        {
            var reportPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "charts", "notebook_report.html");
            ViewBag.ReportExists = System.IO.File.Exists(reportPath);
            return View();
        }

        [HttpPost]
        [ActionName("ForecastResult")]
        public async Task<IActionResult> ForecastPost(int horizon = 30)
        {
            var query = new ForecastSalesQuery { Horizon = horizon };
            var result = await _mediator.Send(query);

            if (result.IsSuccess)
                ViewBag.Forecast = result.Value;
            else
                ViewBag.ForecastError = result.Error;

            ViewBag.SelectedHorizon = horizon;
            return View("Forecast");
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
