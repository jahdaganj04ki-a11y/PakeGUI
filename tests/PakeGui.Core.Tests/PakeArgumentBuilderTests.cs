using PakeGui.Core;
using Xunit;

namespace PakeGui.Core.Tests;

public class PakeArgumentBuilderTests
{
    private static PakeSettings Base() => new()
    {
        Url = "https://example.com",
        Name = "Example",
    };

    private static string ValueOf(IReadOnlyList<string> args, string flag)
    {
        var index = args.ToList().IndexOf(flag);
        Assert.True(index >= 0, $"Flag {flag} fehlt in: {string.Join(" ", args)}");
        Assert.True(index + 1 < args.Count, $"Flag {flag} hat keinen Wert");
        return args[index + 1];
    }

    [Fact]
    public void Defaults_mirror_workflow_defaults()
    {
        var s = new PakeSettings();

        Assert.Equal("windows-latest", s.Platform);
        Assert.Equal(1200, s.Width);
        Assert.Equal(780, s.Height);
        Assert.Equal("1.0.0", s.AppVersion);
        Assert.False(s.Fullscreen);
        Assert.False(s.HideTitleBar);
        Assert.False(s.NewWindow);
        Assert.False(s.MultiArch);
        Assert.Equal(0, s.MinWidth);
        Assert.Equal(0, s.MinHeight);
    }

    [Fact]
    public void Local_platform_name_maps_to_local_targets()
    {
        var platform = PakePlatforms.LocalPlatformName;

        Assert.Contains(PakePlatforms.LocalBuildTargets[0], PakePlatforms.TargetsFor(platform));
        Assert.Equal(PakePlatforms.DefaultTarget(platform), PakePlatforms.LocalBuildTargets[0]);
        Assert.Equal("Lokaler Build moeglich.", PakePlatforms.LocalBuildHint(platform));
    }

    [Theory]
    [InlineData("ubuntu-24.04")]
    [InlineData("ubuntu-24.04 (Linux)")]
    [InlineData("linux")]
    public void Linux_variants_are_recognised(string platform)
    {
        Assert.Equal(PakePlatforms.LinuxRunner, PakePlatforms.Family(platform));
        Assert.Equal("deb", PakePlatforms.DefaultTarget(platform));
    }

    [Theory]
    [InlineData("macos-latest")]
    [InlineData("macOS")]
    [InlineData("darwin")]
    public void Mac_variants_are_recognised(string platform)
    {
        Assert.Equal(PakePlatforms.MacRunner, PakePlatforms.Family(platform));
        Assert.Equal("dmg", PakePlatforms.DefaultTarget(platform));
    }

    [Fact]
    public void Mac_platform_does_not_warn_about_hide_title_bar()
    {
        var s = Base();
        s.Platform = "macOS";
        s.HideTitleBar = true;
        s.Targets = "dmg";

        var built = PakeArgumentBuilder.Build(s);

        Assert.Contains("--hide-title-bar", built.Arguments);
        Assert.DoesNotContain(built.Warnings, w => w.Contains("hide-title-bar"));
    }

    [Fact]
    public void Url_is_first_argument()
    {
        var built = PakeArgumentBuilder.Build(Base());
        Assert.Equal("https://example.com", built.Arguments[0]);
    }

    [Fact]
    public void All_workflow_inputs_are_passed_as_flags()
    {
        var s = Base();
        s.Icon = "https://example.com/icon.png";
        s.Width = 1400;
        s.Height = 900;
        s.MinWidth = 800;
        s.MinHeight = 600;
        s.AppVersion = "2.3.4";
        s.Fullscreen = true;
        s.HideTitleBar = true;
        s.NewWindow = true;
        s.MultiArch = true;
        s.Platform = "macos-latest";
        s.Targets = "dmg,universal";

        var args = PakeArgumentBuilder.Build(s).Arguments;

        Assert.Equal("Example", ValueOf(args, "--name"));
        Assert.Equal("https://example.com/icon.png", ValueOf(args, "--icon"));
        Assert.Equal("1400", ValueOf(args, "--width"));
        Assert.Equal("900", ValueOf(args, "--height"));
        Assert.Equal("800", ValueOf(args, "--min-width"));
        Assert.Equal("600", ValueOf(args, "--min-height"));
        Assert.Equal("2.3.4", ValueOf(args, "--app-version"));
        Assert.Contains("--fullscreen", args);
        Assert.Contains("--hide-title-bar", args);
        Assert.Contains("--new-window", args);
        Assert.Contains("--multi-arch", args);
        Assert.Equal("dmg,universal", ValueOf(args, "--targets"));
    }

