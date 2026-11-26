namespace EquationStudio.Web.Components;

internal sealed class GraphViewport(double fitStep)
{
    public const double GridPixels = 44;
    public const double PlotCenterX = 300;
    public const double PlotCenterY = 160;
    private const double ZoomLimit = 4096;
    private const double DragPixelsPerZoom = 150;

    public double CenterX { get; private set; }
    public double CenterY { get; private set; }
    public double XStep { get; private set; } = NiceStep(fitStep);
    public double YStep { get; private set; } = NiceStep(fitStep);

    public double MinX => CenterX - 6 * XStep;
    public double MaxX => CenterX + 6 * XStep;
    public double MinY => CenterY - 3 * YStep;
    public double MaxY => CenterY + 3 * YStep;

    public double ProjectX(double x) => PlotCenterX + (x - CenterX) / XStep * GridPixels;
    public double ProjectY(double y) => PlotCenterY - (y - CenterY) / YStep * GridPixels;

    public void Reset()
    {
        CenterX = 0;
        CenterY = 0;
        XStep = NiceStep(fitStep);
        YStep = XStep;
    }

    public void Pan(double horizontal, double vertical)
    {
        double limit = fitStep * 100_000_000;
        CenterX = Math.Clamp(CenterX - horizontal / GridPixels * XStep, -limit, limit);
        CenterY = Math.Clamp(CenterY + vertical / GridPixels * YStep, -limit, limit);
    }

    public void ZoomX(double horizontal) => XStep = Zoom(XStep, -horizontal, CenterX);

    public void ZoomY(double vertical) => YStep = Zoom(YStep, vertical, CenterY);

    public IEnumerable<double> XTicks() => Ticks(MinX, MaxX, XStep, 13);

    public IEnumerable<double> YTicks() => Ticks(MinY, MaxY, YStep, 7);

    private double Zoom(double step, double pixels, double center)
    {
        double initial = NiceStep(fitStep);
        double minimum = Math.Max(initial / ZoomLimit, Math.Abs(center) * 1e-13);
        return Math.Clamp(step * Math.Exp(pixels / DragPixelsPerZoom), minimum, initial * ZoomLimit);
    }

    private static IEnumerable<double> Ticks(double minimum, double maximum, double step, int limit)
    {
        double interval = NiceStep(step);
        double first = Math.Ceiling(minimum / interval - 1e-10) * interval;
        for (int index = 0; index < limit; index++)
        {
            double value = first + index * interval;
            if (value > maximum + interval * 1e-10)
            {
                yield break;
            }

            yield return Math.Abs(value) < interval * 1e-10 ? 0 : value;
        }
    }

    private static double NiceStep(double target)
    {
        double magnitude = Math.Pow(10, Math.Floor(Math.Log10(target)));
        double normalized = target / magnitude;
        double multiplier = normalized switch { <= 1 => 1, <= 2 => 2, <= 5 => 5, _ => 10 };
        return multiplier * magnitude;
    }
}
