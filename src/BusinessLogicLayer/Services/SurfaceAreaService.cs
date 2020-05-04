using NCalc;
using System;
using System.IO;
using System.Text;

namespace BusinessLogicLayer.Services
{
    public static class SurfaceAreaService
    {
        public const double BoundOneCoefficient = 1;
        public const double BoundHalfCoefficient = 0.5;
        public const double BoundQuarterCoefficient = 0.25;
        public const double H = 1E-9;

        public static object Locker { get; set; } = new object();

        public static double F(double x, double y)
        {
            return Math.Sqrt(1 + Math.Pow(Math.Exp(x) + 2 * x, 2) + Math.Pow(Math.Exp(y) + 2 * y, 2));
        }

        public static double FNamedAnalytics(double x, double y, Expression expression)
        {
            return Math.Sqrt(1 + Math.Pow(FNamed_diff_x(x, y, expression), 2) + Math.Pow(FNamed_diff_y(x, y,expression), 2));
        }

        public static double FNamed(double x, double y, Expression expression)
        {
            expression.Parameters["x"] = x;
            expression.Parameters["y"] = y;

            return Convert.ToDouble(expression.Evaluate());
        }

        public static double FNamed_diff_x(double x, double y, Expression expression)
        {
            return ( FNamed(x + H, y, expression) - FNamed(x, y, expression) ) / H;
        }

        public static double FNamed_diff_y(double x, double y, Expression expression)
        {
            return (FNamed(x, y + H, expression) - FNamed(x, y, expression)) / H;
        }

        public static double CalculateSurfaceArea(
            double xn,
            double xk,
            double yn,
            double yk,
            int n,
            string outputFileName)
        {
            double h1 = (xk - xn) / n; // x step
            double h2 = (yk - yn) / n; //  y step

            double x, y, w, sum = 0;

            for (int i = 0; i <= n; i++)
            {
                x = xn + i * h1;

                for (int j = 0; j <= n; j++)
                {
                    if (i > 0 && i < n && j > 0 && j < n)
                    {
                        w = BoundOneCoefficient;
                    }
                    else if ((i == 0 || i == n) && (j == 0 || j == n))
                    {
                        w = BoundQuarterCoefficient;
                    }
                    else
                    {
                        w = BoundHalfCoefficient;
                    }

                    y = yn + j * h2;
                    sum += w * F(x, y);
                }
            }

            double result = h1 * h2 * sum;

            lock ( Locker )
            {
                WriteResultToFile(result, outputFileName);
            }
            return result;
        }

        public static double CalculateSurfaceAreaNamed(
            double xn,
            double xk,
            double yn,
            double yk,
            double n, string name)
        {
            Expression expression = new Expression(name);

            double h1 = (xk - xn) / n; // x step
            double h2 = (yk - yn) / n; //  y step

            double x, y, w, sum = 0;

            for (int i = 0; i <= n; i++)
            {
                x = xn + i * h1;

                for (int j = 0; j <= n; j++)
                {
                    if (i > 0 && i < n && j > 0 && j < n)
                    {
                        w = BoundOneCoefficient;
                    }
                    else if ((i == 0 || i == n) && (j == 0 || j == n))
                    {
                        w = BoundQuarterCoefficient;
                    }
                    else
                    {
                        w = BoundHalfCoefficient;
                    }

                    y = yn + j * h2;
                    sum += w * FNamedAnalytics(x, y, expression);
                }
            }

            return h1 * h2 * sum;
        }

        public static void WriteResultToFile(double result, string filename)
        {
            using (StreamWriter sw = new StreamWriter(filename, true, Encoding.Default))
            {
                sw.Write(Convert.ToString(result) + ";");
            }
        }
    }
}