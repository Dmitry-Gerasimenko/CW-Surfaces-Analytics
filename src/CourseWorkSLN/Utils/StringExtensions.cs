using System.Text.RegularExpressions;

namespace CourseWorkSLN.Utils
{
    public static class StringExtensions
    {
        public static string Optimize(this string str)
        {
            Regex regex = new Regex(@"Pow\((\s*([\d+|\w+])\s*,\s*([\d+|\w+])\s*)\)");

            string optimized = regex.Replace(str, "$2**$3")
                .Replace("Exp", "exp")
                .Replace("Sqrt", "sqrt")
                .Replace("Log", "log")
                .Replace("Sin", "sin")
                .Replace("Cos", "cos")
                .Replace("Tan", "tan")
                .Replace("Asin", "arcsin")
                .Replace("Acos", "arccos")
                .Replace("Atan", "arctan");

            return optimized;
        }
    }
}