    [Fact]
    public void Empty_optional_values_are_omitted()
    {
        var s = Base();
        s.Platform = "windows-latest";
        s.Targets = string.Empty;

        var args = PakeArgumentBuilder.Build(s).Arguments;

        Assert.DoesNotContain("--icon", args);
        Assert.DoesNotContain("--min-width", args);
        Assert.DoesNotContain("--min-height", args);
        Assert.DoesNotContain("--targets", args);
        Assert.DoesNotContain("--fullscreen", args);
    }

    [Fact]
    public void Target_that_does_not_match_platform_is_dropped_with_warning()
    {
        var s = Base();
        s.Platform = "windows-latest";
        s.Targets = "deb";

        var built = PakeArgumentBuilder.Build(s);

        Assert.DoesNotContain("--targets", built.Arguments);
        Assert.Contains(built.Warnings, w => w.Contains("deb"));
    }

    [Fact]
    public void Mixed_targets_keep_only_valid_ones()
    {
        var s = Base();
        s.Platform = "windows-latest";
        s.Targets = "x64, deb";

        var args = PakeArgumentBuilder.Build(s).Arguments;

        Assert.Equal("x64", ValueOf(args, "--targets"));
    }

    [Fact]
    public void Extended_options_map_to_cli_flags()
    {
        var s = Base();
        s.Identifier = "com.example.app";
        s.Title = "Mein Titel";
        s.Zoom = 120;
        s.Maximize = true;
        s.AlwaysOnTop = true;
        s.DarkMode = true;
        s.HideWindowDecorations = true;
        s.DisabledWebShortcuts = true;
        s.ActivationShortcut = "CmdOrControl+Shift+P";
        s.ShowSystemTray = true;
        s.SystemTrayIcon = "tray.ico";
        s.StartToTray = true;
        s.HideOnClose = HideOnCloseMode.No;
        s.NoRestoreLastUrl = true;
        s.Incognito = true;
        s.PasswordAutosave = true;
        s.Wasm = true;
        s.EnableDragDrop = true;
        s.MultiInstance = true;
        s.MultiWindow = true;
        s.EnableFind = true;
        s.ForceInternalNavigation = true;
        s.InternalUrlRegex = "^https://example\\.com";
        s.UserAgent = "TestAgent";
        s.ProxyUrl = "socks5://127.0.0.1:7891";
        s.IgnoreCertificateErrors = true;
        s.Inject = "a.css, b.js";
        s.DownloadDir = "C:\\Downloads\\App";
        s.UseLocalFile = true;
        s.Debug = true;
        s.KeepBinary = true;
        s.InstallerLanguage = "de-DE";
        s.WindowsToolchain = "gnu";

        var args = PakeArgumentBuilder.Build(s).Arguments;

        Assert.Equal("com.example.app", ValueOf(args, "--identifier"));
        Assert.Equal("Mein Titel", ValueOf(args, "--title"));
        Assert.Equal("120", ValueOf(args, "--zoom"));
        Assert.Equal("tray.ico", ValueOf(args, "--system-tray-icon"));
        Assert.Equal("false", ValueOf(args, "--hide-on-close"));
        Assert.Equal("a.css,b.js", ValueOf(args, "--inject"));
        Assert.Equal("C:\\Downloads\\App", ValueOf(args, "--download-dir"));
        Assert.Equal("de-DE", ValueOf(args, "--installer-language"));
        Assert.Equal("gnu", ValueOf(args, "--windows-toolchain"));
        Assert.Equal("^https://example\\.com", ValueOf(args, "--internal-url-regex"));
        Assert.Contains("--maximize", args);
        Assert.Contains("--always-on-top", args);
        Assert.Contains("--dark-mode", args);
        Assert.Contains("--hide-window-decorations", args);
        Assert.Contains("--disabled-web-shortcuts", args);
        Assert.Contains("--show-system-tray", args);
        Assert.Contains("--start-to-tray", args);
        Assert.Contains("--no-restore-last-url", args);
        Assert.Contains("--incognito", args);
        Assert.Contains("--password-autosave", args);
        Assert.Contains("--wasm", args);
        Assert.Contains("--enable-drag-drop", args);
        Assert.Contains("--multi-instance", args);
        Assert.Contains("--multi-window", args);
        Assert.Contains("--enable-find", args);
        Assert.Contains("--force-internal-navigation", args);
        Assert.Contains("--ignore-certificate-errors", args);
        Assert.Contains("--use-local-file", args);
        Assert.Contains("--debug", args);
        Assert.Contains("--keep-binary", args);
        Assert.Equal("socks5://127.0.0.1:7891", ValueOf(args, "--proxy-url"));
        Assert.Equal("TestAgent", ValueOf(args, "--user-agent"));
    }

