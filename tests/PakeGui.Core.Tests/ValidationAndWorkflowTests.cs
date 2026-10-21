using PakeGui.Core;
using Xunit;

namespace PakeGui.Core.Tests;

public class ValidationTests
{
    private static PakeSettings Valid() => new()
    {
        Url = "https://example.com",
        Name = "Example",
        Platform = PakePlatforms.LocalPlatformName,
    };

    [Fact]
    public void Valid_settings_produce_no_issues()
    {
        var issues = PakeValidation.Validate(Valid());
        Assert.DoesNotContain(issues, i => i.IsError);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Empty_url_is_an_error(string url)
    {
        var s = Valid();
        s.Url = url;
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "url" && i.IsError);
    }

    [Fact]
    public void Empty_name_is_an_error()
    {
        var s = Valid();
        s.Name = " ";
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "name" && i.IsError);
    }

    [Fact]
    public void Invalid_file_name_characters_are_rejected()
    {
        var s = Valid();
        s.Name = "Bad:Name?";
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "name" && i.IsError);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("abc")]
    [InlineData("1.0.0.0")]
    public void Bad_app_version_is_rejected(string version)
    {
        var s = Valid();
        s.AppVersion = version;
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "appVersion" && i.IsError);
    }

    [Theory]
    [InlineData("1.0.0")]
    [InlineData("2.3.4-beta.1")]
    [InlineData("10.20.30+build5")]
    public void Semver_like_versions_pass(string version)
    {
        var s = Valid();
        s.AppVersion = version;
        Assert.DoesNotContain(PakeValidation.Validate(s), i => i.Field == "appVersion" && i.IsError);
    }

    [Fact]
    public void Zoom_out_of_range_is_rejected()
    {
        var s = Valid();
        s.Zoom = 300;
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "zoom" && i.IsError);
    }

    [Fact]
    public void Broken_regex_is_rejected()
    {
        var s = Valid();
        s.InternalUrlRegex = "([";
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "internalUrlRegex" && i.IsError);
    }

    [Fact]
    public void Relative_download_directory_is_rejected()
    {
        var s = Valid();
        s.DownloadDir = "relative/path";
        Assert.Contains(PakeValidation.Validate(s), i => i.Field == "downloadDir" && i.IsError);
    }

    [Fact]
    public void Tilde_download_directory_is_allowed()
    {
        var s = Valid();
        s.DownloadDir = "~/Downloads/App";
        Assert.DoesNotContain(PakeValidation.Validate(s), i => i.Field == "downloadDir" && i.IsError);
    }

    [Fact]
    public void Icon_that_does_not_exist_is_a_warning()
    {
        var s = Valid();
        s.Icon = "does-not-exist.ico";
        var issue = Assert.Single(PakeValidation.Validate(s), i => i.Field == "icon");
        Assert.False(issue.IsError);
    }

    [Fact]
    public void Other_platform_is_a_warning_not_an_error()
    {
        var s = Valid();
        s.Platform = "macos-latest";
        var issue = Assert.Single(PakeValidation.Validate(s), i => i.Field == "platform");
        Assert.False(issue.IsError);
    }

    [Fact]
    public void Local_path_url_is_accepted()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pake-validation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var s = Valid();
            s.Url = dir;
            Assert.DoesNotContain(PakeValidation.Validate(s), i => i.Field == "url" && i.IsError);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}

public class WorkflowDispatchTests
{
    private static PakeSettings Settings() => new()
    {
        Platform = "ubuntu-24.04",
        Url = "https://example.com",
        Name = "Meine App",
        Icon = "https://example.com/icon.icns",
        Width = 1400,
        Height = 900,
        MinWidth = 900,
        MinHeight = 600,
        AppVersion = "2.0.0",
        Fullscreen = true,
        HideTitleBar = true,
        NewWindow = true,
        MultiArch = true,
        Targets = "deb,appimage",
    };

    [Fact]
    public void Inputs_use_workflow_names_and_types()
    {
        var inputs = WorkflowDispatch.ToInputs(Settings());

        Assert.Equal(
            ["platform", "url", "name", "icon", "width", "height", "min_width", "min_height",
             "app_version", "fullscreen", "hide_title_bar", "new_window", "multi_arch", "targets"],
            inputs.Keys.ToArray());

        Assert.Equal("ubuntu-24.04", inputs["platform"]);
        Assert.Equal("1400", inputs["width"]);
        Assert.Equal("2.0.0", inputs["app_version"]);
        Assert.Equal("true", inputs["fullscreen"]);
        Assert.Equal("true", inputs["new_window"]);
        Assert.Equal("true", inputs["hide_title_bar"]);
        Assert.Equal("deb,appimage", inputs["targets"]);
    }

    [Fact]
    public void Optional_numbers_are_empty_when_unset()
    {
        var s = Settings();
        s.MinWidth = 0;
        s.MinHeight = 0;

        var inputs = WorkflowDispatch.ToInputs(s);

        Assert.Equal(string.Empty, inputs["min_width"]);
        Assert.Equal(string.Empty, inputs["min_height"]);
    }

    [Fact]
    public void Json_payload_contains_ref_and_inputs()
    {
        var json = WorkflowDispatch.ToDispatchJson(Settings(), "main");

        using var doc = System.Text.Json.JsonDocument.Parse(json);
        Assert.Equal("main", doc.RootElement.GetProperty("ref").GetString());
        Assert.Equal("Meine App", doc.RootElement.GetProperty("inputs").GetProperty("name").GetString());
        Assert.Equal("true", doc.RootElement.GetProperty("inputs").GetProperty("multi_arch").GetString());
    }

    [Fact]
    public void Gh_command_covers_every_input()
    {
        var command = WorkflowDispatch.ToGhCommand(Settings());

        Assert.StartsWith("gh workflow run pake-cli.yaml --repo jahdaganj04ki-a11y/PakeGUIWin --ref main", command);
        foreach (var key in WorkflowDispatch.ToInputs(Settings()).Keys)
            Assert.Contains($" -f {key}=", command);

        Assert.Contains("-f name=\"Meine App\"", command);
        Assert.Contains("-f fullscreen=true", command);
    }
}
