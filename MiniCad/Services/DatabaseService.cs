using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using MiniCad.Models;

namespace MiniCad.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString =
    "Server=localhost\\SQLEXPRESS;Database=DiagramDb;Integrated Security=True;TrustServerCertificate=True;";

        public async Task<DataTable> GetProjectsSummaryAsync()
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("SELECT * FROM v_ProjectStatistics ORDER BY [Последнее изменение] DESC", conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    var dt = new DataTable();
                    dt.Load(reader);
                    return dt;
                }
            }
        }

        public async Task<int> SaveProjectAsync(int? projectId, string title, List<Shape> shapes, int width, int height)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmdProj = new SqlCommand("sp_SaveProject", conn, trans))
                        {
                            cmdProj.CommandType = CommandType.StoredProcedure;

                            var idParam = new SqlParameter("@ProjectID", SqlDbType.Int)
                            {
                                Direction = ParameterDirection.InputOutput,
                                Value = (object?)projectId ?? DBNull.Value
                            };

                            cmdProj.Parameters.Add(idParam);
                            cmdProj.Parameters.AddWithValue("@Title", title);
                            cmdProj.Parameters.AddWithValue("@Author", Environment.UserName);
                            cmdProj.Parameters.AddWithValue("@CanvasWidth", width);
                            cmdProj.Parameters.AddWithValue("@CanvasHeight", height);

                            await cmdProj.ExecuteNonQueryAsync();
                            int savedProjectId = (int)idParam.Value;

                            int order = 0;
                            foreach (var s in shapes)
                            {
                                string typeName = s switch
                                {
                                    RectangleShape => "Rectangle",
                                    EllipseShape => "Ellipse",
                                    RhombusShape => "Rhombus",
                                    TextBlockShape => "TextBlock",
                                    LineShape => "Line",
                                    _ => "Rectangle"
                                };

                                string textContent = s is TextBlockShape textShape ? textShape.Text : null;

                                int geomWidth = s is LineShape lineShape ? lineShape.EndX : s.Width;
                                int geomHeight = s is LineShape lineShape2 ? lineShape2.EndY : s.Height;

                                string query = @"
                                    INSERT INTO Shapes (ProjectID, ShapeTypeID, X, Y, Width, Height, StrokeColor, FillColor, StrokeWidth, TextContent, ZOrder)
                                    SELECT @Pid, ShapeTypeID, @X, @Y, @W, @H, @Stroke, @Fill, @Sw, @Text, @Z
                                    FROM ShapeTypes WHERE TypeName = @TypeName;";

                                using (var cmdShape = new SqlCommand(query, conn, trans))
                                {
                                    cmdShape.Parameters.AddWithValue("@Pid", savedProjectId);
                                    cmdShape.Parameters.AddWithValue("@TypeName", typeName);
                                    cmdShape.Parameters.AddWithValue("@X", s.X);
                                    cmdShape.Parameters.AddWithValue("@Y", s.Y);
                                    cmdShape.Parameters.AddWithValue("@W", geomWidth);
                                    cmdShape.Parameters.AddWithValue("@H", geomHeight);
                                    cmdShape.Parameters.AddWithValue("@Stroke", ColorTranslator.ToHtml(s.StrokeColor));
                                    cmdShape.Parameters.AddWithValue("@Fill", ColorTranslator.ToHtml(s.FillColor));
                                    cmdShape.Parameters.AddWithValue("@Sw", s.StrokeWidth);
                                    cmdShape.Parameters.AddWithValue("@Text", (object?)textContent ?? DBNull.Value);
                                    cmdShape.Parameters.AddWithValue("@Z", order++);

                                    await cmdShape.ExecuteNonQueryAsync();
                                }
                            }

                            await trans.CommitAsync();
                            return savedProjectId;
                        }
                    }
                    catch (Exception)
                    {
                        await trans.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<List<Shape>> LoadProjectShapesAsync(int projectId)
        {
            var result = new List<Shape>();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();
                using (var cmd = new SqlCommand("sp_GetProjectShapes", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ProjectID", projectId);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string type = reader.GetString(reader.GetOrdinal("TypeName"));
                            int x = reader.GetInt32(reader.GetOrdinal("X"));
                            int y = reader.GetInt32(reader.GetOrdinal("Y"));
                            int w = reader.GetInt32(reader.GetOrdinal("Width"));
                            int h = reader.GetInt32(reader.GetOrdinal("Height"));
                            string strokeHex = reader.GetString(reader.GetOrdinal("StrokeColor"));
                            string fillHex = reader.GetString(reader.GetOrdinal("FillColor"));
                            float strokeW = (float)reader.GetDouble(reader.GetOrdinal("StrokeWidth"));
                            string textContent = reader.IsDBNull(reader.GetOrdinal("TextContent"))
                                ? null
                                : reader.GetString(reader.GetOrdinal("TextContent"));

                            Shape shape = type switch
                            {
                                "Rectangle" => new RectangleShape(),
                                "Ellipse" => new EllipseShape(),
                                "Rhombus" => new RhombusShape(),
                                "TextBlock" => new TextBlockShape { Text = textContent ?? "Текст" },
                                "Line" => new LineShape { EndX = w, EndY = h },
                                _ => new RectangleShape()
                            };

                            shape.X = x;
                            shape.Y = y;

                            if (!(shape is LineShape))
                            {
                                shape.Width = w;
                                shape.Height = h;
                            }

                            shape.StrokeColor = ColorTranslator.FromHtml(strokeHex);
                            shape.FillColor = ColorTranslator.FromHtml(fillHex);
                            shape.StrokeWidth = strokeW;

                            result.Add(shape);
                        }
                    }
                }
            }
            return result;
        }
    }
}
