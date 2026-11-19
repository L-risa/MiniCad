using MiniCad.Controls;
using MiniCad.Enums;
using MiniCad.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace MiniCad
{
    public partial class MainForm : Form
    {
        private readonly List<Shape> _shapes = new();
        private Shape _selectedShape = null;
        private Point _dragStartPoint;
        private bool _isDragging = false;
        private ToolMode _currentMode = ToolMode.Select;
        private bool _isDraggingStartPoint = false;
        private bool _isDraggingEndPoint = false;

        private readonly Services.DatabaseService _dbService = new Services.DatabaseService();
        private int? _currentProjectId = null; 

        private CanvasPanel canvas;
        private PropertyGrid propertyGrid;
        private ToolStrip toolStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblCoords;
        private ToolStripStatusLabel lblStatus;

        public MainForm()
        {
            InitializeUI();
            propertyGrid.PropertyValueChanged += (s, e) => canvas.Invalidate();
        }

        private void InitializeUI()
        {
            this.Text = "Mini-CAD / Visual Diagram Editor";
            this.Size = new Size(1150, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            toolStrip = new ToolStrip();

            var btnSelect = new ToolStripButton("↖ Выделение", null, (s, e) => ChangeMode(ToolMode.Select));
            var btnRect = new ToolStripButton("⬛ Прямоугольник", null, (s, e) => ChangeMode(ToolMode.Rectangle));
            var btnEllipse = new ToolStripButton("⚪ Эллипс", null, (s, e) => ChangeMode(ToolMode.Ellipse));
            var btnRhombus = new ToolStripButton("🔷 Ромб", null, (s, e) => ChangeMode(ToolMode.Rhombus));
            var btnText = new ToolStripButton("🔤 Текст (Процесс)", null, (s, e) => ChangeMode(ToolMode.Text));
            var btnLine = new ToolStripButton("🏹 Стрелка связи", null, (s, e) => ChangeMode(ToolMode.Line));

            var btnSaveDb = new ToolStripButton("🗄️ Синхронизировать с БД", null, btnSaveDb_Click);
            var btnOpenDb = new ToolStripButton("🗂️ Каталог схем (БД)", null, btnOpenDb_Click);

            var btnDeleteSelected = new ToolStripButton("❌ Удалить выбранное", null, btnDeleteSelected_Click);
            var btnClear = new ToolStripButton("🗑 Очистить всё", null, (s, e) =>
            {
                _shapes.Clear();
                _selectedShape = null;
                propertyGrid.SelectedObject = null;
                canvas.Invalidate();
                UpdateStatus();
            });

            var btnSave = new ToolStripButton("💾 Сохранить JSON", null, btnSaveJson_Click);
            var btnLoad = new ToolStripButton("📂 Загрузить JSON", null, btnLoadJson_Click);
            var btnExport = new ToolStripButton("🖼 Экспорт PNG", null, btnExportPng_Click);

            toolStrip.Items.AddRange(new ToolStripItem[] {
                btnSelect, btnRect, btnEllipse, btnRhombus, btnText, btnLine,
                new ToolStripSeparator(),
                btnDeleteSelected, btnClear,
                new ToolStripSeparator(),
                btnSave, btnLoad, btnExport,
                new ToolStripSeparator(),
                btnSaveDb, btnOpenDb
            });
            this.Controls.Add(toolStrip);

            propertyGrid = new PropertyGrid { Dock = DockStyle.Right, Width = 280 };
            this.Controls.Add(propertyGrid);

            canvas = new CanvasPanel { Dock = DockStyle.Fill, BackColor = Color.White };
            canvas.Paint += canvas_Paint;
            canvas.MouseDown += canvas_MouseDown;
            canvas.MouseMove += canvas_MouseMove;
            canvas.MouseUp += canvas_MouseUp;
            this.Controls.Add(canvas);

            statusStrip = new StatusStrip();
            lblStatus = new ToolStripStatusLabel("Режим: Выделение | Фигур на холсте: 0   ");
            lblCoords = new ToolStripStatusLabel("X: 0, Y: 0");
            statusStrip.Items.AddRange(new ToolStripItem[] { lblStatus, lblCoords });
            this.Controls.Add(statusStrip);

            ChangeMode(ToolMode.Select);
        }

        private void ChangeMode(ToolMode mode)
        {
            _currentMode = mode;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            lblStatus.Text = $"Режим: {_currentMode} | Фигур на холсте: {_shapes.Count}   ";
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach (var shape in _shapes)
            {
                shape.Draw(e.Graphics);
            }
        }

        private void canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (_currentMode == ToolMode.Select)
            {
                if (_selectedShape is LineShape line && line.IsSelected)
                {
                    if (line.IsStartMarkerHit(e.Location))
                    {
                        _isDraggingStartPoint = true;
                        _dragStartPoint = e.Location;
                        return;
                    }
                    if (line.IsEndMarkerHit(e.Location))
                    {
                        _isDraggingEndPoint = true;
                        _dragStartPoint = e.Location;
                        return;
                    }
                }

                if (_selectedShape != null) _selectedShape.IsSelected = false;

                _selectedShape = _shapes.LastOrDefault(s => s.ContainsPoint(e.Location));

                if (_selectedShape != null)
                {
                    _selectedShape.IsSelected = true;

                    if (_selectedShape is LineShape newLine)
                    {
                        if (newLine.IsStartMarkerHit(e.Location)) _isDraggingStartPoint = true;
                        else if (newLine.IsEndMarkerHit(e.Location)) _isDraggingEndPoint = true;
                        else _isDragging = true;
                    }
                    else
                    {
                        _isDragging = true;
                    }

                    _dragStartPoint = e.Location;
                    propertyGrid.SelectedObject = _selectedShape;
                }
                else
                {
                    propertyGrid.SelectedObject = null;
                }
                canvas.Invalidate();
            }
            else
            {
                Shape? newShape = _currentMode switch
                {
                    ToolMode.Rectangle => new RectangleShape { X = e.X, Y = e.Y },
                    ToolMode.Ellipse => new EllipseShape { X = e.X, Y = e.Y },
                    ToolMode.Rhombus => new RhombusShape { X = e.X, Y = e.Y },
                    ToolMode.Text => new TextBlockShape { X = e.X, Y = e.Y, Width = 120, Height = 60 },
                    ToolMode.Line => new LineShape { X = e.X, Y = e.Y, EndX = e.X + 80, EndY = e.Y + 50 },
                    _ => null
                };

                if (newShape != null)
                {
                    _shapes.Add(newShape);
                    UpdateStatus();
                    canvas.Invalidate();
                }
            }
        }

        private void canvas_MouseMove(object sender, MouseEventArgs e)
        {
            lblCoords.Text = $"X: {e.X}, Y: {e.Y}";

            if (_selectedShape != null)
            {
                if (_isDraggingStartPoint && _selectedShape is LineShape lineStart)
                {
                    lineStart.X = e.X;
                    lineStart.Y = e.Y;
                    propertyGrid.Refresh();
                    canvas.Invalidate();
                }
                else if (_isDraggingEndPoint && _selectedShape is LineShape lineEnd)
                {
                    lineEnd.EndX = e.X;
                    lineEnd.EndY = e.Y;
                    propertyGrid.Refresh();
                    canvas.Invalidate();
                }
                else if (_isDragging)
                {
                    int dx = e.X - _dragStartPoint.X;
                    int dy = e.Y - _dragStartPoint.Y;
                    _selectedShape.Move(dx, dy);
                    _dragStartPoint = e.Location;
                    propertyGrid.Refresh();
                    canvas.Invalidate();
                }
            }
        }

        private void canvas_MouseUp(object sender, MouseEventArgs e)
        {
            _isDragging = false;
            _isDraggingStartPoint = false;
            _isDraggingEndPoint = false;
        }

        private void btnDeleteSelected_Click(object sender, EventArgs e)
        {
            if (_selectedShape != null)
            {
                _shapes.Remove(_selectedShape);
                _selectedShape = null;
                propertyGrid.SelectedObject = null;
                UpdateStatus();
                canvas.Invalidate();
            }
            else
            {
                MessageBox.Show("Сначала выделите фигуру кликом мыши!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void btnSaveJson_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "JSON схема (*.json)|*.json" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(_shapes, options);
                    File.WriteAllText(sfd.FileName, json);
                    MessageBox.Show("Схема успешно сохранена!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        private void btnLoadJson_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog { Filter = "JSON схема (.json)|.json" })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string json = File.ReadAllText(ofd.FileName);
                        var loaded = JsonSerializer.Deserialize<List<Shape>>(json);
                        if (loaded != null)
                        {
                            _shapes.Clear();
                            _shapes.AddRange(loaded);
                            _selectedShape = null;
                            propertyGrid.SelectedObject = null;
                            UpdateStatus();
                            canvas.Invalidate();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при чтении файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
        private void btnExportPng_Click(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog { Filter = "PNG изображение (.png)|.png" })
            {
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    using (var bmp = new Bitmap(canvas.Width, canvas.Height))
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        foreach (var s in _shapes)
                        {
                            s.Draw(g);
                        }
                        bmp.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png);
                        MessageBox.Show("Изображение успешно экспортировано!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        private async void btnSaveDb_Click(object sender, EventArgs e)
        {
            if (_shapes.Count == 0)
            {
                MessageBox.Show("Холст пуст! Нарисуйте фигуры перед сохранением.", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string defaultName = _currentProjectId.HasValue ? "Пересохранить проект" : "Моя блок-схема";
            string title = Microsoft.VisualBasic.Interaction.InputBox("Введите название схемы для базы данных:", "Сохранение в БД", defaultName);

            if (string.IsNullOrWhiteSpace(title)) return;

            if (title != "Пересохранить проект" && _currentProjectId.HasValue)
            {
                _currentProjectId = null;
            }

            try
            {
                _currentProjectId = await _dbService.SaveProjectAsync(_currentProjectId, title, _shapes, canvas.Width, canvas.Height);
                MessageBox.Show($"Проект «{title}» успешно сохранен в БД под ID: {_currentProjectId}", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения в БД:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void btnOpenDb_Click(object sender, EventArgs e)
        {
            using (var catalog = new ProjectsCatalogForm())
            {
                if (catalog.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        int projectId = catalog.SelectedProjectId;

                        var loadedShapes = await _dbService.LoadProjectShapesAsync(projectId);

                        _shapes.Clear();
                        _selectedShape = null;
                        propertyGrid.SelectedObject = null;

                        _shapes.AddRange(loadedShapes);
                        _currentProjectId = projectId;

                        UpdateStatus();
                        canvas.Invalidate();
                        canvas.Update();

                        MessageBox.Show($"Схема «{catalog.SelectedProjectTitle}» успешно загружена на холст!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка восстановления объектов:\n{ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}