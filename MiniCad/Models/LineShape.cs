using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MiniCad.Models;

public class LineShape : Shape
{
    [Category("Конечная позиция линии")]
    [Description("Координата X конца стрелки")]
    public int EndX { get; set; }

    [Category("Конечная позиция линии")]
    [Description("Координата Y конца стрелки")]
    public int EndY { get; set; }

    private const int MarkerSize = 8;

    public override void Draw(Graphics g)
    {
        using var pen = new Pen(StrokeColor, StrokeWidth);
        pen.CustomEndCap = new AdjustableArrowCap(5, 5);
        g.DrawLine(pen, X, Y, EndX, EndY);

        if (IsSelected)
        {
            using var brush = new SolidBrush(Color.FromArgb(37, 99, 235));
            g.FillRectangle(brush, GetStartMarkerBounds());
            g.FillRectangle(brush, GetEndMarkerBounds());
        }
    }

    public override bool ContainsPoint(Point p)
    {
        if (IsStartMarkerHit(p) || IsEndMarkerHit(p)) return true;

        using var path = new GraphicsPath();
        using var testPen = new Pen(Color.Black, Math.Max(StrokeWidth, 8f));
        path.AddLine(X, Y, EndX, EndY);

        return path.IsOutlineVisible(p, testPen);
    }

    public bool IsStartMarkerHit(Point p) => GetStartMarkerBounds().Contains(p);

    public bool IsEndMarkerHit(Point p) => GetEndMarkerBounds().Contains(p);

    private Rectangle GetStartMarkerBounds() =>
        new Rectangle(X - MarkerSize / 2, Y - MarkerSize / 2, MarkerSize, MarkerSize);

    private Rectangle GetEndMarkerBounds() =>
        new Rectangle(EndX - MarkerSize / 2, EndY - MarkerSize / 2, MarkerSize, MarkerSize);

    public override void Move(int deltaX, int deltaY)
    {
        base.Move(deltaX, deltaY);
        EndX += deltaX;
        EndY += deltaY;
    }
}