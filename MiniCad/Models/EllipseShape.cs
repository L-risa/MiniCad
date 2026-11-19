namespace MiniCad.Models;

public class EllipseShape : Shape
{
    public override void Draw(Graphics g)
    {
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        g.FillEllipse(brush, Bounds);
        g.DrawEllipse(pen, Bounds);
        DrawSelectionFrame(g);
    }

    public override bool ContainsPoint(Point p)
    {
        float rx = Width / 2.0f;
        float ry = Height / 2.0f;
        if (rx <= 0 || ry <= 0) return false;
        float cx = X + rx;
        float cy = Y + ry;
        return (Math.Pow(p.X - cx, 2) / (rx * rx)) + (Math.Pow(p.Y - cy, 2) / (ry * ry)) <= 1.0;
    }
}