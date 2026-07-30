using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class MainForm : Form
    {
        private readonly Label gameRootValue = CreateValueLabel();
        private readonly Label configPathValue = CreateValueLabel();
        private readonly Label loadStatusValue = CreateValueLabel();
        private readonly Label gameStatusValue = CreateValueLabel();
        private readonly Label resultValue = CreateValueLabel();
        private readonly CheckBox enabledCheckBox = new CheckBox { Text = "有効にする", AutoSize = true };
        private readonly NumericUpDown multiplierInput = new NumericUpDown
        {
            Minimum = ConfigDocument.MinimumMultiplier,
            Maximum = ConfigDocument.MaximumMultiplier,
            DecimalPlaces = 1,
            Increment = 0.1m,
            Width = 120
        };
        private readonly Button reloadButton = new Button { Text = "設定を再読み込み", AutoSize = true };
        private readonly Button saveButton = new Button { Text = "設定を保存", AutoSize = true };
        private readonly Button openButton = new Button { Text = "設定フォルダを開く", AutoSize = true };
        private readonly Timer processTimer = new Timer { Interval = 2000 };

        private string? gameRoot;
        private string? configPath;
        private ConfigDocument? document;
        private bool loadIsValid;

        internal MainForm()
        {
            Text = "LOM_MessageSpeed 設定エディタ 0.1.0";
            Font = SystemFonts.MessageBoxFont;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(760, 480);
            Size = new Size(860, 540);
            AutoScaleMode = AutoScaleMode.Dpi;

            Controls.Add(BuildLayout());
            reloadButton.Click += delegate { LoadConfig(); };
            saveButton.Click += delegate { SaveConfig(); };
            openButton.Click += delegate { OpenConfigFolder(); };
            processTimer.Tick += delegate { RefreshProcessStatus(); };
            Shown += delegate { DetectInitialRoot(); };
            processTimer.Start();
            UpdateButtons();
        }

        private Control BuildLayout()
        {
            TableLayoutPanel main = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 2,
                RowCount = 10,
                AutoScroll = true
            };
            main.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            main.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            AddRow(main, 0, "ゲームルート", gameRootValue);
            Button selectButton = new Button { Text = "ゲームフォルダを選択", AutoSize = true };
            selectButton.Click += delegate { SelectGameRoot(); };
            main.Controls.Add(selectButton, 1, 1);
            AddRow(main, 2, "設定ファイル", configPathValue);
            AddRow(main, 3, "読み込み状態", loadStatusValue);
            AddRow(main, 4, "ゲーム状態", gameStatusValue);

            GroupBox settings = new GroupBox { Text = "設定", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            FlowLayoutPanel settingsFlow = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            settingsFlow.Controls.Add(enabledCheckBox);
            settingsFlow.Controls.Add(new Label { Text = "文字送り速度倍率", AutoSize = true, Margin = new Padding(24, 7, 3, 3) });
            settingsFlow.Controls.Add(multiplierInput);
            settings.Controls.Add(settingsFlow);
            main.Controls.Add(settings, 0, 5);
            main.SetColumnSpan(settings, 2);

            FlowLayoutPanel actions = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
            actions.Controls.Add(reloadButton);
            actions.Controls.Add(saveButton);
            Button defaultsButton = new Button { Text = "既定値へ戻す", AutoSize = true };
            defaultsButton.Click += delegate { enabledCheckBox.Checked = true; multiplierInput.Value = ConfigDocument.DefaultMultiplier; SetResult("既定値を画面へ設定しました。保存するまでファイルは変わりません。", false); };
            actions.Controls.Add(defaultsButton);
            actions.Controls.Add(openButton);
            Button closeButton = new Button { Text = "閉じる", AutoSize = true };
            closeButton.Click += delegate { Close(); };
            actions.Controls.Add(closeButton);
            main.Controls.Add(actions, 0, 6);
            main.SetColumnSpan(actions, 2);

            Label notice = new Label
            {
                Text = "設定変更はゲームを完全に終了してから行ってください。保存内容は次回ゲーム起動時に反映されます。",
                AutoSize = true,
                ForeColor = Color.DarkBlue,
                Margin = new Padding(3, 14, 3, 8)
            };
            main.Controls.Add(notice, 0, 7);
            main.SetColumnSpan(notice, 2);
            AddRow(main, 8, "結果", resultValue);
            main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            return main;
        }

        private static void AddRow(TableLayoutPanel panel, int row, string name, Control value)
        {
            Label label = new Label { Text = name, AutoSize = true, Margin = new Padding(3, 6, 14, 6) };
            label.Font = new Font(label.Font, FontStyle.Bold);
            value.Margin = new Padding(3, 6, 3, 6);
            panel.Controls.Add(label, 0, row);
            panel.Controls.Add(value, 1, row);
        }

        private static Label CreateValueLabel()
        {
            return new Label { Text = "未選択", AutoSize = true, MaximumSize = new Size(620, 0) };
        }

        private void DetectInitialRoot()
        {
            var candidates = GameLocator.FindCandidates();
            if (candidates.Count == 1)
            {
                SetGameRoot(candidates[0]);
                return;
            }

            if (candidates.Count > 1)
            {
                MessageBox.Show("複数のゲーム候補が見つかりました。使用するゲームフォルダを選択してください。", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                SetResult("ゲームを自動検出できませんでした。ゲームフォルダを選択してください。", false);
            }

            SelectGameRoot();
        }

        private void SelectGameRoot()
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "Mortal.exeがあるLegendOfMortalゲームフォルダを選択してください。",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string validated;
            string error;
            if (!GameLocator.TryValidateRoot(dialog.SelectedPath, out validated, out error))
            {
                MessageBox.Show(error, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                SetResult(error, true);
                return;
            }

            SetGameRoot(validated);
        }

        private void SetGameRoot(string root)
        {
            gameRoot = root;
            configPath = GameLocator.GetConfigPath(root);
            gameRootValue.Text = root;
            configPathValue.Text = configPath;
            LoadConfig();
        }

        private void LoadConfig()
        {
            document = null;
            loadIsValid = false;
            if (configPath == null)
            {
                loadStatusValue.Text = "ゲームフォルダを選択してください";
                UpdateButtons();
                return;
            }

            try
            {
                ConfigDocument loaded = ConfigDocument.Load(configPath);
                document = loaded;
                loadIsValid = true;
                enabledCheckBox.Checked = loaded.Enabled;
                multiplierInput.Value = loaded.SpeedMultiplier;
                if (loaded.Exists)
                {
                    loadStatusValue.Text = "正常に読み込みました";
                    SetResult("設定を読み込みました。", false);
                }
                else
                {
                    loadStatusValue.Text = "未生成（保存時に確認して新規作成できます）";
                    SetResult("設定ファイルがありません。一度ゲームを起動して生成させるか、保存ボタンから最小設定を作成できます。", false);
                }
            }
            catch (ConfigException ex)
            {
                loadStatusValue.Text = "読み込みエラー（保存禁止）";
                SetResult(ex.Message, true);
            }
            catch (Exception ex)
            {
                loadStatusValue.Text = "読み込みエラー（保存禁止）";
                SetResult("予期しない読み込みエラーです。元ファイルは変更していません: " + ex.Message, true);
            }

            RefreshProcessStatus();
        }

        private void SaveConfig()
        {
            if (!loadIsValid || document == null || gameRoot == null || configPath == null)
            {
                SetResult("正常に読み込めていないため保存できません。", true);
                return;
            }

            GameRunningStatus running = GameProcessGuard.Check(gameRoot);
            if (running.BlocksSave)
            {
                SetResult("ゲームを完全に終了してから再試行してください。" + running.Message, true);
                RefreshProcessStatus();
                return;
            }

            string validated;
            string validationError;
            if (!GameLocator.TryValidateRoot(gameRoot, out validated, out validationError) || !string.Equals(GameLocator.GetConfigPath(validated), configPath, StringComparison.OrdinalIgnoreCase))
            {
                SetResult("ゲームルートと設定パスの再検証に失敗したため保存しません: " + validationError, true);
                return;
            }

            if (!document.Exists)
            {
                DialogResult answer = MessageBox.Show(
                    "lom-messagespeed.cfgがまだありません。\r\n[General] Enabledと[Message] SpeedMultiplierだけを含む最小設定を新規作成しますか？",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    SetResult("新規作成をキャンセルしました。", false);
                    return;
                }
            }

            try
            {
                bool created = !document.Exists;
                document.Save(enabledCheckBox.Checked, multiplierInput.Value);
                LoadConfig();
                SetResult(created ? "最小設定ファイルを新規作成しました。" : "保存しました。.bakへ直前の設定をバックアップしました。", false);
            }
            catch (ConfigChangedException ex)
            {
                loadIsValid = false;
                SetResult(ex.Message, true);
                loadStatusValue.Text = "外部変更を検出（再読み込みが必要）";
                UpdateButtons();
            }
            catch (ConfigException ex)
            {
                SetResult(ex.Message, true);
            }
        }

        private void OpenConfigFolder()
        {
            if (configPath == null)
            {
                return;
            }

            string? directory = Path.GetDirectoryName(configPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                SetResult("設定フォルダが存在しません。", true);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = directory, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetResult("設定フォルダを開けません: " + ex.Message, true);
            }
        }

        private void RefreshProcessStatus()
        {
            GameRunningStatus status = GameProcessGuard.Check(gameRoot);
            gameStatusValue.Text = status.Message;
            gameStatusValue.ForeColor = status.BlocksSave ? Color.DarkRed : Color.DarkGreen;
            UpdateButtons(status);
        }

        private void UpdateButtons()
        {
            UpdateButtons(GameProcessGuard.Check(gameRoot));
        }

        private void UpdateButtons(GameRunningStatus status)
        {
            reloadButton.Enabled = configPath != null;
            openButton.Enabled = configPath != null && Directory.Exists(Path.GetDirectoryName(configPath));
            saveButton.Enabled = loadIsValid && document != null && !status.BlocksSave;
            enabledCheckBox.Enabled = loadIsValid;
            multiplierInput.Enabled = loadIsValid;
        }

        private void SetResult(string message, bool error)
        {
            resultValue.Text = message;
            resultValue.ForeColor = error ? Color.DarkRed : Color.DarkGreen;
        }
    }
}
