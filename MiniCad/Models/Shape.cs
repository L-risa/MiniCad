using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text.Json.Serialization;

namespace MiniCad.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RectangleShape), "rect")]
[JsonDerivedType(typeof(EllipseShape), "ellipse")]
[JsonDerivedType(typeof(RhombusShape), "rhombus")]
[JsonDerivedType(typeof(TextBlockShape), "text")] 
[JsonDerivedType(typeof(LineShape), "line")]       
public abstract class Shape
{
    private int _width = 80;
    private int _height = 50;

    [Category("Позиция")]
    [Description("Координата X левого верхнего угла")]
    public int X { get; set; }

    [Category("Позиция")]
    [Description("Координата Y левого верхнего угла")]
    public int Y { get; set; }

    [Category("Размеры")]
    public int Width
    {
        get => _width;
        set => _width = value >= 10 ? value : 10;
    }

    [Category("Размеры")]
    public int Height
    {
        get => _height;
        set => _height = value >= 10 ? value : 10;
    }

    [Category("Внешний вид")]
    [Description("Цвет контура фигуры")]
    public Color StrokeColor { get; set; } = Color.Black;

    [Category("Внешний вид")]
    [Description("Цвет заливки фигуры")]
    public Color FillColor { get; set; } = Color.LightSkyBlue;

    [Category("Внешний вид")]
    [Description("Толщина линии контура")]
    public float StrokeWidth { get; set; } = 2.0f;

    [Browsable(false)] 
    public bool IsSelected { get; set; } = false;

    [Browsable(false)]
    public Rectangle Bounds => new Rectangle(X, Y, Width, Height);

    public abstract void Draw(Graphics g);
    public abstract bool ContainsPoint(Point p);

    public virtual void Move(int deltaX, int deltaY)
    {
        X += deltaX;
        Y += deltaY;
    }

    protected void DrawSelectionFrame(Graphics g)
    {
        if (!IsSelected) return;

        using var pen = new Pen(Color.FromArgb(37, 99, 235), 1.5f)
        {
            DashStyle = DashStyle.Dash
        };
        g.DrawRectangle(pen, X - 3, Y - 3, Width + 6, Height + 6);
    }
}