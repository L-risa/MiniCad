using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using MiniCad.Services;

namespace MiniCad
{
    public partial class ProjectsCatalogForm : Form
    {
        private readonly DatabaseService _dbService = new DatabaseService();

        public int SelectedProjectId { get; private set; } = -1;
        public string SelectedProjectTitle { get; private set; } = string.Empty;

        private DataGridView dgvProjects;
        private Button btnOpen;
        private Button btnRefresh;
        private Panel bottomPanel;

        public ProjectsCatalogForm()
        {
            InitializeCustomComponents();
            this.Load += ProjectsCatalogForm_Load;
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Каталог схем в базе данных SQL Server";
            this.Size = new Size(750, 450);
            this.StartPosition = FormStartPosition.CenterParent;

            bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 60, BackColor = Color.LightGray };

            btnOpen = new Button
            {
                Text = "Открыть выбранную схему",
                Location = new Point(12, 14),
                Size = new Size(180, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnOpen.Click += BtnOpen_Click;

            btnRefresh = new Button
            {
                Text = "Обновить список",
                Location = new Point(205, 14),
                Size = new Size(130, 32)
            };
            btnRefresh.Click += async (s, e) => await RefreshGridAsync();

            bottomPanel.Controls.Add(btnOpen);
            bottomPanel.Controls.Add(btnRefresh);
            this.Controls.Add(bottomPanel);

            dgvProjects = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                BackgroundColor = Color.White,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            this.Controls.Add(dgvProjects);
        }

        private async void ProjectsCatalogForm_Load(object sender, EventArgs e)
        {
            await RefreshGridAsync();
        }

        private async Task RefreshGridAsync()
        {
            try
            {
                dgvProjects.DataSource = await _dbService.GetProjectsSummaryAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить данные из СУБД:\n{ex.Message}", "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            if (dgvProjects.SelectedRows.Count > 0)
            {
                var selectedRow = dgvProjects.SelectedRows[0]; 
                SelectedProjectId = Convert.ToInt32(selectedRow.Cells["ID"].Value);
                SelectedProjectTitle = selectedRow.Cells["Название схемы"].Value.ToString();

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Пожалуйста, выделите строку с проектом кликом мыши!", "Внимание", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

