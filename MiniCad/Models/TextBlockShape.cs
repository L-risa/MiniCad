using System.ComponentModel;
using System.Drawing;

namespace MiniCad.Models;

public class TextBlockShape : Shape
{
    [Category("Текст")]
    [Description("Содержимое текстового блока")]
    public string Text { get; set; } = "Выполнить действие";

    [Category("Текст")]
    [Description("Цвет текста")]
    public Color TextColor { get; set; } = Color.Black;

    [Category("Текст")]
    [Description("Размер шрифта")]
    public float FontSize { get; set; } = 10f;

    public override void Draw(Graphics g)
    {
        using var brush = new SolidBrush(FillColor);
        using var pen = new Pen(StrokeColor, StrokeWidth);
        g.FillRectangle(brush, Bounds);
        g.DrawRectangle(pen, Bounds);

        if (!string.IsNullOrEmpty(Text))
        {
            using var textBrush = new SolidBrush(TextColor);
            using var font = new Font("Arial", FontSize, FontStyle.Regular);
            using var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,       
                LineAlignment = StringAlignment.Center   
            };
            g.DrawString(Text, font, textBrush, Bounds, sf);
        }

        DrawSelectionFrame(g);
    }

    public override bool ContainsPoint(Point p) => Bounds.Contains(p);
}