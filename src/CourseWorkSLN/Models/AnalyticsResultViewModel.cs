namespace CourseWorkSLN.Models
{
    public class AnalyticsResultViewModel
    {
        public AreaResultViewModel ApproximateResult  { get; set; }

        public double ExactArea { get; set; }

        public double AbsoluteAccuracy { get; set; }

        public double RelativeAccuracy { get; set; }
    }
}
