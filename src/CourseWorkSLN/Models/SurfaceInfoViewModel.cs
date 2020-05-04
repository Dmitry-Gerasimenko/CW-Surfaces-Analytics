using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace CourseWorkSLN.Models
{
    public class SurfaceInfoViewModel
    {
        [DisplayName("Вид уравнения")]
        [Required(ErrorMessage = "Уравнение не может быть пустым.", AllowEmptyStrings =false)]
        [MinLength(1, ErrorMessage  = "Введите уравнение")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Введите предел по X.")]
        [DisplayName("Xn")]
        public double XStart { get; set; }

        [Required(ErrorMessage = "Введите предел по X.")]
        [DisplayName("Xk")]
        public double XEnd { get; set; }

        [Required(ErrorMessage = "Введите предел по Y.")]
        [DisplayName("Yn")]
        public double YStart { get; set; }

        [Required(ErrorMessage = "Введите предел по Y.")]
        [DisplayName("Yk")]
        public double YEnd { get; set; }

        [DisplayName("Количество разбиений")]
        public int Steps { get; set; }

        [DisplayName("Параллельные вычисления")]
        public bool IsParallel { get; set; }

        [DisplayName("Потоков:")]
        [Range(1, int.MaxValue, ErrorMessage = "Количество потоков не менее 1.")]
        public int ThreadsCount { get; set; }
    }
}
