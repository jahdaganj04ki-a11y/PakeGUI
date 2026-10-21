using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PakeGui.Core;

namespace PakeGui.Win;

/// <summary>Hauptfenster: Einstellungen links, Befehl/Protokoll und Build-Start rechts.</summary>
internal sealed class MainForm : Form
{
    private enum RunKind
    {
        None,
        Build,
        Environment,
        Install,
        Workflow,
    }

    private static readonly JsonSerializerOptions ProfileJson = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly string[] ArtifactExtensions =
        [".msi", ".exe", ".dmg", ".deb", ".rpm", ".appimage", ".zst", ".pkg"];

    private readonly SettingsPanel _settings = new();
    private readonly BuildRunner _runner = new();
    private readonly ToolTip _tips = new();

    private readonly ComboBox _runnerMode = new();
    private readonly TextBox _outputBox = new();
    private readonly TextBox _commandBox = new();
    private readonly RichTextBox _log = new();
    private readonly Label _summary = new();
    private readonly Button _buildButton;
    private readonly Button _cancelButton;
    private readonly ToolStripStatusLabel _status = new();
    private readonly ToolStripProgressBar _progress = new();

    private RunKind _running = RunKind.None;
    private string _customRunner = string.Empty;
    private string _lastOutputDirectory = string.Empty;
    private bool _updatingRunnerMode;

    public MainForm()
    {
        Text = "Pake GUI für Windows – Webseite als Desktop-App";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1020, 680);
        ClientSize = new Size(1200, 800);
        BackColor = Theme.Surface;
        Font = Theme.Body;
        AutoScaleMode = AutoScaleMode.Font;
        KeyPreview = true;

