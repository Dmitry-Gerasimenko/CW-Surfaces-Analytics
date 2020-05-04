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
        private const double DefStep = 0.6;

        public IActionResult Index()
        {
            return View(new SurfaceInfoViewModel
            {
                Name = "Pow(x, 2) + Pow(y, 2) + Exp(x) + Exp(y)",
                XStart = -10,
                XEnd = 10,
                YStart = -10,
                YEnd = 10,
                Steps = 2000,
                IsParallel = true,
                ThreadsCount = 1,
            });
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
                    var approximateViewResult = GetAreaWithParallel(model);
                    var exactAreaResult = await GetExactArea(model);

                    var absAccuracy = Math.Abs(approximateViewResult.Result - exactAreaResult);
                    var relAccuracy = (absAccuracy / exactAreaResult) * 100;

                    var analyticsViewModel = new AnalyticsResultViewModel
                    {
                        RelativeAccuracy = relAccuracy,
                        AbsoluteAccuracy = absAccuracy,
                        ApproximateResult = approximateViewResult,
                        ExactArea = exactAreaResult,
                    };

                    return PartialView("_AnalyticsResultPartial", analyticsViewModel);
                }
                else
                {
                    var approximateViewResult = GetAreaWithoutParallel(model);
                    var exactAreaResult = await GetExactArea(model);

                    var absAccuracy = Math.Abs(approximateViewResult.Result - exactAreaResult);
                    var relAccuracy = (absAccuracy / exactAreaResult) * 100;

                    var analyticsViewModel = new AnalyticsResultViewModel
                    {
                        RelativeAccuracy = relAccuracy,
                        AbsoluteAccuracy = absAccuracy,
                        ApproximateResult = approximateViewResult,
                        ExactArea = exactAreaResult,
                    };

                    return PartialView("_AnalyticsResultPartial", analyticsViewModel);
                }
            }
            catch (Exception ex)
            {
                HttpContext.Response.StatusCode = 500;
                ModelState.AddModelError(string.Empty, ex.Message);
                return View("_AreaErrorPartial", ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult> GetTimeChart(SurfaceInfoViewModel model)
        {
            string outFileName = $"{DateTime.Now.ToString("yymmssfff")}-performance-testing.txt";

            var timeArr = new List<long>();
            var threadsArr = new [] { 1, 2, 3, 5, 10, 15, 30 };

            foreach (var thrdCount in threadsArr)
            {
                var tasks = new List<Task<double>>();
                double hX = (model.XEnd - model.XStart) / thrdCount;
                double hY = (model.YEnd - model.YStart) / thrdCount;

                for (int i = 0; i < thrdCount; i++)
                {
                    double xnStep = model.XStart + hX * i;
                    double xkStep = model.XStart + hX * (i + 1);

                    tasks.Add(new Task<double>(() =>
                        SurfaceAreaService.CalculateSurfaceArea(
                            xnStep, xkStep,
                            model.YStart, model.YEnd,
                            model.Steps / thrdCount,
                            outFileName)));
                }

                var startTime = Stopwatch.StartNew();

                tasks.ForEach(t => t.Start());
                Task.WaitAll(tasks.ToArray());
                var res = tasks.Sum(t => t.Result);

                startTime.Stop();

                timeArr.Add(startTime.ElapsedMilliseconds);
            }

            return new ObjectResult( new { threadsArr = threadsArr, timeArr = timeArr.ToArray() });
        }

        [HttpPost]
        public async Task<ActionResult> GetAccuracyChart(SurfaceInfoViewModel model)
        {
            var exactAreaResult = await GetExactArea(model);
            string outFileName = $"{DateTime.Now.ToString("yymmssfff")}-accuracy-testing.txt";

            var accuracyArr = new List<double>();
            var threadsArr = new[] { 1, 2, 3, 5, 10, 15, 30 };

            foreach (var thrdCount in threadsArr)
            {
                var tasks = new List<Task<double>>();
                double hX = (model.XEnd - model.XStart) / thrdCount;
                double hY = (model.YEnd - model.YStart) / thrdCount;

                for (int i = 0; i < thrdCount; i++)
                {
                    double xnStep = model.XStart + hX * i;
                    double xkStep = model.XStart + hX * (i + 1);

                    tasks.Add(new Task<double>(() =>
                        SurfaceAreaService.CalculateSurfaceArea(
                            xnStep, xkStep,
                            model.YStart, model.YEnd,
                            model.Steps / thrdCount,
                            outFileName)));
                }
                tasks.ForEach(t => t.Start());
                Task.WaitAll(tasks.ToArray());

                var res = tasks.Sum(t => t.Result);
            

                accuracyArr.Add(Math.Abs(res - exactAreaResult));
            }

            return new ObjectResult(new { threadsArr, accuracyArr = accuracyArr.ToArray() });
        }

        private async Task<double> GetExactArea(SurfaceInfoViewModel model)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri("http://localhost:5000");

            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("xstart", Convert.ToString(model.XStart)),
                new KeyValuePair<string, string>("xend", Convert.ToString(model.XEnd)),
                new KeyValuePair<string, string>("ystart", Convert.ToString(model.YStart)),
                new KeyValuePair<string, string>("yend", Convert.ToString(model.YEnd)),
                new KeyValuePair<string, string>("step", Convert.ToString(DefStep)),
            });

            var response = await client.PostAsync("get_exact_data", formContent);
            if (response.IsSuccessStatusCode)
            {
                return Convert.ToDouble(await response.Content.ReadAsStringAsync());
            }
            throw new InvalidOperationException("Невозможно найти точную площадь");
        }

        private AreaResultViewModel GetAreaWithoutParallel(SurfaceInfoViewModel model)
        {
            string outFileName = $"{DateTime.Now.ToString("yymmssfff")}-thread-calculations.txt";

            var startTime = Stopwatch.StartNew();
            double res = SurfaceAreaService.CalculateSurfaceArea(model.XStart, model.XEnd,
                                                                 model.YStart, model.YEnd,
                                                                 model.Steps,
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
                        model.Steps /  model.ThreadsCount,
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