using PakeGui.Core;

namespace PakeGui.Win;

/// <summary>
/// Alle Einstellungen als Tabs. Jede Zeile registriert einen Lese- und einen
/// Schreibzugriff, damit <see cref="Collect"/> und <see cref="Load"/> automatisch
/// vollstaendig bleiben.
/// </summary>
internal sealed class SettingsPanel : UserControl
{
    private readonly DarkTabStrip _tabStrip = new();
    private readonly Panel _content = new();
    private readonly List<Panel> _pages = [];
    private readonly List<Action<PakeSettings>> _apply = [];
    private readonly List<Action<PakeSettings>> _load = [];

    private PakeSettings _baseline = new();
    private bool _loading;

    private Label _platformHint = null!;
    private ComboBox _targetsBox = null!;
    private ComboBox _platformBox = null!;

    public event EventHandler? SettingsChanged;

    public SettingsPanel()
    {
        Dock = DockStyle.Fill;
        BackColor = Theme.Surface;
        Padding = new Padding(0);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = Theme.Surface;

        Controls.Add(_content);
        Controls.Add(_tabStrip);

        BuildTabs();
        _tabStrip.SelectedIndexChanged += (_, _) => ShowPage(_tabStrip.SelectedIndex);
        ShowPage(0);

        Apply(new PakeSettings());
    }

    // ------------------------------------------------------------------
    // Oeffentliche API
    // ------------------------------------------------------------------

    public PakeSettings Collect()
    {
        var settings = _baseline.Clone();
        foreach (var apply in _apply)
            apply(settings);
        return settings;
    }

    public void Apply(PakeSettings settings)
    {
        _baseline = settings.Clone();
        _loading = true;
        try
        {
            foreach (var load in _load)
                load(settings);

            _platformHint.Text = PakePlatforms.LocalBuildHint(settings.Platform);
            RefreshTargets(settings.Platform);
        }
        finally
        {
            _loading = false;
        }

        RaiseChanged();
    }

    public void ResetToDefaults() => Apply(new PakeSettings());