    [Fact]
    public void HideOnClose_true_is_written_explicitly()
    {
        var s = Base();
        s.HideOnClose = HideOnCloseMode.Yes;

        var args = PakeArgumentBuilder.Build(s).Arguments;
        Assert.Equal("true", ValueOf(args, "--hide-on-close"));
    }

    [Fact]
    public void Extra_arguments_are_appended_and_split()
    {
        var s = Base();
        s.ExtraArgs = "--verbose --flag \"mit leerzeichen\"";

        var args = PakeArgumentBuilder.Build(s).Arguments;

        Assert.Equal("--verbose", args[^3]);
        Assert.Equal("--flag", args[^2]);
        Assert.Equal("mit leerzeichen", args[^1]);
    }

    [Fact]
    public void Start_to_tray_without_tray_is_reported()
    {
        var s = Base();
        s.StartToTray = true;

        var built = PakeArgumentBuilder.Build(s);

        Assert.DoesNotContain("--start-to-tray", built.Arguments);
        Assert.Contains(built.Warnings, w => w.Contains("--start-to-tray"));
    }

    [Fact]
    public void Command_line_quotes_arguments_with_spaces()
    {
        var s = Base();
        s.Name = "Mein App";
        s.OutputDirectory = string.Empty;
        s.RunnerMode = PakeRunnerMode.Custom;
        s.CustomRunnerPath = "pake --json";

        var command = PakeCommandLineBuilder.Build(s);

        Assert.Equal("pake", command.Executable);
        Assert.Equal("--json", command.Arguments[0]);
        Assert.Contains("\"Mein App\"", command.Display);
    }

    [Fact]
    public void Npx_runner_prefixes_package()
    {
        var s = Base();
        s.RunnerMode = PakeRunnerMode.Npx;

        var command = PakeCommandLineBuilder.Build(s);

        Assert.Equal("npx", command.Executable);
        Assert.Equal("--yes", command.Arguments[0]);
        Assert.Equal(PakeCommandLineBuilder.NpxPackage, command.Arguments[1]);
    }

    [Fact]
    public void Build_script_contains_working_directory_and_command()
    {
        var command = new PakeCommandLine("npx", ["--yes", "pake-cli@latest", "https://example.com"], []);
        var dir = Path.Combine(Path.GetTempPath(), "pake test");

        var script = PakeCommandLineBuilder.CreateScript(command, dir);

        Assert.Contains("npx", script);
        Assert.Contains("https://example.com", script);
        Assert.Contains("pake test", script);
        if (OperatingSystem.IsWindows())
        {
            Assert.StartsWith("@echo off", script);
            Assert.Contains("cd /d", script);
        }
        else
        {
            Assert.StartsWith("#!/bin/sh", script);
            Assert.Contains("cd '", script);
        }
    }

    [Fact]
    public void Percent_signs_are_escaped_for_cmd()
    {
        var quoted = CommandLineJoin.QuoteForCmd("100% ready");
        Assert.Equal("\"100%% ready\"", quoted);
    }

    [Fact]
    public void Splitter_handles_quotes_and_whitespace()
    {
        var parts = CommandLineSplitter.Split("  --a \"b c\" 'd e'  ");
        Assert.Equal(["--a", "b c", "d e"], parts);
    }

    [Fact]
    public void Splitter_returns_empty_for_blank_input()
    {
        Assert.Empty(CommandLineSplitter.Split("   "));
        Assert.Empty(CommandLineSplitter.Split(null));
    }
}
