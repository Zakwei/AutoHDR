using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace AutoHDR
{
    public partial class SettingsForm : Form
    {
        private readonly string _appDir;
        private readonly List<string> _games;

        public SettingsForm(string appDir)
        {
            _appDir = appDir;
            _games = new List<string>();
            InitializeComponents();
            LoadGames();
        }

        private void InitializeComponents()
        {
            Text = Locale.Get("GameListTitle");
            Size = new System.Drawing.Size(520, 460);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;

            var listBox = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 280,
                SelectionMode = SelectionMode.MultiExtended
            };
            listBox.Name = "listBox";
            Controls.Add(listBox);

            var panel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 120,
                Padding = new Padding(10)
            };
            Controls.Add(panel);

            var lbl = new Label
            {
                Text = Locale.Get("ProcessNameNoExe"),
                Top = 10,
                Left = 10,
                Width = 160,
                Height = 20
            };
            panel.Controls.Add(lbl);

            var textBox = new TextBox
            {
                Name = "textBox",
                Top = 10,
                Left = 180,
                Width = 200,
                Height = 20
            };
            panel.Controls.Add(textBox);

            int top = 45;
            int left = 10;
            int btnWidth = 110;

            var btnAddFile = new Button
            {
                Text = Locale.Get("AddFromFile"),
                Top = top,
                Left = left,
                Width = btnWidth
            };
            btnAddFile.Click += (s, e) => AddFromFile();
            panel.Controls.Add(btnAddFile);

            left += btnWidth + 10;
            var btnAdd = new Button
            {
                Text = Locale.Get("Add"),
                Top = top,
                Left = left,
                Width = 80
            };
            btnAdd.Click += (s, e) => AddFromTextBox(textBox);
            panel.Controls.Add(btnAdd);

            left += 90;
            var btnDelete = new Button
            {
                Text = Locale.Get("Delete"),
                Top = top,
                Left = left,
                Width = 80
            };
            btnDelete.Click += (s, e) => DeleteSelected(listBox);
            panel.Controls.Add(btnDelete);

            top += 35;
            left = 10;
            var btnSave = new Button
            {
                Text = Locale.Get("Save"),
                Top = top,
                Left = left,
                Width = 100,
                DialogResult = DialogResult.OK
            };
            btnSave.Click += (s, e) => SaveAndClose();
            panel.Controls.Add(btnSave);

            left += 110;
            var btnCancel = new Button
            {
                Text = Locale.Get("Cancel"),
                Top = top,
                Left = left,
                Width = 100,
                DialogResult = DialogResult.Cancel
            };
            panel.Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void LoadGames()
        {
            _games.Clear();
            try
            {
                string path = Path.Combine(_appDir, "games.txt");
                if (File.Exists(path))
                {
                    foreach (var line in File.ReadAllLines(path))
                    {
                        string name = GameDetector.SanitizeGameName(line);
                        if (!string.IsNullOrEmpty(name) && !_games.Contains(name, StringComparer.OrdinalIgnoreCase))
                            _games.Add(name);
                    }
                }
            }
            catch { }
            RefreshList();
        }

        private static string Sanitize(string line)
        {
            return GameDetector.SanitizeGameName(line);
        }

        private void RefreshList()
        {
            var listBox = (ListBox)Controls.Find("listBox", false)[0];
            listBox.Items.Clear();
            foreach (var g in _games.OrderBy(g => g, StringComparer.OrdinalIgnoreCase))
                listBox.Items.Add(g);
        }

        private void AddFromTextBox(TextBox textBox)
        {
            string name = Sanitize(textBox.Text);
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show(Locale.Get("InvalidProcessName"), "AutoHDR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!_games.Contains(name, StringComparer.OrdinalIgnoreCase))
            {
                _games.Add(name);
                RefreshList();
                textBox.Clear();
            }
            else
            {
                MessageBox.Show(Locale.Get("GameAlreadyOnList"), "AutoHDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void AddFromFile()
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = Locale.Get("GameFileFilter");
                dlg.Title = Locale.Get("SelectGameExe");
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    string name = GameDetector.SanitizeGameName(dlg.FileName);
                    if (string.IsNullOrEmpty(name)) return;
                    if (!_games.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        _games.Add(name);
                        RefreshList();
                    }
                    else
                    {
                        MessageBox.Show(Locale.Get("GameAlreadyOnList"), "AutoHDR", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void DeleteSelected(ListBox listBox)
        {
            var selected = new List<object>();
            foreach (var item in listBox.SelectedItems)
                selected.Add(item);

            foreach (var item in selected)
            {
                _games.Remove(item.ToString());
            }
            RefreshList();
        }

        private void SaveAndClose()
        {
            try
            {
                string path = Path.Combine(_appDir, "games.txt");
                var lines = new List<string>
                {
                    Locale.Get("GamesFileHeader1"),
                    Locale.Get("GamesFileHeader2"),
                    Locale.Get("GamesFileHeader3")
                };
                lines.AddRange(_games.OrderBy(g => g, StringComparer.OrdinalIgnoreCase));
                File.WriteAllLines(path, lines);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(Locale.Get("SaveError") + ": " + ex.Message, "AutoHDR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
