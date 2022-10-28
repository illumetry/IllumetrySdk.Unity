namespace Illumetry {
    public interface IFunc2d {
        double[] Coeffs { get; set; }
        int DoFs { get; }
        double Evaluate(double x, double y);
    }
}