        try
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        }
        catch (Exception)
        {
            Icon = SystemIcons.Application;
        }

        _buildButton = Theme.PrimaryButton("Build starten", 134);
        _cancelButton = Theme.SecondaryButton("Abbrechen", 100);
        _buildButton.Click += (_, _) => StartBuild();
        _cancelButton.Click += (_, _) => _runner.Cancel();
        _cancelButton.Enabled = false;

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = Theme.Surface,
            Panel1MinSize = 520,
            Panel2MinSize = 390,
        };
        split.Panel1.BackColor = Theme.Surface;
        split.Panel2.BackColor = Theme.Surface;
        split.Panel1.Controls.Add(_settings);
        split.Panel2.Controls.Add(BuildRightPanel());

        var header = BuildHeader();
        var menu = BuildMenu();
        var statusStrip = BuildStatusStrip();

        Controls.Add(split);
        Controls.Add(header);
        Controls.Add(menu);
        Controls.Add(statusStrip);
        MainMenuStrip = menu;

        ActiveControl = _settings;

        _runner.OutputReceived += (_, line) => AppendLog(line);
        _runner.Completed += OnRunnerCompleted;
        _settings.SettingsChanged += (_, _) => UpdatePreview();
        _outputBox.TextChanged += (_, _) => UpdatePreview();
        _runnerMode.SelectedIndexChanged += (_, _) => OnRunnerModeChanged();

        Shown += (_, _) =>
        {
            try
            {
                split.SplitterDistance = 700;
            }
            catch (InvalidOperationException)
            {
                // Fenster zu schmal – Standardaufteilung beibehalten.
            }
        };

        LoadAutoSettings();
        UpdatePreview();

        KeyDown += OnGlobalKeyDown;
        FormClosing += OnFormClosing;
    }

    // ------------------------------------------------------------------
    // Aufbau der Oberflaeche
    // ------------------------------------------------------------------

    private static Panel BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 70,
            BackColor = Theme.Header,
            Padding = new Padding(18, 12, 18, 10),
        };

        var title = new Label
        {
            Text = "Pake GUI",
            Font = Theme.Title,
            ForeColor = Color.White,
            AutoSize = true,
            Location = new Point(20, 10),
        };

        var subtitle = new Label
        {
            Text = "Alle Einstellungen des GitHub-Workflows \u201eBuild App With Pake CLI\u201c \u2013 lokal als Windows-App bauen",
            Font = Theme.Subtitle,
            ForeColor = Theme.HeaderAccent,
            AutoSize = true,
            Location = new Point(22, 39),
        };

        var link = new LinkLabel
        {
            Text = "Pake auf GitHub",
            Font = Theme.Small,
            LinkColor = Theme.HeaderAccent,
            ActiveLinkColor = Color.White,
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        link.Location = new Point(header.Width - 120, 45);
        link.LinkClicked += (_, _) => OpenUrl("https://github.com/tw93/Pake");

        header.Controls.Add(title);
        header.Controls.Add(subtitle);
        header.Controls.Add(link);
        header.Resize += (_, _) => link.Location = new Point(header.Width - link.Width - 22, 30);
        return header;
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip
        {
            BackColor = Theme.Card,
            Padding = new Padding(6, 2, 0, 2),
        };

        var file = new ToolStripMenuItem("&Datei");
        file.DropDownItems.Add(Item("Profil speichern…", SaveProfile, Keys.Control | Keys.S));
        file.DropDownItems.Add(Item("Profil laden…", LoadProfile, Keys.Control | Keys.O));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Einstellungen zurücksetzen", () =>
        {
            _settings.ResetToDefaults();
            _outputBox.Text = DefaultOutputDirectory;
            _runnerMode.SelectedIndex = 0;
            UpdatePreview();
        }));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Ausgabeordner öffnen", OpenOutputFolder));
        file.DropDownItems.Add(new ToolStripSeparator());
        file.DropDownItems.Add(Item("Beenden", Close));

        var build = new ToolStripMenuItem("&Build");
        build.DropDownItems.Add(Item("Build starten", StartBuild, Keys.F5));
        build.DropDownItems.Add(Item("Build abbrechen", () => _runner.Cancel()));
        build.DropDownItems.Add(new ToolStripSeparator());
        build.DropDownItems.Add(Item("Befehl kopieren", CopyCommand));
        build.DropDownItems.Add(Item("Umgebung prüfen", CheckEnvironment));
        build.DropDownItems.Add(new ToolStripSeparator());
        build.DropDownItems.Add(Item("Pake-CLI installieren (npm -g)", InstallPake));
        build.DropDownItems.Add(Item("Pake-CLI-Dokumentation öffnen",
            () => OpenUrl("https://github.com/tw93/Pake/blob/master/docs/cli-usage.md")));

        var workflow = new ToolStripMenuItem("&Workflow");
        workflow.DropDownItems.Add(Item("Workflow starten (gh CLI)…", StartWorkflow));
        workflow.DropDownItems.Add(Item("Letzte Workflow-Läufe anzeigen", ShowWorkflowRuns));
        workflow.DropDownItems.Add(new ToolStripSeparator());
        workflow.DropDownItems.Add(Item("gh-Befehl kopieren", CopyGhCommand));
        workflow.DropDownItems.Add(Item("Dispatch-JSON kopieren", CopyDispatchJson));
        workflow.DropDownItems.Add(Item("Dispatch-JSON speichern…", SaveDispatchJson));
        workflow.DropDownItems.Add(new ToolStripSeparator());
        workflow.DropDownItems.Add(Item("Workflow im Browser öffnen", () => OpenUrl(WorkflowUrl)));

        var help = new ToolStripMenuItem("&Hilfe");
        help.DropDownItems.Add(Item("Über Pake GUI", ShowAbout));

        menu.Items.AddRange([file, build, workflow, help]);
        return menu;
    }

    private StatusStrip BuildStatusStrip()
    {
        var strip = new StatusStrip
        {
            BackColor = Theme.Card,
            SizingGrip = false,
            Font = Theme.Small,
        };

        _status.Text = "Bereit.";
        _status.Spring = true;
        _status.TextAlign = ContentAlignment.MiddleLeft;

        _progress.Style = ProgressBarStyle.Marquee;
        _progress.Visible = false;
        _progress.Width = 130;

        strip.Items.Add(_status);
        strip.Items.Add(_progress);
        return strip;
    }

    private Control BuildRightPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Theme.Surface,
            Padding = new Padding(10, 10, 12, 10),
            Margin = new Padding(0),
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 122));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 132));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(BuildExecutionGroup(), 0, 0);
        panel.Controls.Add(BuildCommandGroup(), 0, 1);

        _summary.Dock = DockStyle.Fill;
        _summary.Font = Theme.Small;
        _summary.ForeColor = Theme.Muted;
        _summary.TextAlign = ContentAlignment.MiddleLeft;
        _summary.Margin = new Padding(2, 4, 0, 0);
        panel.Controls.Add(_summary, 0, 2);

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
            Margin = new Padding(0),
        };
        _buildButton.Margin = new Padding(0, 6, 8, 0);
        _cancelButton.Margin = new Padding(0, 6, 8, 0);
        var copyButton = Theme.SecondaryButton("Befehl kopieren", 138);
        copyButton.Margin = new Padding(0, 6, 0, 0);
        copyButton.Click += (_, _) => CopyCommand();
        buttonBar.Controls.AddRange([_buildButton, _cancelButton, copyButton]);
        panel.Controls.Add(buttonBar, 0, 3);

        panel.Controls.Add(BuildLogToolbar(), 0, 4);
        panel.Controls.Add(BuildLogBox(), 0, 5);

        return panel;
    }

    private Control BuildExecutionGroup()
    {
        var group = new GroupBox
        {
            Text = "Build-Ausführung",
            Dock = DockStyle.Fill,
            Font = Theme.BodyBold,
            ForeColor = Theme.Text,
            BackColor = Theme.Card,
            Padding = new Padding(10, 4, 10, 8),
            Margin = new Padding(0, 0, 0, 8),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Theme.Card,
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 108));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));

        _outputBox.Font = Theme.Body;
        _outputBox.BorderStyle = BorderStyle.FixedSingle;
        _outputBox.Margin = new Padding(0);
        var outputHost = WithButton(_outputBox, "Ordner wählen", BrowseOutputFolder);

        _runnerMode.Font = Theme.Body;
        _runnerMode.DropDownStyle = ComboBoxStyle.DropDownList;
        _runnerMode.Items.AddRange(
        [
            "Automatisch (pake, sonst npx)",
            "npx pake-cli@latest",
            "Global installiertes pake",
            "Eigener Befehl…",
        ]);
        _runnerMode.SelectedIndex = 0;
        _runnerMode.Margin = new Padding(0);
        _runnerMode.Anchor = AnchorStyles.Left | AnchorStyles.Right;

        table.Controls.Add(RowLabel("Ausgabeordner"), 0, 0);
        table.Controls.Add(outputHost, 1, 0);
        table.Controls.Add(RowLabel("Pake-Aufruf"), 0, 1);
        table.Controls.Add(_runnerMode, 1, 1);

        group.Controls.Add(table);
        _tips.SetToolTip(_runnerMode, "Wie die Pake-CLI gestartet wird. Ohne globale Installation wird npx verwendet.");
        return group;
    }

    private Control BuildCommandGroup()
    {
        var group = new GroupBox
        {
            Text = "Befehl",
            Dock = DockStyle.Fill,
            Font = Theme.BodyBold,
            ForeColor = Theme.Text,
            BackColor = Theme.Card,
            Padding = new Padding(10, 4, 10, 8),
            Margin = new Padding(0, 0, 0, 6),
        };

        _commandBox.Multiline = true;
        _commandBox.ReadOnly = true;
        _commandBox.Dock = DockStyle.Fill;
        _commandBox.Font = Theme.Mono;
        _commandBox.BorderStyle = BorderStyle.FixedSingle;
        _commandBox.BackColor = Color.FromArgb(248, 250, 252);
        _commandBox.ForeColor = Theme.Text;
        _commandBox.ScrollBars = ScrollBars.Vertical;
        _commandBox.WordWrap = false;

        group.Controls.Add(_commandBox);
        _tips.SetToolTip(_commandBox, "Dieser Aufruf wird beim Build ausgeführt (Strg+Shift+C kopiert ihn).");
        return group;
    }

    private Control BuildLogToolbar()
    {
        var bar = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface };

        var caption = new Label
        {
            Text = "Protokoll",
            Font = Theme.BodyBold,
            ForeColor = Theme.Text,
            AutoSize = true,
            Location = new Point(2, 8),
        };

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Theme.Surface,
        };

        foreach (var (text, action) in new (string, Action)[]
                 {
                     ("Umgebung prüfen", CheckEnvironment),
                     ("Ausgabeordner", OpenOutputFolder),
                     ("Leeren", () => _log.Clear()),
                 })
        {
            var button = new Button
            {
                Text = text,
                AutoSize = true,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Card,
                ForeColor = Theme.Text,
                Font = Theme.Small,
                Margin = new Padding(4, 0, 0, 0),
            };
            button.FlatAppearance.BorderColor = Theme.Border;
            button.Click += (_, _) => action();
            actions.Controls.Add(button);
        }

        bar.Controls.Add(actions);
        bar.Controls.Add(caption);
        return bar;
    }

    private Control BuildLogBox()
    {
        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Theme.LogBackground;
        _log.ForeColor = Theme.LogText;
        _log.Font = Theme.Mono;
        _log.WordWrap = true;
        _log.DetectUrls = false;
        _log.HideSelection = false;
        _log.Margin = new Padding(0, 2, 0, 0);

        var host = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.LogBackground,
            Padding = new Padding(8, 6, 4, 6),
        };
        host.Controls.Add(_log);
        return host;
    }

    private static Label RowLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.Text,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 8, 0),
    };

    private static Control WithButton(Control main, string tooltip, Action onClick)
    {
        var host = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            BackColor = main.BackColor,
            Margin = new Padding(0, 4, 0, 4),
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 38));
        host.MinimumSize = new Size(0, 30);

        main.Dock = DockStyle.Fill;
        var button = Theme.BrowseButton(tooltip);
        button.Margin = new Padding(4, 0, 0, 0);
        button.Dock = DockStyle.Fill;
        button.Click += (_, _) => onClick();

        host.Controls.Add(main, 0, 0);
        host.Controls.Add(button, 1, 0);
        return host;
    }

    private static ToolStripMenuItem Item(string text, Action action, Keys shortcut = Keys.None)
    {
        var item = new ToolStripMenuItem(text)
        {
            ShowShortcutKeys = shortcut != Keys.None,
            ShortcutKeys = shortcut,
        };
        item.Click += (_, _) => action();
        return item;
    }

    // ------------------------------------------------------------------
    // Einstellungen und Vorschau
    // ------------------------------------------------------------------

    private static string WorkflowUrl =>
        $"https://github.com/{WorkflowDispatch.Repository}/actions/workflows/{WorkflowDispatch.WorkflowFile}";

    private static string ProfileDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PakeGUIWin");

    private static string AutoSavePath => Path.Combine(ProfileDirectory, "settings.json");

    private static string DefaultOutputDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PakeBuilds");

    private PakeSettings CollectAll()
    {
        var settings = _settings.Collect();
        settings.OutputDirectory = _outputBox.Text.Trim();
        settings.RunnerMode = (PakeRunnerMode)Math.Clamp(_runnerMode.SelectedIndex, 0, 3);
        settings.CustomRunnerPath = _customRunner;

        if (string.IsNullOrWhiteSpace(settings.OutputDirectory))
            settings.OutputDirectory = DefaultOutputDirectory;

        return settings;
    }

    private void UpdatePreview()
    {
        var settings = CollectAll();
        var command = PakeCommandLineBuilder.Build(settings);

        var preview = new StringBuilder();
        preview.Append("cd \"").Append(settings.OutputDirectory).Append('"').AppendLine();
        preview.Append(command.Display);
        _commandBox.Text = preview.ToString();

        var issues = PakeValidation.Validate(settings);
        var errors = issues.Where(i => i.IsError).ToList();
        var warnings = issues.Where(i => !i.IsError).ToList();

        if (errors.Count > 0)
        {
            _summary.ForeColor = Theme.Danger;
            _summary.Text = "\u2716 " + errors[0].Message +
                            (errors.Count > 1 ? $"  (+{errors.Count - 1} weitere Fehler)" : string.Empty);
        }
        else if (warnings.Count > 0)
        {
            _summary.ForeColor = Theme.Warning;
            _summary.Text = "\u26A0 " + warnings[0].Message +
                            (warnings.Count > 1 ? $"  (+{warnings.Count - 1} weitere Hinweise)" : string.Empty);
        }
        else
        {
            _summary.ForeColor = Theme.Success;
            _summary.Text = "\u2714 Einstellungen vollständig – bereit zum Bauen.";
        }

        var tip = string.Join(
            Environment.NewLine,
            errors.Concat(warnings).Select(i => (i.IsError ? "Fehler: " : "Hinweis: ") + i.Message));

        if (command.Warnings.Count > 0)
            tip = (tip.Length > 0 ? tip + Environment.NewLine : string.Empty) +
                  string.Join(Environment.NewLine, command.Warnings.Select(w => "Pake: " + w));

        _tips.SetToolTip(_summary, tip.Length > 0 ? tip : "Keine Auffälligkeiten.");
    }

    private void OnRunnerModeChanged()
    {
        if (_updatingRunnerMode)
            return;

        if (_runnerMode.SelectedIndex == (int)PakeRunnerMode.Custom)
        {
            var value = PromptForm.Ask(
                this,
                "Eigener Pake-Befehl",
                "Befehl oder Pfad (Argumente erlaubt), z. B. pnpm pake oder C:\\tools\\pake.exe:",
                _customRunner.Length > 0 ? _customRunner : "npx --yes pake-cli@latest");

            if (string.IsNullOrWhiteSpace(value))
            {
                _updatingRunnerMode = true;
                _runnerMode.SelectedIndex = (int)PakeRunnerMode.Auto;
                _updatingRunnerMode = false;
            }
            else
            {
                _customRunner = value.Trim();
                _tips.SetToolTip(_runnerMode, "Eigener Befehl: " + _customRunner);
            }
        }

        UpdatePreview();
    }

    private void BrowseOutputFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Ordner für die fertigen Installationsdateien",
            UseDescriptionForTitle = true,
        };

        if (Directory.Exists(_outputBox.Text.Trim()))
            dialog.SelectedPath = _outputBox.Text.Trim();

        if (dialog.ShowDialog(this) == DialogResult.OK)
            _outputBox.Text = dialog.SelectedPath;
    }

    // ------------------------------------------------------------------
    // Build
    // ------------------------------------------------------------------

    private void StartBuild()
    {
        if (_runner.IsRunning)
            return;

        var settings = CollectAll();
        var issues = PakeValidation.Validate(settings);
        var errors = issues.Where(i => i.IsError).ToList();

        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                "Bitte zuerst korrigieren:" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => "\u2022 " + e.Message)),
                "Eingaben prüfen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Directory.CreateDirectory(settings.OutputDirectory);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Der Ausgabeordner konnte nicht angelegt werden:" + Environment.NewLine + ex.Message,
                "Ausgabeordner", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        var command = PakeCommandLineBuilder.Build(settings);
        _lastOutputDirectory = settings.OutputDirectory;

        AppendLog(string.Empty);
        AppendLog("=== Build gestartet ===", Theme.HeaderAccent);
        AppendLog("Befehl: " + command.Display);
        AppendLog("Ausgabeordner: " + settings.OutputDirectory);

        foreach (var warning in command.Warnings)
            AppendLog("Hinweis: " + warning, Theme.Warning);

        foreach (var warning in issues.Where(i => !i.IsError))
            AppendLog("Hinweis: " + warning.Message, Theme.Warning);

        var script = PakeCommandLineBuilder.CreateScript(command, settings.OutputDirectory);

        SetRunning(RunKind.Build);
        try
        {
            _runner.Start(script, command.Display);
        }
        catch (Exception ex)
        {
            AppendLog("Start fehlgeschlagen: " + ex.Message, Theme.Danger);
            SetRunning(RunKind.None);
        }
    }

    private void CheckEnvironment()
    {
        if (_runner.IsRunning)
            return;

        var script = OperatingSystem.IsWindows() ? WindowsEnvironmentScript : PosixEnvironmentScript;
        AppendLog(string.Empty);
        AppendLog("=== Umgebung prüfen ===", Theme.HeaderAccent);
        SetRunning(RunKind.Environment);

        try
        {
            _runner.Start(script, "Umgebungsprüfung");
        }
        catch (Exception ex)
        {
            AppendLog("Start fehlgeschlagen: " + ex.Message, Theme.Danger);
            SetRunning(RunKind.None);
        }
    }

    private void InstallPake()
    {
        if (_runner.IsRunning)
            return;

        if (MessageBox.Show(
                this,
                "Pake-CLI global installieren?" + Environment.NewLine + Environment.NewLine +
                "npm install -g pake-cli" + Environment.NewLine + Environment.NewLine +
                "Das kann einige Minuten dauern und benötigt Node.js sowie Internet.",
                "Pake-CLI installieren",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        var script = OperatingSystem.IsWindows()
            ? "@echo off\r\nchcp 65001 >nul\r\necho [PakeGUI] npm install -g pake-cli\r\nnpm install -g pake-cli\r\n"
            : "#!/bin/sh\nnpm install -g pake-cli\n";

        AppendLog(string.Empty);
        AppendLog("=== Pake-CLI installieren ===", Theme.HeaderAccent);
        SetRunning(RunKind.Install);

        try
        {
            _runner.Start(script, "npm install -g pake-cli");
        }
        catch (Exception ex)
        {
            AppendLog("Start fehlgeschlagen: " + ex.Message, Theme.Danger);
            SetRunning(RunKind.None);
        }
    }

    private void StartWorkflow()
    {
        if (_runner.IsRunning)
            return;

        var settings = CollectAll();
        var errors = PakeValidation.Validate(settings).Where(i => i.IsError).ToList();
        if (errors.Count > 0)
        {
            MessageBox.Show(
                this,
                "Der Workflow braucht vollständige Eingaben:" + Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, errors.Select(e => "\u2022 " + e.Message)),
                "Eingaben prüfen",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var gh = WorkflowDispatch.ToGhCommand(settings);

        if (MessageBox.Show(
                this,
                "Diesen Workflow auf GitHub starten?" + Environment.NewLine + Environment.NewLine + gh +
                Environment.NewLine + Environment.NewLine +
                "Voraussetzung: GitHub CLI (gh) ist installiert und mit 'gh auth login' angemeldet.",
                "Online-Build starten",
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Question) != DialogResult.OK)
        {
            return;
        }

        var script = OperatingSystem.IsWindows()
            ? "@echo off\r\nchcp 65001 >nul\r\n" +
              "echo [PakeGUI] Starte GitHub-Workflow ...\r\n" +
              "where gh >nul 2>&1 || (echo   gh CLI nicht gefunden - https://cli.github.com & exit /b 1)\r\n" +
              gh + "\r\n"
            : "#!/bin/sh\ncommand -v gh >/dev/null || { echo 'gh CLI nicht gefunden'; exit 1; }\n" + gh + "\n";

        AppendLog(string.Empty);
        AppendLog("=== GitHub-Workflow wird gestartet ===", Theme.HeaderAccent);
        AppendLog(gh);
        SetRunning(RunKind.Workflow);

        try
        {
            _runner.Start(script, gh);
        }
        catch (Exception ex)
        {
            AppendLog("Start fehlgeschlagen: " + ex.Message, Theme.Danger);
            SetRunning(RunKind.None);
        }
    }

    private void ShowWorkflowRuns()
    {
        if (_runner.IsRunning)
            return;

        var command = $"gh run list --repo {WorkflowDispatch.Repository} " +
                      $"--workflow {WorkflowDispatch.WorkflowFile} --limit 5";

        AppendLog(string.Empty);
        AppendLog("=== Letzte Workflow-Läufe ===", Theme.HeaderAccent);
        SetRunning(RunKind.Workflow);

        try
        {
            _runner.Start(OperatingSystem.IsWindows()
                ? "@echo off\r\nchcp 65001 >nul\r\n" + command + "\r\n"
                : "#!/bin/sh\n" + command + "\n", command);
        }
        catch (Exception ex)
        {
            AppendLog("Start fehlgeschlagen: " + ex.Message, Theme.Danger);
            SetRunning(RunKind.None);
        }
    }

    private void OnRunnerCompleted(object? sender, int exitCode)
    {
        if (IsDisposed)
            return;

        try
        {
            BeginInvoke(() =>
            {
                var kind = _running;
                SetRunning(RunKind.None);

                switch (kind)
                {
                    case RunKind.Build when exitCode == 0:
                        AppendLog("=== Build erfolgreich beendet (Exitcode 0) ===", Theme.Success);
                        _status.Text = "Build erfolgreich.";
                        ReportArtifacts();
                        break;

                    case RunKind.Build:
                        AppendLog($"=== Build fehlgeschlagen (Exitcode {exitCode}) ===", Theme.Danger);
                        _status.Text = $"Build fehlgeschlagen (Exitcode {exitCode}).";
                        break;

                    case RunKind.Environment:
                        AppendLog("=== Prüfung beendet ===", Theme.Success);
                        _status.Text = "Prüfung beendet.";
                        break;

                    case RunKind.Install:
                        AppendLog(exitCode == 0
                            ? "=== Pake-CLI installiert ==="
                            : $"=== Installation fehlgeschlagen (Exitcode {exitCode}) ===",
                            exitCode == 0 ? Theme.Success : Theme.Danger);
                        _status.Text = "npm beendet.";
                        break;

                    case RunKind.Workflow:
                        if (exitCode == 0)
                        {
                            AppendLog("=== An GitHub gesendet ===", Theme.Success);
                            AppendLog("Läufe: " + WorkflowUrl);
                            _status.Text = "Workflow gestartet.";
                        }
                        else
                        {
                            AppendLog($"=== gh beendet mit Exitcode {exitCode} ===", Theme.Danger);
                            AppendLog("Bei Problemen: 'gh auth login' ausführen oder GH_TOKEN setzen.");
                            _status.Text = "Workflow-Aufruf fehlgeschlagen.";
                        }

                        break;

                    default:
                        _status.Text = "Bereit.";
                        break;
                }
            });
        }
        catch (InvalidOperationException)
        {
            // Fenster wurde bereits geschlossen.
        }
    }

    private void ReportArtifacts()
    {
        try
        {
            if (!Directory.Exists(_lastOutputDirectory))
                return;

            var since = DateTime.Now.AddHours(-3);
            var artifacts = new DirectoryInfo(_lastOutputDirectory)
                .EnumerateFiles()
                .Where(f => f.LastWriteTime >= since &&
                            ArtifactExtensions.Contains(f.Extension.ToLowerInvariant()))
                .OrderByDescending(f => f.LastWriteTime)
                .Take(6)
                .ToList();

            if (artifacts.Count == 0)
            {
                AppendLog("Kein neues Installationspaket im Ausgabeordner gefunden.");
                return;
            }

            foreach (var file in artifacts)
                AppendLog($"Artefakt: {file.FullName} ({FormatSize(file.Length)})", Theme.Success);
        }
        catch (Exception ex)
        {
            AppendLog("Artefakte konnten nicht gelesen werden: " + ex.Message);
        }
    }

    private static string FormatSize(long bytes) => bytes switch
    {
        > 1024L * 1024 * 1024 => $"{bytes / 1024d / 1024 / 1024:0.0} GB",
        > 1024L * 1024 => $"{bytes / 1024d / 1024:0.0} MB",
        > 1024 => $"{bytes / 1024d:0} KB",
        _ => $"{bytes} B",
    };

    private void SetRunning(RunKind kind)
    {
        _running = kind;
        var running = kind != RunKind.None;

        _buildButton.Enabled = !running;
        _cancelButton.Enabled = running;
        _settings.Enabled = !running;
        _runnerMode.Enabled = !running;
        _outputBox.Enabled = !running;
        _progress.Visible = running;

        if (running)
        {
            _status.Text = kind switch
            {
                RunKind.Build => "Build läuft …",
                RunKind.Environment => "Prüfe Umgebung …",
                RunKind.Install => "npm läuft …",
                _ => "GitHub-Workflow …",
            };
        }
        else if ((_status.Text ?? string.Empty).EndsWith('…'))
        {
            _status.Text = "Bereit.";
        }
    }

    // ------------------------------------------------------------------
    // Protokoll
    // ------------------------------------------------------------------

    private void AppendLog(string message, Color? color = null)
    {
        if (_log.IsDisposed)
            return;

        if (_log.InvokeRequired)
        {
            try
            {
                _log.BeginInvoke(new Action(() => AppendLog(message, color)));
            }
            catch (InvalidOperationException)
            {
                // Fenster bereits geschlossen.
            }

            return;
        }

        if (_log.TextLength > 400_000)
            _log.Clear();

        _log.SelectionStart = _log.TextLength;
        _log.SelectionColor = color ?? Theme.LogText;
        _log.AppendText(message.Length == 0
            ? Environment.NewLine
            : $"{DateTime.Now:HH:mm:ss}  {message}{Environment.NewLine}");
        _log.SelectionStart = _log.TextLength;
        _log.ScrollToCaret();
    }

    // ------------------------------------------------------------------
    // Kopieren / Öffnen
    // ------------------------------------------------------------------

    private void CopyCommand()
    {
        CopyToClipboard(_commandBox.Text, "Befehl kopiert.");
    }

    private void CopyGhCommand()
    {
        var command = WorkflowDispatch.ToGhCommand(CollectAll());
        CopyToClipboard(command, "gh-Befehl kopiert.");
        AppendLog("gh-Befehl: " + command);
    }

    private void CopyDispatchJson()
    {
        var json = WorkflowDispatch.ToDispatchJson(CollectAll());
        CopyToClipboard(json, "Dispatch-JSON kopiert.");
    }

    private void SaveDispatchJson()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "workflow_dispatch-Payload speichern",
            Filter = "JSON (*.json)|*.json|Alle Dateien (*.*)|*.*",
            FileName = "pake-workflow-dispatch.json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, WorkflowDispatch.ToDispatchJson(CollectAll()));
            _status.Text = "Dispatch-JSON gespeichert: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "JSON konnte nicht gespeichert werden:" + Environment.NewLine + ex.Message,
                "Dispatch-JSON", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyToClipboard(string text, string status)
    {
        try
        {
            Clipboard.SetText(text);
            _status.Text = status;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Kopieren fehlgeschlagen: " + ex.Message, "Zwischenablage",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void OpenOutputFolder()
    {
        var dir = _outputBox.Text.Trim();
        if (dir.Length == 0 || !Directory.Exists(dir))
        {
            MessageBox.Show(this, "Der Ausgabeordner existiert noch nicht.", "Ausgabeordner",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{dir}\"") { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppendLog("Ordner konnte nicht geöffnet werden: " + ex.Message, Theme.Danger);
        }
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception)
        {
            // Kein Standardbrowser eingerichtet.
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "Pake GUI für Windows" + Environment.NewLine +
            "Version " + Application.ProductVersion + Environment.NewLine + Environment.NewLine +
            "Native Oberfläche für die Pake-CLI (https://github.com/tw93/Pake)." + Environment.NewLine +
            "Bildet alle Eingaben des GitHub-Workflows \u201eBuild App With Pake CLI\u201c ab" + Environment.NewLine +
            "und kann zusätzlich jede weitere CLI-Option setzen." + Environment.NewLine + Environment.NewLine +
            "Pake steht unter GPL-3.0; mit Pake erzeugte Apps gehören dir.",
            "Über Pake GUI",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    // ------------------------------------------------------------------
    // Profile
    // ------------------------------------------------------------------

    private void SaveProfile()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Profil speichern",
            Filter = "Pake-Profil (*.json)|*.json|Alle Dateien (*.*)|*.*",
            FileName = "pake-profil.json",
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(CollectAll(), ProfileJson));
            _status.Text = "Profil gespeichert: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Profil konnte nicht gespeichert werden:" + Environment.NewLine + ex.Message,
                "Profil speichern", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadProfile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Profil laden",
            Filter = "Pake-Profil (*.json)|*.json|Alle Dateien (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            var settings = JsonSerializer.Deserialize<PakeSettings>(
                File.ReadAllText(dialog.FileName), ProfileJson);

            if (settings is null)
                throw new InvalidDataException("Die Datei enthält keine Einstellungen.");

            ApplySettings(settings);
            _status.Text = "Profil geladen: " + dialog.FileName;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Profil konnte nicht geladen werden:" + Environment.NewLine + ex.Message,
                "Profil laden", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplySettings(PakeSettings settings)
    {
        _settings.Apply(settings);
        _outputBox.Text = string.IsNullOrWhiteSpace(settings.OutputDirectory)
            ? DefaultOutputDirectory
            : settings.OutputDirectory;

        _customRunner = settings.CustomRunnerPath;
        _updatingRunnerMode = true;
        _runnerMode.SelectedIndex = (int)settings.RunnerMode;
        _updatingRunnerMode = false;

        UpdatePreview();
    }

    private void LoadAutoSettings()
    {
        try
        {
            if (File.Exists(AutoSavePath))
            {
                var settings = JsonSerializer.Deserialize<PakeSettings>(
                    File.ReadAllText(AutoSavePath), ProfileJson);

                if (settings is not null)
                {
                    ApplySettings(settings);
                    return;
                }
            }
        }
        catch (Exception)
        {
            // Beschädigte Datei ignorieren und mit Standardwerten starten.
        }

        _settings.ResetToDefaults();
        _outputBox.Text = DefaultOutputDirectory;
        UpdatePreview();
    }

    private void SaveAutoSettings()
    {
        try
        {
            Directory.CreateDirectory(ProfileDirectory);
            File.WriteAllText(AutoSavePath, JsonSerializer.Serialize(CollectAll(), ProfileJson));
        }
        catch (Exception)
        {
            // Beim Beenden nicht weiter stören.
        }
    }

    // ------------------------------------------------------------------
    // Ereignisse
    // ------------------------------------------------------------------

    private void OnGlobalKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            StartBuild();
            e.Handled = true;
        }
        else if (e.Control && e.Shift && e.KeyCode == Keys.C)
        {
            CopyCommand();
            e.Handled = true;
        }
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_runner.IsRunning)
        {
            if (MessageBox.Show(
                    this,
                    "Es läuft noch ein Build. Abbrechen und Programm schließen?",
                    "Build läuft",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _runner.Cancel();
        }

        SaveAutoSettings();
        _runner.Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _runner.Dispose();
            _tips.Dispose();
        }

        base.Dispose(disposing);
    }

    // ------------------------------------------------------------------
    // Skripte
    // ------------------------------------------------------------------

    private const string WindowsEnvironmentScript =
        """
        @echo off
        chcp 65001 >nul
        echo [PakeGUI] Prüfe Voraussetzungen für Pake ...
        echo.
        echo --- Node.js ---
        where node 2>nul && node --version || echo   node nicht gefunden
        echo --- npm ---
        where npm 2>nul && npm --version || echo   npm nicht gefunden
        echo --- Rust ---
        where rustc 2>nul && rustc --version || echo   rustc nicht gefunden
        where cargo 2>nul && cargo --version || echo   cargo nicht gefunden
        echo --- Visual Studio Build Tools ---
        where cl 2>nul || echo   cl.exe nicht im PATH (MSVC-Builds brauchen die Build Tools)
        echo --- pake ---
        where pake 2>nul || echo   pake nicht global installiert (npx wird verwendet)
        echo --- WebView2 Runtime ---
        reg query "HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}" /v pv 2>nul || echo   WebView2 nicht erkannt
        echo.
        echo [PakeGUI] Prüfung abgeschlossen.
        """;

    private const string PosixEnvironmentScript =
        """
        #!/bin/sh
        echo "[PakeGUI] Prüfe Voraussetzungen für Pake ..."
        echo "--- node ---"; node --version 2>/dev/null || echo "   node nicht gefunden"
        echo "--- npm ---"; npm --version 2>/dev/null || echo "   npm nicht gefunden"
        echo "--- cargo ---"; cargo --version 2>/dev/null || echo "   cargo nicht gefunden"
        echo "--- pake ---"; command -v pake || echo "   pake nicht global installiert (npx wird verwendet)"
        echo "[PakeGUI] Prüfung abgeschlossen."
        """;
}
