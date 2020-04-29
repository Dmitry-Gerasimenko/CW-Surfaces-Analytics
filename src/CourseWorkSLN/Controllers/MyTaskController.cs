using Microsoft.AspNetCore.Mvc;

namespace CourseWorkSLN.Controllers
{
    public class MyTaskController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}