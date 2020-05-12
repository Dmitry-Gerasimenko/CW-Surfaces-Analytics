using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CourseWorkSLN.Models;
using System.Net.Http;
using BusinessLogicLayer.Services;
using System.Linq;
using CourseWorkSLN.Utils;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;

namespace CourseWorkSLN.Controllers
{
    public class HomeController : Controller
    {
        private const double DefStep = 0.7;

        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<ActionResult> InitData()
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");

            var response = await client.GetAsync("init_data");
            return Content(await response.Content.ReadAsStringAsync());
        }

        [HttpGet]
        public IActionResult SolveTask()
        {
            return View(new SurfaceInfoViewModel
            {
                Name = "Pow(x, 2) + 2*Pow(y, 2) + Sqrt(Pow(y,2))",
                XStart = -5,
                XEnd = 10,
                YStart = -5,
                YEnd = 10,
                Steps = 1000,
                IsParallel = true,
                ThreadsCount = 1,
            });
        }

        [HttpPost]
        public async Task<ActionResult> SolveTaskChart(SurfaceInfoViewModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest("Заполните обязательные поля корректно.");
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("name", model.Name.Optimize()),
                new KeyValuePair<string, string>("xstart", Convert.ToString(model.XStart)),
                new KeyValuePair<string, string>("xend", Convert.ToString(model.XEnd)),
                new KeyValuePair<string, string>("ystart", Convert.ToString(model.YStart)),
                new KeyValuePair<string, string>("yend", Convert.ToString(model.YEnd)),
                new KeyValuePair<string, string>("step", Convert.ToString(DefStep)),
            });

            var response = await client.PostAsync("get_data", formContent);
            if(response.IsSuccessStatusCode)
            {
                return Content(await response.Content.ReadAsStringAsync());
            }

            HttpContext.Response.StatusCode = 500;
            return View("_PlotErrorPartial", response.ReasonPhrase);
        }

        [HttpPost]
        public async Task<ActionResult> SolveTaskArea(SurfaceInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Заполните обязательные поля корректно.");
            }

            try
            {
                if (model.IsParallel)
                {
                    var viewResult = GetAreaWithParallel(model);

                    return View("_AreaPartial", viewResult);
                }
                else
                {
                    var viewResult = GetAreaWithoutParallel(model);

                    return View("_AreaPartial", viewResult);
                }
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("_AreaErrorPartial", ex.Message);
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddDays(1) }
            );

            return LocalRedirect(returnUrl);
        }

        private AreaResultViewModel GetAreaWithoutParallel(SurfaceInfoViewModel model)
        {
            var startTime = Stopwatch.StartNew();
            double res = SurfaceAreaService.CalculateSurfaceAreaNamed(model.XStart, model.XEnd,
                                                                      model.YStart, model.YEnd,
                                                                      model.Steps,
                                                                      model.Name);

            startTime.Stop();

            var resultTime = startTime.Elapsed;
            return new AreaResultViewModel { Result = res, Description = resultTime.ToString(),};
        }

        private AreaResultViewModel GetAreaWithParallel(SurfaceInfoViewModel model)
        {
            var tasks = new List<Task<double>>();
            double hX = (model.XEnd - model.XStart) / model.ThreadsCount;
            double hY = (model.YEnd - model.YStart) / model.ThreadsCount;

            for (int i = 0; i < model.ThreadsCount; i++)
            {
                double xnStep = model.XStart + hX * i;
                double xkStep = model.XStart + hX * (i + 1);

                tasks.Add(new Task<double>(() =>
                    SurfaceAreaService.CalculateSurfaceAreaNamed(
                        xnStep, xkStep,
                        model.YStart, model.YEnd,
                        model.Steps / model.ThreadsCount,
                        model.Name)));
            }

            var startTime = Stopwatch.StartNew();

            tasks.ForEach(t => t.Start());
            Task.WaitAll(tasks.ToArray());

            var res = tasks.Sum(t => t.Result);

            startTime.Stop();

            var resultTime = startTime.Elapsed;
            return new AreaResultViewModel { Result = res, Description = resultTime.ToString(), };
        }
    }
}
