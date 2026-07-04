using Avalonia;
using Avalonia.Media;

namespace FloorplanFit.Desktop.Controls.Preview;

internal static class PreviewLineClipper
{
    private const int Inside = 0;
    private const int Left = 1;
    private const int Right = 2;
    private const int Top = 4;
    private const int Bottom = 8;

    public static void DrawLine(DrawingContext context, Rect bounds, Pen pen, Point start, Point end)
    {
        if (TryClipToBounds(bounds, start, end, out var clippedStart, out var clippedEnd))
        {
            context.DrawLine(pen, clippedStart, clippedEnd);
        }
    }

    internal static bool TryClipToBounds(
        Rect bounds,
        Point start,
        Point end,
        out Point clippedStart,
        out Point clippedEnd)
    {
        clippedStart = start;
        clippedEnd = end;

        if (bounds.Width <= double.Epsilon || bounds.Height <= double.Epsilon ||
            !IsFinite(start) || !IsFinite(end))
        {
            return false;
        }

        var x0 = start.X;
        var y0 = start.Y;
        var x1 = end.X;
        var y1 = end.Y;
        var startCode = ComputeOutCode(bounds, x0, y0);
        var endCode = ComputeOutCode(bounds, x1, y1);

        while (true)
        {
            if ((startCode | endCode) == Inside)
            {
                clippedStart = new Point(x0, y0);
                clippedEnd = new Point(x1, y1);
                return true;
            }

            if ((startCode & endCode) != Inside)
            {
                return false;
            }

            var outsideCode = startCode != Inside ? startCode : endCode;
            double x;
            double y;

            if ((outsideCode & Top) != Inside)
            {
                if (Math.Abs(y1 - y0) <= double.Epsilon)
                {
                    return false;
                }

                x = x0 + ((x1 - x0) * (bounds.Top - y0) / (y1 - y0));
                y = bounds.Top;
            }
            else if ((outsideCode & Bottom) != Inside)
            {
                if (Math.Abs(y1 - y0) <= double.Epsilon)
                {
                    return false;
                }

                x = x0 + ((x1 - x0) * (bounds.Bottom - y0) / (y1 - y0));
                y = bounds.Bottom;
            }
            else if ((outsideCode & Right) != Inside)
            {
                if (Math.Abs(x1 - x0) <= double.Epsilon)
                {
                    return false;
                }

                y = y0 + ((y1 - y0) * (bounds.Right - x0) / (x1 - x0));
                x = bounds.Right;
            }
            else
            {
                if (Math.Abs(x1 - x0) <= double.Epsilon)
                {
                    return false;
                }

                y = y0 + ((y1 - y0) * (bounds.Left - x0) / (x1 - x0));
                x = bounds.Left;
            }

            if (!double.IsFinite(x) || !double.IsFinite(y))
            {
                return false;
            }

            if (outsideCode == startCode)
            {
                x0 = x;
                y0 = y;
                startCode = ComputeOutCode(bounds, x0, y0);
            }
            else
            {
                x1 = x;
                y1 = y;
                endCode = ComputeOutCode(bounds, x1, y1);
            }
        }
    }

    private static int ComputeOutCode(Rect bounds, double x, double y)
    {
        var code = Inside;

        if (x < bounds.Left)
        {
            code |= Left;
        }
        else if (x > bounds.Right)
        {
            code |= Right;
        }

        if (y < bounds.Top)
        {
            code |= Top;
        }
        else if (y > bounds.Bottom)
        {
            code |= Bottom;
        }

        return code;
    }

    private static bool IsFinite(Point point)
        => double.IsFinite(point.X) && double.IsFinite(point.Y);
}
