using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using BusinessLogicLayer.Services;
using CourseWorkSLN.Models;
using CourseWorkSLN.Utils;
using Microsoft.AspNetCore.Mvc;

namespace CourseWorkSLN.Controllers
{
    public class AnalyticsController : Controller
    {
        private const double MaxStep = 2.002;

        public IActionResult Index()
        {
            return View(new SurfaceInfoViewModel
            {
                Name = "Pow(x, 2) + Pow(y, 2) + Exp(x) + Exp(y)",
                XStart = -10,
                XEnd = 10,
                YStart = -10,
                YEnd = 10,
                Step = 2,
                IsParallel = true,
                ThreadsCount = 1,
            });
        }

        public async Task<IActionResult> GetExactData(SurfaceInfoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Заполните обязательные поля корректно.");
            }

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("xstart", Convert.ToString(model.XStart)),
                new KeyValuePair<string, string>("xend", Convert.ToString(model.XEnd)),
                new KeyValuePair<string, string>("ystart", Convert.ToString(model.YStart)),
                new KeyValuePair<string, string>("yend", Convert.ToString(model.YEnd)),
                new KeyValuePair<string, string>("step", Convert.ToString(2.5 - model.Step)),
            });

            var response = await client.PostAsync("get_exact_data", formContent);
            if (response.IsSuccessStatusCode)
            {
                return Content(await response.Content.ReadAsStringAsync());
            }

            return BadRequest(response.ReasonPhrase);
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

        private AreaResultViewModel GetAreaWithoutParallel(SurfaceInfoViewModel model)
        {
            string outFileName = $"{DateTime.Now.ToString("yymmssfff")}-thread-calculations.txt";

            var startTime = Stopwatch.StartNew();
            double res = SurfaceAreaService.CalculateSurfaceArea(model.XStart, model.XEnd,
                                                                 model.YStart, model.YEnd,
                                                                 MaxStep - model.Step,
                                                                 MaxStep - model.Step,
                                                                 outFileName);

            startTime.Stop();

            var resultTime = startTime.Elapsed;
            return new AreaResultViewModel { Result = res, Description = resultTime.ToString(), };
        }

        private AreaResultViewModel GetAreaWithParallel(SurfaceInfoViewModel model)
        {
            string outFileName = $"{DateTime.Now.ToString("yymmssfff")}-thread-calculations.txt";

            var tasks = new List<Task<double>>();
            double hX = (model.XEnd - model.XStart) / model.ThreadsCount;
            double hY = (model.YEnd - model.YStart) / model.ThreadsCount;

            for (int i = 0; i < model.ThreadsCount; i++)
            {
                double xnStep = model.XStart + hX * i;
                double xkStep = model.XStart + hX * (i + 1);

                tasks.Add(new Task<double>(() =>
                    SurfaceAreaService.CalculateSurfaceArea(
                        xnStep, xkStep,
                        model.YStart, model.YEnd,
                        MaxStep - model.Step,
                        MaxStep - model.Step,
                        outFileName)));
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