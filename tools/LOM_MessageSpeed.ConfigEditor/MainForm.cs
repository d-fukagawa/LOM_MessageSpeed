using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class MainForm : Form
    {
        private readonly TabControl tabs = new TabControl { Dock = DockStyle.Fill };
        private readonly TabPage toolTab = new TabPage("ツール設定");
        private readonly TabPage bepinexTab = new TabPage("BepInEx導入サポート");
        private readonly TabPage configTab = new TabPage("コンフィグ編集");
        private readonly RadioButton driveMode = new RadioButton { Text = "ドライブから選択", AutoSize = true };
        private readonly RadioButton manualMode = new RadioButton { Text = "パスを手動指定", AutoSize = true };
        private readonly ComboBox driveInput = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
        private readonly Label expectedPathValue = CreateValueLabel();
        private readonly TextBox manualPathInput = new TextBox { Width = 480 };
        private readonly Button browseButton = new Button { Text = "参照", AutoSize = true };
        private readonly Button validateButton = new Button { Text = "このフォルダを使用", AutoSize = true };
        private readonly Label gameRootValue = CreateValueLabel();
        private readonly Label bepinexValue = CreateValueLabel();
        private readonly Label supportGameValue = CreateValueLabel();
        private readonly Label supportVerifiedValue = CreateValueLabel();
        private readonly Label supportStateValue = CreateValueLabel();
        private readonly Label supportMessageValue = CreateValueLabel();
        private readonly Label supportNextValue = CreateValueLabel();
        private readonly Label supportDetailsValue = CreateValueLabel();
        private readonly Button toggleBepInExDetailsButton = new Button { Text = "問題報告用の詳細情報", AutoSize = true };
        private readonly Button openReinstallGuideButton = new Button { Text = "導入・入れ直し手順を開く", AutoSize = true };
        private readonly Button recheckBepInExButton = new Button { Text = "状態を再確認", AutoSize = true };
        private readonly Button initializeGameButton = new Button { Text = "初回初期化のためゲームを起動", AutoSize = true };
        private readonly Button goToPluginButton = new Button { Text = "プラグインの操作へ進む", AutoSize = true };
        private readonly Label targetVersionValue = CreateValueLabel();
        private readonly Label installedVersionValue = CreateValueLabel();
        private readonly Label pluginPathValue = CreateValueLabel();
        private readonly Button installButton = new Button { Text = "プラグインをインストール", AutoSize = true };
        private readonly Button openPluginButton = new Button { Text = "プラグインフォルダを開く", AutoSize = true };
        private readonly Label configPathValue = CreateValueLabel();
        private readonly Label loadStatusValue = CreateValueLabel();
        private readonly CheckBox enabledCheckBox = new CheckBox { Text = "変更を有効にする", AutoSize = true };
        private readonly NumericUpDown messageInput = CreateMultiplierInput();
        private readonly Button saveButton = new Button { Text = "設定を保存", AutoSize = true };
        private readonly Button defaultsButton = new Button { Text = "既定値へ戻す", AutoSize = true };
        private readonly Button openConfigButton = new Button { Text = "設定フォルダを開く", AutoSize = true };
        private readonly Button launchGameButton = new Button { Text = "ゲームを起動", AutoSize = true };
        private readonly Label gameStatusValue = CreateValueLabel();
        private readonly Label resultValue = CreateValueLabel();
        private readonly Timer processTimer = new Timer { Interval = 2000 };
        private readonly PluginManifest manifest = PluginManifest.Current;
        private readonly PluginInstaller installer = new PluginInstaller();
        private readonly string settingsPath;

        private ToolSettings settings;
        private string? gameRoot;
        private string? configPath;
        private ConfigDocument? document;
        private PluginInspection? pluginInspection;
        private BepInExInspection bepinexInspection = BepInExInspector.Inspect(null);
        private bool loadIsValid;
        private bool suppressDirty;
        private bool isDirty;

        internal MainForm(string? settingsPathOverride = null)
        {
            Text = "LOM_MessageSpeed 設定ツール 0.3.0";
            Font = SystemFonts.MessageBoxFont;
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(820, 650);
            Size = new Size(940, 760);
            AutoScaleMode = AutoScaleMode.Dpi;

            settingsPath = settingsPathOverride ?? ToolSettings.DefaultPath;
            settings = ToolSettings.Load(settingsPath, out string? settingsWarning);
            BuildLayout();
            targetVersionValue.Text = PluginManifest.DisplayName + " " + manifest.VersionDisplay + "（最新版・文字送り専用）";
            supportVerifiedValue.Text = BepInExSupportInfo.VerifiedDisplay;
            supportGameValue.Text = "未確認";
            supportStateValue.Text = bepinexInspection.Message;
            supportNextValue.Text = "先にツール設定でゲームフォルダを確認してください。";
            WireEvents();
            PopulateSettings();
            InitializeGameRoot(settingsWarning);
            processTimer.Start();
            UpdateButtons();
        }

        private void BuildLayout()
        {
            configTab.Enabled = false;
            toolTab.Controls.Add(BuildToolTab());
            bepinexTab.Controls.Add(BuildBepInExSupportTab());
            configTab.Controls.Add(BuildConfigTab());
            tabs.TabPages.Add(toolTab);
            tabs.TabPages.Add(bepinexTab);
            tabs.TabPages.Add(configTab);

            TableLayoutPanel root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 3
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(tabs, 0, 0);
            root.SetColumnSpan(tabs, 2);
            AddRow(root, 1, "ゲーム状態", gameStatusValue);
            AddRow(root, 2, "結果", resultValue);
            Controls.Add(root);
        }

        private Control BuildToolTab()
        {
            TableLayoutPanel panel = CreateTwoColumnPanel();
            GroupBox location = new GroupBox { Text = "ゲームフォルダ", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            TableLayoutPanel locationGrid = CreateTwoColumnPanel();
            locationGrid.Controls.Add(driveMode, 0, 0);
            locationGrid.SetColumnSpan(driveMode, 2);
            AddRow(locationGrid, 1, "ドライブ", driveInput);
            AddRow(locationGrid, 2, "想定パス", expectedPathValue);
            locationGrid.Controls.Add(manualMode, 0, 3);
            locationGrid.SetColumnSpan(manualMode, 2);
            FlowLayoutPanel manualFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            manualFlow.Controls.Add(manualPathInput);
            manualFlow.Controls.Add(browseButton);
            locationGrid.Controls.Add(manualFlow, 1, 4);
            locationGrid.Controls.Add(validateButton, 1, 5);
            AddRow(locationGrid, 6, "使用中のゲームルート", gameRootValue);
            AddRow(locationGrid, 7, "BepInEx状態", bepinexValue);
            location.Controls.Add(locationGrid);
            panel.Controls.Add(location, 0, 0);
            panel.SetColumnSpan(location, 2);

            GroupBox plugin = new GroupBox { Text = "プラグイン", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            TableLayoutPanel pluginGrid = CreateTwoColumnPanel();
            AddRow(pluginGrid, 0, "導入対象", targetVersionValue);
            AddRow(pluginGrid, 1, "インストール済み", installedVersionValue);
            AddRow(pluginGrid, 2, "インストール先", pluginPathValue);
            FlowLayoutPanel pluginActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            pluginActions.Controls.Add(installButton);
            pluginActions.Controls.Add(openPluginButton);
            pluginGrid.Controls.Add(pluginActions, 1, 3);
            plugin.Controls.Add(pluginGrid);
            panel.Controls.Add(plugin, 0, 1);
            panel.SetColumnSpan(plugin, 2);
            return panel;
        }

        private Control BuildBepInExSupportTab()
        {
            TableLayoutPanel panel = CreateTwoColumnPanel();

            GroupBox current = new GroupBox { Text = "BepInEx", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            TableLayoutPanel currentGrid = CreateTwoColumnPanel();
            AddRow(currentGrid, 0, "ゲーム", supportGameValue);
            AddRow(currentGrid, 1, "このツールで動作確認したもの", supportVerifiedValue);
            AddRow(currentGrid, 2, "現在入っているもの", supportStateValue);
            supportVerifiedValue.AccessibleName = "このツールで動作確認したBepInEx";
            supportStateValue.AccessibleName = "現在ゲームフォルダに入っているBepInEx";
            supportMessageValue.MaximumSize = new Size(760, 0);
            supportMessageValue.AccessibleName = "BepInExをそのまま使用できる条件";
            currentGrid.Controls.Add(supportMessageValue, 0, 3);
            currentGrid.SetColumnSpan(supportMessageValue, 2);
            supportNextValue.MaximumSize = new Size(760, 0);
            supportNextValue.AccessibleName = "BepInExに問題がある場合の次の操作";
            currentGrid.Controls.Add(supportNextValue, 0, 4);
            currentGrid.SetColumnSpan(supportNextValue, 2);
            FlowLayoutPanel primaryActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            primaryActions.Controls.Add(openReinstallGuideButton);
            primaryActions.Controls.Add(recheckBepInExButton);
            primaryActions.Controls.Add(initializeGameButton);
            primaryActions.Controls.Add(goToPluginButton);
            currentGrid.Controls.Add(primaryActions, 0, 5);
            currentGrid.SetColumnSpan(primaryActions, 2);
            supportDetailsValue.Visible = false;
            supportDetailsValue.ForeColor = Color.DimGray;
            supportDetailsValue.AccessibleName = "BepInExの問題報告用の詳細情報";
            currentGrid.Controls.Add(toggleBepInExDetailsButton, 0, 6);
            currentGrid.SetColumnSpan(toggleBepInExDetailsButton, 2);
            currentGrid.Controls.Add(supportDetailsValue, 0, 7);
            currentGrid.SetColumnSpan(supportDetailsValue, 2);
            current.Controls.Add(currentGrid);
            panel.Controls.Add(current, 0, 0);
            panel.SetColumnSpan(current, 2);

            foreach (Button button in new[] { toggleBepInExDetailsButton, openReinstallGuideButton, recheckBepInExButton, initializeGameButton, goToPluginButton })
            {
                button.AccessibleName = button.Text;
            }

            return panel;
        }

        private Control BuildConfigTab()
        {
            TableLayoutPanel panel = CreateTwoColumnPanel();
            GroupBox target = new GroupBox { Text = "対象", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            TableLayoutPanel targetGrid = CreateTwoColumnPanel();
            AddRow(targetGrid, 0, "設定ファイル", configPathValue);
            AddRow(targetGrid, 1, "読み込み状態", loadStatusValue);
            target.Controls.Add(targetGrid);
            panel.Controls.Add(target, 0, 0);
            panel.SetColumnSpan(target, 2);

            GroupBox speedSettings = new GroupBox { Text = "速度設定", Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(12) };
            TableLayoutPanel speedGrid = CreateTwoColumnPanel();
            FlowLayoutPanel messageFlow = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false };
            messageFlow.Controls.Add(messageInput);
            enabledCheckBox.Margin = new Padding(12, 7, 3, 3);
            messageFlow.Controls.Add(enabledCheckBox);
            AddRow(speedGrid, 0, "文字送り速度の倍率", messageFlow);

            speedGrid.Controls.Add(new Label { Text = "操作", AutoSize = true, Margin = new Padding(3, 7, 14, 6), Font = new Font(Font, FontStyle.Bold) }, 0, 1);
            speedGrid.Controls.Add(defaultsButton, 1, 1);

            Label help = new Label
            {
                Text = "1.0 = ゲーム本来の速度　 2.0 = 所要時間を半分にして2倍速　 0.5 = 所要時間を2倍にして半速",
                AutoSize = true,
                ForeColor = Color.DarkBlue,
                Margin = new Padding(3, 12, 3, 6)
            };
            speedGrid.Controls.Add(help, 0, 2);
            speedGrid.SetColumnSpan(help, 2);
            speedSettings.Controls.Add(speedGrid);
            panel.Controls.Add(speedSettings, 0, 1);
            panel.SetColumnSpan(speedSettings, 2);

            TableLayoutPanel actionRow = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Top, ColumnCount = 2 };
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actionRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            FlowLayoutPanel configActions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
            configActions.Controls.Add(saveButton);
            configActions.Controls.Add(openConfigButton);
            actionRow.Controls.Add(configActions, 0, 0);
            launchGameButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            actionRow.Controls.Add(launchGameButton, 1, 0);
            panel.Controls.Add(actionRow, 0, 2);
            panel.SetColumnSpan(actionRow, 2);
            return panel;
        }

        private void WireEvents()
        {
            driveMode.CheckedChanged += delegate { UpdateLocationMode(); };
            manualMode.CheckedChanged += delegate { UpdateLocationMode(); };
            driveInput.SelectedIndexChanged += delegate { UpdateExpectedPath(); };
            browseButton.Click += delegate { BrowseGameRoot(); };
            validateButton.Click += delegate { ValidateSelectedRoot(); };
            manualPathInput.KeyDown += delegate (object? sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    ValidateSelectedRoot();
                }
            };
            installButton.Click += delegate { InstallPlugin(); };
            toggleBepInExDetailsButton.Click += delegate
            {
                supportDetailsValue.Visible = !supportDetailsValue.Visible;
                toggleBepInExDetailsButton.Text = supportDetailsValue.Visible ? "問題報告用の詳細情報を閉じる" : "問題報告用の詳細情報";
            };
            openReinstallGuideButton.Click += delegate { OpenReinstallGuide(); };
            recheckBepInExButton.Click += delegate { RecheckBepInEx(); };
            initializeGameButton.Click += delegate { LaunchGame(); };
            goToPluginButton.Click += delegate { tabs.SelectedTab = toolTab; installButton.Focus(); };
            openPluginButton.Click += delegate { OpenFolder(Path.GetDirectoryName(pluginPathValue.Text), "プラグインフォルダ"); };
            saveButton.Click += delegate { SaveConfig(); };
            defaultsButton.Click += delegate { ApplyDefaults(); };
            openConfigButton.Click += delegate { OpenFolder(configPath == null ? null : Path.GetDirectoryName(configPath), "設定フォルダ"); };
            launchGameButton.Click += delegate { LaunchGame(); };
            enabledCheckBox.CheckedChanged += delegate { MarkDirty(); };
            messageInput.ValueChanged += delegate { MarkDirty(); };
            tabs.SelectedIndexChanged += delegate { SaveToolSettingsSilently(); };
            processTimer.Tick += delegate { RefreshStatus(); };
            FormClosing += OnFormClosing;
        }

        private void PopulateSettings()
        {
            foreach (string drive in GameLocationOptions.GetReadyFixedDrives())
            {
                driveInput.Items.Add(drive);
            }

            int driveIndex = driveInput.Items.IndexOf(settings.LastDrive);
            if (driveIndex < 0 && driveInput.Items.Count > 0)
            {
                driveIndex = 0;
            }

            if (driveIndex >= 0)
            {
                driveInput.SelectedIndex = driveIndex;
            }

            manualPathInput.Text = settings.LastValidatedManualPath;
            driveMode.Checked = settings.LocationMode == GameLocationMode.Drive;
            manualMode.Checked = settings.LocationMode == GameLocationMode.Manual;
            tabs.SelectedIndex = 0;
            installButton.Visible = manifest.HasApprovedPayload;
            UpdateLocationMode();
            UpdateExpectedPath();
        }

        private void InitializeGameRoot(string? settingsWarning)
        {
            List<string> candidates = new List<string>();
            if (settings.LocationMode == GameLocationMode.Drive && driveInput.SelectedItem is string drive)
            {
                candidates.AddRange(GameLocationOptions.GetDriveCandidates(drive));
            }
            else if (!string.IsNullOrWhiteSpace(settings.LastValidatedManualPath))
            {
                candidates.Add(settings.LastValidatedManualPath);
            }

            candidates.AddRange(GameLocator.FindCandidates());
            StartupGameSelection selection = StartupGameLocator.Select(
                settings.LastValidatedGameRoot,
                candidates);
            if (selection.State == StartupGameSelectionState.Found && selection.Root != null)
            {
                settings.LastValidatedGameRoot = selection.Root;
                if (settings.LocationMode == GameLocationMode.Manual)
                {
                    settings.LastValidatedManualPath = selection.Root;
                    manualPathInput.Text = selection.Root;
                }

                SetGameRoot(selection.Root);
                SaveToolSettingsSilently();
                if (document != null && document.Exists)
                {
                    string prefix = selection.UsedSavedRoot
                        ? "保存済みのゲームフォルダを確認し、設定を読み込みました。"
                        : "ゲームフォルダを自動検出し、設定を読み込みました。";
                    SetResult(settingsWarning == null ? prefix : settingsWarning + " " + prefix, settingsWarning != null);
                }

                return;
            }

            configTab.Enabled = false;
            if (selection.State == StartupGameSelectionState.Multiple)
            {
                SetResult(
                    "設定を読み込めませんでした。複数のゲームフォルダ候補があります。使用するフォルダを選択してください。",
                    true);
            }
            else
            {
                string message = "設定を読み込めませんでした。ゲームフォルダを確認してください。詳細: " + selection.Detail;
                SetResult(settingsWarning == null ? message : settingsWarning + " " + message, true);
            }
        }

        private void UpdateLocationMode()
        {
            bool drive = driveMode.Checked;
            driveInput.Enabled = drive;
            manualPathInput.Enabled = !drive;
            browseButton.Enabled = !drive;
            settings.LocationMode = drive ? GameLocationMode.Drive : GameLocationMode.Manual;
        }

        private void UpdateExpectedPath()
        {
            if (driveInput.SelectedItem is string drive)
            {
                expectedPathValue.Text = GameLocationOptions.GetDriveTemplate(drive);
                settings.LastDrive = drive;
            }
            else
            {
                expectedPathValue.Text = "利用可能な固定ドライブがありません";
            }
        }

        private void BrowseGameRoot()
        {
            using FolderBrowserDialog dialog = new FolderBrowserDialog
            {
                Description = "Mortal.exeがあるLegendOfMortalゲームフォルダを選択してください。",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = false,
                SelectedPath = Directory.Exists(manualPathInput.Text) ? manualPathInput.Text : string.Empty
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                manualMode.Checked = true;
                manualPathInput.Text = dialog.SelectedPath;
                ValidateSelectedRoot();
            }
        }

        private void ValidateSelectedRoot()
        {
            if (isDirty && !ConfirmDiscard())
            {
                return;
            }

            List<string> candidates = new List<string>();
            if (driveMode.Checked && driveInput.SelectedItem is string drive)
            {
                candidates.AddRange(GameLocationOptions.GetDriveCandidates(drive));
            }
            else
            {
                candidates.Add(manualPathInput.Text.Trim());
            }

            string lastError = "候補がありません。";
            foreach (string candidate in candidates)
            {
                if (GameLocator.TryValidateRoot(candidate, out string validated, out string error))
                {
                    SetGameRoot(validated);
                    settings.LastValidatedGameRoot = validated;
                    if (manualMode.Checked)
                    {
                        manualPathInput.Text = validated;
                        settings.LastValidatedManualPath = validated;
                    }
                    SaveToolSettingsSilently();
                    if (!bepinexInspection.AllowsPluginUse)
                    {
                        tabs.SelectedTab = bepinexTab;
                        SetResult("ゲームフォルダを確認しました。BepInEx導入サポートを確認してください。", false);
                    }
                    return;
                }

                lastError = candidate + ": " + error;
            }

            SetResult("ゲームフォルダを使用できません: " + lastError, true);
        }

        private void SetGameRoot(string root)
        {
            gameRoot = root;
            configPath = GameLocator.GetConfigPath(root);
            gameRootValue.Text = root;
            supportGameValue.Text = "確認済み: " + root;
            configPathValue.Text = configPath;
            RefreshBepInExInspection();
            tabs.SelectedIndex = Math.Min(settings.LastSelectedTab, tabs.TabPages.Count - 1);
            if (!IsBepInExWritable(bepinexInspection))
            {
                tabs.SelectedTab = bepinexTab;
            }
        }

        private void RefreshBepInExInspection()
        {
            bepinexInspection = BepInExInspector.Inspect(gameRoot);
            Color statusColor = GetBepInExStatusColor(bepinexInspection.Tone);
            bepinexValue.Text = bepinexInspection.Title;
            bepinexValue.ForeColor = statusColor;
            supportStateValue.Text = bepinexInspection.CurrentDisplay;
            supportStateValue.ForeColor = statusColor;
            supportMessageValue.Text = bepinexInspection.Message;
            supportDetailsValue.Text = bepinexInspection.Details;
            supportVerifiedValue.Text = BepInExSupportInfo.VerifiedDisplay;

            bool available = bepinexInspection.AllowsPluginUse && !bepinexInspection.HasReparsePoint;
            configTab.Enabled = available;
            supportNextValue.Text = bepinexInspection.NextAction;

            RefreshPluginInspection();
            if (available)
            {
                LoadConfig();
            }
            else
            {
                document = null;
                loadIsValid = false;
                isDirty = false;
                loadStatusValue.Text = "BepInExの確認後に利用できます";
                installedVersionValue.Text = "BepInExの確認後に検査します";
                UpdateButtons();
            }
        }

        private void RecheckBepInEx()
        {
            if (gameRoot == null)
            {
                SetResult("先にゲームフォルダを選択してください。", true);
                return;
            }

            RefreshBepInExInspection();
            SetResult(bepinexInspection.Title + "。" + bepinexInspection.NextAction, bepinexInspection.Tone);
        }

        private void LoadConfig()
        {
            document = null;
            loadIsValid = false;
            isDirty = false;
            if (configPath == null)
            {
                loadStatusValue.Text = "ゲームフォルダを検証してください";
                UpdateButtons();
                return;
            }

            try
            {
                ConfigDocument loaded = ConfigDocument.Load(configPath);
                document = loaded;
                loadIsValid = true;
                suppressDirty = true;
                enabledCheckBox.Checked = loaded.Enabled;
                messageInput.Value = loaded.SpeedMultiplier;
                suppressDirty = false;
                loadStatusValue.Text = loaded.Exists ? "正常に読み込みました" : "未生成（保存時に確認して新規作成できます）";
                SetResult(loaded.Exists ? "設定を読み込みました。" : "設定ファイルがありません。保存時に内蔵テンプレートから最小設定を作成できます。", false);
            }
            catch (ConfigException ex)
            {
                suppressDirty = false;
                loadStatusValue.Text = "読み込みエラー（保存禁止）";
                SetResult(ex.Message, true);
            }
            catch (Exception ex)
            {
                suppressDirty = false;
                loadStatusValue.Text = "読み込みエラー（保存禁止）";
                SetResult("予期しない読み込みエラーです。元ファイルは変更していません: " + ex.Message, true);
            }

            RefreshStatus();
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
                return;
            }

            if (!GameLocator.TryValidateRoot(gameRoot, out string validated, out string validationError) ||
                !IsBepInExWritable(BepInExInspector.Inspect(validated)) ||
                !string.Equals(GameLocator.GetConfigPath(validated), configPath, StringComparison.OrdinalIgnoreCase))
            {
                SetResult("ゲームルートと設定パスの再検証に失敗したため保存しません: " + validationError, true);
                return;
            }

            if (!document.Exists)
            {
                DialogResult answer = MessageBox.Show(
                    "[General]と[Message]を含む最小設定を新規作成しますか？",
                    Text,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            try
            {
                bool created = !document.Exists;
                ConfigSaveResult saveResult = ConfigSaveCoordinator.Save(
                    document,
                    configPath,
                    enabledCheckBox.Checked,
                    messageInput.Value,
                    CanRebaseSave);
                LoadConfig();
                if (saveResult.Rebased)
                {
                    SetResult(
                        "ゲーム終了後の設定更新を取り込み、画面の文字送り設定を保存しました。.bakも更新しました。",
                        false);
                }
                else
                {
                    SetResult(created ? "内蔵テンプレートから最小設定ファイルを新規作成しました。" : "保存しました。.bakへ直前の設定をバックアップしました。", false);
                }
            }
            catch (ConfigChangedException ex)
            {
                loadStatusValue.Text = "保存保留（ゲーム状態を確認してください）";
                SetResult(ex.Message, true);
                UpdateButtons();
            }
            catch (ConfigException ex)
            {
                SetResult(ex.Message, true);
            }
        }

        private bool CanRebaseSave()
        {
            if (gameRoot == null || configPath == null ||
                !GameLocator.TryValidateRoot(gameRoot, out string validated, out string error) ||
                !IsBepInExWritable(BepInExInspector.Inspect(validated)) ||
                !string.Equals(GameLocator.GetConfigPath(validated), configPath, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return !GameProcessGuard.Check(validated).BlocksSave;
        }

        private void ApplyDefaults()
        {
            enabledCheckBox.Checked = true;
            messageInput.Value = ConfigSchema.DefaultMessageMultiplier;
            SetResult("既定値を画面へ設定しました。保存するまでファイルは変わりません。", false);
        }

        private void LaunchGame()
        {
            if (isDirty)
            {
                SetResult("保存していない変更があります。設定を保存してからゲームを起動してください。", true);
                return;
            }

            string validated = string.Empty;
            string error = "ゲームフォルダが選択されていません。";
            if (gameRoot == null || !GameLocator.TryValidateRoot(gameRoot, out validated, out error))
            {
                SetResult("ゲームルートの再検証に失敗したため起動しません: " + error, true);
                return;
            }

            GameRunningStatus running = GameProcessGuard.Check(validated);
            if (running.BlocksSave)
            {
                SetResult("ゲームは既に起動中、または起動状態を確認できません。" + running.Message, true);
                RefreshStatus();
                return;
            }

            string executable = Path.Combine(validated, "Mortal.exe");
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = executable,
                    WorkingDirectory = validated,
                    UseShellExecute = true
                });
                SetResult(
                    bepinexInspection.State == BepInExState.InstalledNotInitialized
                        ? "BepInEx初期化のためゲームを起動しました。タイトル画面まで進み、終了後に状態を再確認してください。"
                        : "ゲームを起動しました。",
                    false);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                SetResult("ゲームを起動できません: " + ex.Message, true);
            }
        }

        private void RefreshPluginInspection()
        {
            if (gameRoot == null)
            {
                pluginInspection = null;
                return;
            }

            pluginInspection = PluginInspector.Inspect(gameRoot, manifest);
            targetVersionValue.Text = PluginManifest.DisplayName + " " + manifest.VersionDisplay + "（最新版・文字送り専用）";
            pluginPathValue.Text = GameLocator.GetPluginPath(gameRoot);
            installedVersionValue.Text = GetInspectionDisplay(pluginInspection);
        }

        private void InstallPlugin()
        {
            if (gameRoot == null || pluginInspection == null)
            {
                return;
            }

            string current = pluginInspection.Version?.ToString() ?? "未導入";
            string currentHash = pluginInspection.Sha256 ?? "なし";
            string operation = pluginInspection.AllowsUpdate ? "更新" : "新規インストール";
            DialogResult answer = MessageBox.Show(
                "操作: " + operation +
                "\r\n対象ゲームルート: " + gameRoot +
                "\r\nインストール先: " + GameLocator.GetPluginPath(gameRoot) +
                "\r\n現在版: " + current +
                "\r\n現在SHA-256: " + currentHash +
                "\r\n導入版: " + manifest.VersionDisplay +
                "\r\n導入SHA-256: " + manifest.ExpectedSha256 +
                "\r\n\r\n設定cfgは削除・初期化しません。続行しますか？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            PluginInstallResult result = installer.Install(gameRoot, manifest, pluginInspection);
            SetResult(result.Message, !result.Success);
            RefreshPluginInspection();
            UpdateButtons();
        }

        private void RefreshStatus()
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
            bool bepinexAvailable = bepinexInspection.AllowsPluginUse && !bepinexInspection.HasReparsePoint;
            saveButton.Enabled = bepinexAvailable && loadIsValid && document != null && !status.BlocksSave;
            defaultsButton.Enabled = loadIsValid;
            openConfigButton.Enabled = configPath != null && Directory.Exists(Path.GetDirectoryName(configPath));
            launchGameButton.Enabled = gameRoot != null && !status.BlocksSave;
            initializeGameButton.Enabled = gameRoot != null && !status.BlocksSave &&
                bepinexInspection.State == BepInExState.InstalledNotInitialized;
            openReinstallGuideButton.Enabled = !string.IsNullOrWhiteSpace(BepInExSupportInfo.ReinstallGuideUrl);
            recheckBepInExButton.Enabled = gameRoot != null;
            goToPluginButton.Enabled = bepinexAvailable;
            enabledCheckBox.Enabled = bepinexAvailable && loadIsValid;
            messageInput.Enabled = bepinexAvailable && loadIsValid;
            bool canInstall = pluginInspection != null &&
                (pluginInspection.AllowsInstall || pluginInspection.AllowsUpdate);
            installButton.Visible = manifest.HasEmbeddedPayload;
            installButton.Enabled = bepinexAvailable && gameRoot != null && manifest.HasEmbeddedPayload &&
                !status.BlocksSave && canInstall;
            if (pluginInspection == null || pluginInspection.State == PluginState.NotInstalled)
            {
                installButton.Text = "プラグインをインストール";
            }
            else if (pluginInspection.State == PluginState.KnownOlder)
            {
                installButton.Text = "プラグインを更新";
            }
            else if (pluginInspection.State == PluginState.Approved)
            {
                installButton.Text = "インストール済み";
            }
            else
            {
                installButton.Text = "自動導入できません";
            }
            openPluginButton.Enabled = gameRoot != null && Directory.Exists(Path.GetDirectoryName(GameLocator.GetPluginPath(gameRoot)));
        }

        private static string GetInspectionDisplay(PluginInspection inspection)
        {
            string version = inspection.Version?.ToString() ?? "不明";
            switch (inspection.State)
            {
                case PluginState.NotInstalled: return "未導入";
                case PluginState.Approved: return version + "（確認済み）";
                case PluginState.KnownOlder: return version + "（更新可能な確認済み旧版）";
                case PluginState.SameVersionDifferentHash: return version + "（確認済みではない同版）";
                case PluginState.NewerVersion: return version + "（導入対象より新しい版）";
                case PluginState.DuplicatePlacement: return "重複配置（手動整理が必要）";
                case PluginState.CorruptOrUnreadable: return "破損または読み取り不能";
                case PluginState.PayloadUnavailable: return version + "（導入データなし）";
                default: return version + "（確認済みではない版）";
            }
        }

        private void MarkDirty()
        {
            if (!suppressDirty && loadIsValid)
            {
                isDirty = true;
                loadStatusValue.Text = "変更あり（未保存）";
            }
        }

        private bool ConfirmDiscard()
        {
            return MessageBox.Show(
                "保存していない設定変更を破棄しますか？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (isDirty && !ConfirmDiscard())
            {
                e.Cancel = true;
                return;
            }

            SaveToolSettingsSilently();
        }

        private void SaveToolSettingsSilently()
        {
            settings.LocationMode = driveMode.Checked ? GameLocationMode.Drive : GameLocationMode.Manual;
            settings.LastSelectedTab = tabs.SelectedIndex;
            try
            {
                settings.Save(settingsPath);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                SetResult("ツール設定を保存できません: " + ex.Message, true);
            }
        }

        private void OpenFolder(string? path, string name)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                SetResult(name + "が存在しません。", true);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                SetResult(name + "を開けません: " + ex.Message, true);
            }
        }

        private void OpenReinstallGuide()
        {
            string? url = BepInExSupportInfo.ReinstallGuideUrl;
            if (string.IsNullOrWhiteSpace(url))
            {
                SetResult("導入・入れ直し手順は公開準備中です。", BepInExStatusTone.Information);
                return;
            }
            DialogResult answer = MessageBox.Show(
                "作者管理のBepInEx導入・入れ直し手順を既定ブラウザで開きます。\r\n" +
                url +
                "\r\n\r\n外部サイト（GitHub Gist）へ移動します。このツールは手順やBepInExをダウンロードしません。続行しますか？",
                Text,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Information);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.ComponentModel.Win32Exception)
            {
                SetResult("導入・入れ直し手順を開けません: " + ex.Message, true);
            }
        }

        private static bool IsBepInExWritable(BepInExInspection inspection)
        {
            return inspection.AllowsPluginUse && !inspection.HasReparsePoint;
        }

        private void SetResult(string message, bool error)
        {
            resultValue.Text = message;
            resultValue.ForeColor = error ? Color.DarkRed : Color.DarkGreen;
        }

        private void SetResult(string message, BepInExStatusTone tone)
        {
            resultValue.Text = message;
            resultValue.ForeColor = GetBepInExStatusColor(tone);
        }

        private static Color GetBepInExStatusColor(BepInExStatusTone tone)
        {
            switch (tone)
            {
                case BepInExStatusTone.Information: return Color.DarkBlue;
                case BepInExStatusTone.Success: return Color.DarkGreen;
                case BepInExStatusTone.Warning: return Color.DarkOrange;
                case BepInExStatusTone.Error: return Color.DarkRed;
                default: return Color.DimGray;
            }
        }

        private static NumericUpDown CreateMultiplierInput()
        {
            return new NumericUpDown
            {
                Minimum = ConfigSchema.MinimumMultiplier,
                Maximum = ConfigSchema.MaximumMultiplier,
                DecimalPlaces = 1,
                Increment = 0.1m,
                Width = 120
            };
        }

        private static TableLayoutPanel CreateTwoColumnPanel()
        {
            TableLayoutPanel panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                AutoScroll = true,
                ColumnCount = 2,
                Padding = new Padding(8)
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            return panel;
        }

        private static void AddRow(TableLayoutPanel panel, int row, string name, Control value)
        {
            Label label = new Label { Text = name, AutoSize = true, Margin = new Padding(3, 7, 14, 6) };
            label.Font = new Font(label.Font, FontStyle.Bold);
            value.Margin = new Padding(3, 6, 3, 6);
            panel.Controls.Add(label, 0, row);
            panel.Controls.Add(value, 1, row);
        }

        private static Label CreateValueLabel()
        {
            return new Label { Text = "未選択", AutoSize = true, MaximumSize = new Size(700, 0) };
        }
    }
}
