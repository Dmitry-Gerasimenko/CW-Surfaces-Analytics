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

        public static object Locker { get; set; } = new object();

        public static double F(double x, double y)
        {
            return Math.Pow(x, 2) + Math.Pow(y, 2) + Math.Exp(x) + Math.Exp(y);
        }

        public static double FNamed(double x, double y, Expression expression)
        {
            expression.Parameters["x"] = x;
            expression.Parameters["y"] = y;

            return Convert.ToDouble(expression.Evaluate());
        }

        public static double CalculateSurfaceArea(
            double xn,
            double xk,
            double yn,
            double yk,
            double h1,
            double h2,
            string outputFileName)
        {
            int xIter = (int)((xk - xn) / h1);
            int yIter = (int)((yk - yn) / h2);

            double x, y, w, sum = 0;

            for (int i = 0; i <= xIter; i++)
            {
                x = xn + i * h1;

                for (int j = 0; j <= yIter; j++)
                {
                    if (i > 0 && i < xIter && j > 0 && j < yIter)
                    {
                        w = BoundOneCoefficient;
                    }
                    else if ((i == 0 || i == yIter) && (j == 0 || j == xIter))
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
            double h1,
            double h2, string name)
        {
            Expression expression = new Expression(name);

            int xIter = (int)((xk - xn) / h1);
            int yIter = (int)((yk - yn) / h2);

            double x, y, w, sum = 0;

            for (int i = 0; i <= xIter; i++)
            {
                x = xn + i * h1;

                for (int j = 0; j <= yIter; j++)
                {
                    if (i > 0 && i < xIter && j > 0 && j < yIter)
                    {
                        w = BoundOneCoefficient;
                    }
                    else if ((i == 0 || i == yIter) && (j == 0 || j == xIter))
                    {
                        w = BoundQuarterCoefficient;
                    }
                    else
                    {
                        w = BoundHalfCoefficient;
                    }

                    y = yn + j * h2;
                    sum += w * FNamed(x, y, expression);
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