    private void RaiseChanged()
    {
        if (!_loading)
            SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    // ------------------------------------------------------------------
    // Aufbau
    // ------------------------------------------------------------------

    private void BuildTabs()
    {
        var pages = new (string Title, Panel Page)[]
        {
            ("Workflow", BuildWorkflowTab()),
            ("Fenster", BuildWindowTab()),
            ("Funktionen", BuildFeaturesTab()),
            ("Netzwerk", BuildNetworkTab()),
            ("Build", BuildBuildTab()),
        };

        foreach (var (_, page) in pages)
        {
            page.Visible = false;
            _pages.Add(page);
            _content.Controls.Add(page);
        }

        _tabStrip.SetTitles(pages.Select(p => p.Title).ToList());
    }

    private void ShowPage(int index)
    {
        if (index < 0 || index >= _pages.Count)
            return;

        for (var i = 0; i < _pages.Count; i++)
            _pages[i].Visible = i == index;

        _pages[index].BringToFront();
    }

    private static (Panel Page, TableLayoutPanel Table) CreatePage(string intro)
    {
        var page = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Surface,
            AutoScroll = true,
            Padding = new Padding(12, 10, 8, 10),
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 3,
            BackColor = Theme.Surface,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 6, 20),
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 226));

        page.Controls.Add(table);

        if (!string.IsNullOrEmpty(intro))
        {
            var row = AddRow(table);
            var label = new Label
            {
                Text = intro,
                AutoSize = true,
                MaximumSize = new Size(700, 0),
                Font = Theme.Small,
                ForeColor = Theme.Muted,
                Margin = new Padding(0, 0, 0, 10),
            };
            table.Controls.Add(label, 0, row);
            table.SetColumnSpan(label, 3);
        }

        return (page, table);
    }

    private Panel BuildWorkflowTab()
    {
        var (page, t) = CreatePage(
            "Diese Seite entspricht genau den Eingaben des GitHub-Workflows \u201eBuild App With Pake CLI\u201c " +
            "(pake-cli.yaml). Jede Einstellung wird beim Start als passende Pake-CLI-Option uebergeben.");

        AddSection(t, "Ziel und Quelle");

        _platformHint = HintLabel(string.Empty);
        _platformBox = AddComboRaw(
            t, "Plattform (platform)", _platformHint, editable: true,
            s => s.Platform, (s, v) => s.Platform = v, PakePlatforms.Runners);

        _platformBox.SelectedIndexChanged += (_, _) => OnPlatformChanged();
        _platformBox.TextChanged += (_, _) => OnPlatformChanged();

        AddText(t, "URL (url)", "Webseite, lokale HTML-Datei oder dist-Ordner", s => s.Url, (s, v) => s.Url = v,
            box => BrowseUrlOrPath(box));

        AddText(t, "App-Name (name)", "Pflichtfeld. Windows/macOS behalten Leerzeichen und Grossschreibung.",
            s => s.Name, (s, v) => s.Name = v);

        AddText(t, "Icon (icon)", "URL oder Datei. Leer = Icon der Webseite wird automatisch geladen.",
            s => s.Icon, (s, v) => s.Icon = v,
            box => BrowseFile(box, "Icon auswaehlen", "Icons|*.ico;*.png;*.icns;*.jpg;*.jpeg|Alle Dateien|*.*"));

        AddSection(t, "Fenster");

        AddNumber(t, "Breite (width)", "Standard 1200 px", 1, 10000, s => s.Width, (s, v) => s.Width = v);
        AddNumber(t, "Hoehe (height)", "Standard 780 px", 1, 10000, s => s.Height, (s, v) => s.Height = v);
        AddNumber(t, "Min. Breite (min_width)", "0 = keine Vorgabe", 0, 10000, s => s.MinWidth, (s, v) => s.MinWidth = v);
        AddNumber(t, "Min. Hoehe (min_height)", "0 = keine Vorgabe", 0, 10000, s => s.MinHeight, (s, v) => s.MinHeight = v);
        AddText(t, "App-Version (app_version)", "SemVer, z. B. 1.0.0", s => s.AppVersion, (s, v) => s.AppVersion = v);

        AddSection(t, "Verhalten");

        AddCheck(t, "Vollbild starten (fullscreen)", "ohne Titelleiste und Fensterrahmen", s => s.Fullscreen, (s, v) => s.Fullscreen = v);
        AddCheck(t, "Titelleiste ausblenden (hide_title_bar)", "nur macOS", s => s.HideTitleBar, (s, v) => s.HideTitleBar = v);
        AddCheck(t, "Neue Fenster erlauben (new_window)", "hilft bei Popup-Anmeldungen und Tabs", s => s.NewWindow, (s, v) => s.NewWindow = v);
        AddCheck(t, "Universal Binary (multi_arch)", "nur macOS: Intel + Apple Silicon", s => s.MultiArch, (s, v) => s.MultiArch = v);

        AddSection(t, "Zielformat");

        var targetsHint = HintLabel("Windows: x64, arm64 · Linux: deb, appimage, rpm, zst · macOS: dmg, app, intel, apple, universal");
        _targetsBox = AddComboRaw(t, "Zielformat (targets)", targetsHint, editable: true,
            s => s.Targets, (s, v) => s.Targets = v);

        return page;
    }

    private Panel BuildWindowTab()
    {
        var (page, t) = CreatePage("Darstellung und Verhalten des spaeteren Programmfensters.");

        AddSection(t, "Fenster");

        AddNumber(t, "Zoom (zoom)", "50–200 %, 0 = Standard (100 %)", 0, 200, s => s.Zoom, (s, v) => s.Zoom = v);
        AddText(t, "Fenstertitel (title)", "leer = App-Name", s => s.Title, (s, v) => s.Title = v);
        AddCheck(t, "Beim Start maximiert (maximize)", "", s => s.Maximize, (s, v) => s.Maximize = v);
        AddCheck(t, "Immer im Vordergrund (always_on_top)", "", s => s.AlwaysOnTop, (s, v) => s.AlwaysOnTop = v);
        AddCheck(t, "Ohne Fensterrahmen (hide_window_decorations)", "Windows/Linux: eigener Ziehbereich, F11 = Vollbild",
            s => s.HideWindowDecorations, (s, v) => s.HideWindowDecorations = v);
        AddCheck(t, "Dunkelmodus erzwingen (dark_mode)", "macOS, Windows und Linux", s => s.DarkMode, (s, v) => s.DarkMode = v);
        AddCheck(t, "Web-Shortcuts deaktivieren (disabled_web_shortcuts)", "", s => s.DisabledWebShortcuts, (s, v) => s.DisabledWebShortcuts = v);
        AddText(t, "Aktivierungs-Shortcut (activation_shortcut)", "z. B. CmdOrControl+Shift+P",
            s => s.ActivationShortcut, (s, v) => s.ActivationShortcut = v);

        AddEnumRow(t, "Schliessen (hide_on_close)",
            "Standard: macOS versteckt, Windows/Linux beendet",
            [("Plattform-Standard", HideOnCloseMode.Default), ("Fenster verstecken", HideOnCloseMode.Yes), ("App beenden", HideOnCloseMode.No)],
            s => s.HideOnClose, (s, v) => s.HideOnClose = v);

        AddSection(t, "Sitzung und Speicher");

        AddCheck(t, "Beim Start immer die Start-URL oeffnen (no_restore_last_url)", "Cookies und Logins bleiben erhalten",
            s => s.NoRestoreLastUrl, (s, v) => s.NoRestoreLastUrl = v);
        AddCheck(t, "Inkognito-Modus (incognito)", "keine Cookies, kein Verlauf, kein lokaler Speicher",
            s => s.Incognito, (s, v) => s.Incognito = v);
        AddCheck(t, "Passwort-Speichern erlauben (password_autosave)", "nur Windows, wirkungslos bei Inkognito",
            s => s.PasswordAutosave, (s, v) => s.PasswordAutosave = v);

        return page;
    }

    private Panel BuildFeaturesTab()
    {
        var (page, t) = CreatePage("System-Tray, mehrere Fenster und Web-Funktionen.");

        AddSection(t, "System-Tray");

        AddCheck(t, "Tray-Icon anzeigen (show_system_tray)", "", s => s.ShowSystemTray, (s, v) => s.ShowSystemTray = v);
        AddText(t, "Tray-Icon (system_tray_icon)", "ICO/PNG, 32–256 px", s => s.SystemTrayIcon, (s, v) => s.SystemTrayIcon = v,
            box => BrowseFile(box, "Tray-Icon auswaehlen", "Bilder|*.ico;*.png|Alle Dateien|*.*"));
        AddCheck(t, "Minimiert im Tray starten (start_to_tray)", "benoetigt Tray-Icon", s => s.StartToTray, (s, v) => s.StartToTray = v);

        AddSection(t, "Fenster und Instanzen");

        AddCheck(t, "Mehrere Instanzen erlauben (multi_instance)", "mehrere Prozesse der App", s => s.MultiInstance, (s, v) => s.MultiInstance = v);
        AddCheck(t, "Mehrere Fenster erlauben (multi_window)", "ein Prozess, mehrere Fenster", s => s.MultiWindow, (s, v) => s.MultiWindow = v);
        AddCheck(t, "Seite durchsuchen (enable_find)", "Strg+F / Strg+G", s => s.EnableFind, (s, v) => s.EnableFind = v);
        AddCheck(t, "Drag & Drop (enable_drag_drop)", "", s => s.EnableDragDrop, (s, v) => s.EnableDragDrop = v);
        AddCheck(t, "WebAssembly / COOP+COEP (wasm)", "fuer Flutter Web und sqlite3.wasm", s => s.Wasm, (s, v) => s.Wasm = v);

        return page;
    }

    private Panel BuildNetworkTab()
    {
        var (page, t) = CreatePage("Proxy, User-Agent, Navigation und eingebettete Dateien.");

        AddSection(t, "Netzwerk");

        AddText(t, "Proxy (proxy_url)", "http://, https:// oder socks5://", s => s.ProxyUrl, (s, v) => s.ProxyUrl = v);
        AddText(t, "User-Agent (user_agent)", "leer = Standard der WebView", s => s.UserAgent, (s, v) => s.UserAgent = v);
        AddCheck(t, "Zertifikatsfehler ignorieren (ignore_certificate_errors)", "nur fuer Intranet/Dev-Server",
            s => s.IgnoreCertificateErrors, (s, v) => s.IgnoreCertificateErrors = v);
        AddCheck(t, "HTTP-Basic-Auth abfragen (basic_auth)", "nur macOS", s => s.BasicAuth, (s, v) => s.BasicAuth = v);

        AddSection(t, "Navigation");

        AddCheck(t, "Alle Links intern oeffnen (force_internal_navigation)", "", s => s.ForceInternalNavigation, (s, v) => s.ForceInternalNavigation = v);
        AddText(t, "Vertrauenswuerdige Domains (safe_domain)", "kommagetrennt, z. B. slack.com,okta.com",
            s => s.SafeDomain, (s, v) => s.SafeDomain = v);
        AddText(t, "Interne URLs per Regex (internal_url_regex)", "hat Vorrang vor safe_domain",
            s => s.InternalUrlRegex, (s, v) => s.InternalUrlRegex = v);

        AddSection(t, "Dateien");

        AddText(t, "CSS/JS einbetten (inject)", "kommagetrennte Dateipfade", s => s.Inject, (s, v) => s.Inject = v,
            box => BrowseFiles(box, "CSS/JS-Dateien auswaehlen", "Web-Dateien|*.css;*.js|Alle Dateien|*.*"));
        AddText(t, "Download-Ordner (download_dir)", "absoluter Pfad oder ~/...", s => s.DownloadDir, (s, v) => s.DownloadDir = v,
            box => BrowseFolder(box, "Download-Ordner auswaehlen"));
        AddCheck(t, "Lokale Dateien mitkopieren (use_local_file)", "nur bei einer einzelnen HTML-Datei relevant",
            s => s.UseLocalFile, (s, v) => s.UseLocalFile = v);

        AddSection(t, "Identitaet");

        AddText(t, "Bundle-Identifier (identifier)", "leer = com.pake.a<hash>", s => s.Identifier, (s, v) => s.Identifier = v);

        return page;
    }

    private Panel BuildBuildTab()
    {
        var (page, t) = CreatePage("Artefakte und Details des lokalen Builds.");

        AddSection(t, "Windows-Build");

        AddComboRaw(t, "Rust-Toolchain (windows_toolchain)",
            HintLabel("msvc = Visual Studio Build Tools, gnu = MinGW/MSYS2"),
            editable: true, s => s.WindowsToolchain, (s, v) => s.WindowsToolchain = v,
            ["", "msvc", "gnu"]);

        AddComboRaw(t, "Installer-Sprache (installer_language)",
            HintLabel("nur Windows-MSIs, z. B. de-DE"),
            editable: true, s => s.InstallerLanguage, (s, v) => s.InstallerLanguage = v,
            ["", "de-DE", "en-US", "fr-FR", "es-ES", "it-IT", "ja-JP", "zh-CN", "zh-TW", "ko-KR", "pt-BR", "ru-RU"]);

        AddSection(t, "Artefakte");

        AddCheck(t, "Rohe EXE zusaetzlich behalten (keep_binary)", "", s => s.KeepBinary, (s, v) => s.KeepBinary = v);
        AddCheck(t, "Nur Binaerdatei, kein Paket (no_bundle)", "nur Linux", s => s.NoBundle, (s, v) => s.NoBundle = v);
        AddCheck(t, "Schnellbuild ohne Installer (iterative_build)", "nur macOS: .app ohne DMG", s => s.IterativeBuild, (s, v) => s.IterativeBuild = v);
        AddCheck(t, "Direkt nach /Applications installieren (install)", "nur macOS", s => s.Install, (s, v) => s.Install = v);
        AddCheck(t, "Kamera-Zugriff (camera)", "nur macOS", s => s.Camera, (s, v) => s.Camera = v);
        AddCheck(t, "Mikrofon-Zugriff (microphone)", "nur macOS", s => s.Microphone, (s, v) => s.Microphone = v);

        AddSection(t, "Diagnose");

        AddCheck(t, "Debug-Modus (debug)", "Entwicklertools und ausfuehrliche Ausgabe", s => s.Debug, (s, v) => s.Debug = v);
        AddText(t, "Zusaetzliche Argumente", "werden unveraendert angehaengt, z. B. --verbose",
            s => s.ExtraArgs, (s, v) => s.ExtraArgs = v);

        return page;
    }

    private void OnPlatformChanged()
    {
        if (_loading || _platformHint is null || _targetsBox is null)
            return;

        var platform = _platformBox.Text.Trim();
        if (platform.Length == 0)
            return;

        _platformHint.Text = PakePlatforms.LocalBuildHint(platform);
        RefreshTargets(platform);

        var current = _targetsBox.Text.Trim();
        var isOldDefault = PakePlatforms.Runners.Any(
            r => string.Equals(PakePlatforms.DefaultTarget(r), current, StringComparison.OrdinalIgnoreCase));

        if (current.Length == 0 || isOldDefault)
            _targetsBox.Text = PakePlatforms.DefaultTarget(platform);
    }

    private void RefreshTargets(string platform)
    {
        var items = PakePlatforms.TargetsFor(platform);
        if (_targetsBox.Items.Count == items.Length
            && items.All(i => _targetsBox.Items.Contains(i)))
            return;

        var text = _targetsBox.Text;
        _targetsBox.Items.Clear();
        _targetsBox.Items.AddRange(items);
        _targetsBox.Text = text;
    }

    // ------------------------------------------------------------------
    // Zeilen-Fabriken
    // ------------------------------------------------------------------

    private static int AddRow(TableLayoutPanel table)
    {
        var row = table.RowCount;
        table.RowCount = row + 1;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        return row;
    }

    private static void AddSection(TableLayoutPanel table, string title)
    {
        var row = AddRow(table);
        var label = new Label
        {
            Text = title,
            AutoSize = true,
            Font = Theme.BodyBold,
            ForeColor = Theme.Accent,
            Margin = new Padding(0, 16, 0, 6),
        };
        table.Controls.Add(label, 0, row);
        table.SetColumnSpan(label, 3);
    }

    private static Label RowLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = Theme.Body,
        ForeColor = Theme.Text,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 7, 10, 4),
    };

    private static Label HintLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        MaximumSize = new Size(214, 0),
        Font = Theme.Small,
        ForeColor = Theme.Muted,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(2, 7, 0, 4),
    };

    private TextBox AddText(
        TableLayoutPanel table,
        string label,
        string hint,
        Func<PakeSettings, string> get,
        Action<PakeSettings, string> set,
        Action<TextBox>? browse = null)
    {
        var row = AddRow(table);
        table.Controls.Add(RowLabel(label), 0, row);

        var box = new TextBox
        {
            Font = Theme.Body,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 8, 4),
        };
        Theme.StyleTextBox(box);
        box.TextChanged += (_, _) => RaiseChanged();

        if (browse is null)
        {
            table.Controls.Add(box, 1, row);
        }
        else
        {
            var host = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
                Margin = new Padding(0),
                BackColor = Theme.Surface,
            };
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            host.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));
            host.MinimumSize = new Size(0, 30);

            var button = Theme.BrowseButton("Durchsuchen");
            button.Margin = new Padding(4, 4, 8, 4);
            button.Dock = DockStyle.Fill;
            button.Click += (_, _) => browse(box);

            host.Controls.Add(box, 0, 0);
            host.Controls.Add(button, 1, 0);
            table.Controls.Add(host, 1, row);
        }

        table.Controls.Add(HintLabel(hint), 2, row);

        _apply.Add(s => set(s, box.Text.Trim()));
        _load.Add(s => box.Text = get(s) ?? string.Empty);

        return box;
    }

    private NumericUpDown AddNumber(
        TableLayoutPanel table,
        string label,
        string hint,
        int min,
        int max,
        Func<PakeSettings, int> get,
        Action<PakeSettings, int> set)
    {
        var row = AddRow(table);
        table.Controls.Add(RowLabel(label), 0, row);

        var spin = new NumericUpDown
        {
            Font = Theme.Body,
            Minimum = min,
            Maximum = max,
            Width = 132,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 8, 4),
            TextAlign = HorizontalAlignment.Right,
        };
        Theme.StyleSpin(spin);
        spin.ValueChanged += (_, _) => RaiseChanged();

        table.Controls.Add(spin, 1, row);
        table.Controls.Add(HintLabel(hint), 2, row);

        _apply.Add(s => set(s, (int)spin.Value));
        _load.Add(s =>
        {
            var value = Math.Clamp(get(s), min, max);
            spin.Value = value;
        });

        return spin;
    }

    private CheckBox AddCheck(
        TableLayoutPanel table,
        string label,
        string hint,
        Func<PakeSettings, bool> get,
        Action<PakeSettings, bool> set)
    {
        var row = AddRow(table);

        var check = new CheckBox
        {
            Text = label,
            AutoSize = true,
            Font = Theme.Body,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 6, 8, 4),
        };
        Theme.StyleCheck(check);
        check.CheckedChanged += (_, _) => RaiseChanged();

        table.Controls.Add(check, 0, row);
        table.SetColumnSpan(check, 2);
        table.Controls.Add(HintLabel(hint), 2, row);

        _apply.Add(s => set(s, check.Checked));
        _load.Add(s => check.Checked = get(s));

        return check;
    }

    private ComboBox AddComboRaw(
        TableLayoutPanel table,
        string label,
        Label hintLabel,
        bool editable,
        Func<PakeSettings, string> get,
        Action<PakeSettings, string> set,
        string[]? items = null)
    {
        var row = AddRow(table);
        table.Controls.Add(RowLabel(label), 0, row);

        var combo = new ComboBox
        {
            Font = Theme.Body,
            Width = 300,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 8, 4),
            DropDownStyle = editable ? ComboBoxStyle.DropDown : ComboBoxStyle.DropDownList,
        };
        Theme.StyleCombo(combo);
        if (items is not null)
            combo.Items.AddRange(items);
        combo.TextChanged += (_, _) => RaiseChanged();

        table.Controls.Add(combo, 1, row);
        table.Controls.Add(hintLabel, 2, row);

        _apply.Add(s => set(s, combo.Text.Trim()));
        _load.Add(s =>
        {
            var value = get(s) ?? string.Empty;
            if (combo.DropDownStyle == ComboBoxStyle.DropDownList)
            {
                if (combo.Items.Contains(value))
                    combo.SelectedItem = value;
                else if (combo.Items.Count > 0)
                    combo.SelectedIndex = 0;
            }
            else
            {
                combo.Text = value;
            }
        });

        return combo;
    }

    private void AddEnumRow<T>(
        TableLayoutPanel table,
        string label,
        string hint,
        (string Text, T Value)[] options,
        Func<PakeSettings, T> get,
        Action<PakeSettings, T> set)
        where T : struct
    {
        var row = AddRow(table);
        table.Controls.Add(RowLabel(label), 0, row);

        var combo = new ComboBox
        {
            Font = Theme.Body,
            Width = 220,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 4, 8, 4),
            DropDownStyle = ComboBoxStyle.DropDownList,
        };
        foreach (var option in options)
            combo.Items.Add(option.Text);

        combo.SelectedIndexChanged += (_, _) => RaiseChanged();

        table.Controls.Add(combo, 1, row);
        table.Controls.Add(HintLabel(hint), 2, row);

        _apply.Add(s =>
        {
            var index = Math.Clamp(combo.SelectedIndex, 0, options.Length - 1);
            set(s, options[index].Value);
        });
        _load.Add(s =>
        {
            var current = get(s);
            var index = Array.FindIndex(options, o => EqualityComparer<T>.Default.Equals(o.Value, current));
            combo.SelectedIndex = index < 0 ? 0 : index;
        });
    }

    // ------------------------------------------------------------------
    // Dialoge
    // ------------------------------------------------------------------

    private void BrowseFile(TextBox box, string title, string filter)
    {
        using var dialog = new OpenFileDialog { Title = title, Filter = filter, CheckFileExists = true };
        var current = box.Text.Trim();
        if (current.Length > 0 && File.Exists(current))
            dialog.FileName = current;

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            box.Text = dialog.FileName;
    }

    private void BrowseFiles(TextBox box, string title, string filter)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            Multiselect = true,
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            box.Text = string.Join(",", dialog.FileNames);
    }

    private void BrowseFolder(TextBox box, string title)
    {
        using var dialog = new FolderBrowserDialog { Description = title, UseDescriptionForTitle = true };
        var current = box.Text.Trim();
        if (current.Length > 0 && Directory.Exists(current))
            dialog.SelectedPath = current;

        if (dialog.ShowDialog(FindForm()) == DialogResult.OK)
            box.Text = dialog.SelectedPath;
    }

    private void BrowseUrlOrPath(TextBox box)
    {
        var menu = new ContextMenuStrip();
        Theme.StyleContextMenu(menu);
        menu.Items.Add("Web-Adresse eingeben", null, (_, _) => box.Focus());
        menu.Items.Add("Lokale HTML-Datei waehlen...", null, (_, _) =>
            BrowseFile(box, "HTML-Datei auswaehlen", "Webseiten|*.html;*.htm|Alle Dateien|*.*"));
        menu.Items.Add("Ordner mit index.html waehlen...", null, (_, _) =>
            BrowseFolder(box, "dist-Ordner auswaehlen"));

        menu.Show(box, new Point(0, box.Height));
    }
}
