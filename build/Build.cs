using CliWrap;
using Nuke.Common;
using Nuke.Common.ChangeLog;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Octokit;
using Serilog;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

class Build : NukeBuild
{
    /// <summary>
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    /// </summary>

    public static int Main() => Execute<Build>(x => x.Compile);

    #region Pulumi

    public string PulumIDirectory { get; private set; }
    public string PulumiProject { get; private set; }

    #endregion

    #region Secrets


    /// <summary>
    /// Sonarqube API Key.
    /// </summary>
    [Parameter]
    [Secret]
    readonly string SonarKey;

    [Parameter]
    [Secret]
    readonly string SNYK_TOKEN;

    [Parameter]
    [Secret]
    readonly string DTrackApiKey;

    /// <summary>
    ///  jkhuj
    /// </summary>
    [Parameter]
    [Secret]
    readonly string CodecovSecret;

    /// <summary>
    ///  ProGet API key.
    /// </summary>
    [Parameter]
    [Secret]
    readonly string ProGetKey;

    /// <summary>
    ///  ll
    /// </summary>
    [Parameter]
    [Secret]
    readonly string GitHubToken;


    /// <summary>
    ///  ProGet server key.
    /// </summary>
    [Parameter]
    [Secret]
    readonly string ReportGeneratorLicense;


    #endregion

    #region Paths

    /// <summary>
    ///     Output of coverlet code  coverage report.
    /// </summary>
    readonly AbsolutePath CoverletOutput = RootDirectory / "Nuke" / "Output" / "Coverlet";

    /// <summary>
    /// NDependOutput folder.
    /// </summary>
    readonly AbsolutePath NukeOut = RootDirectory / "Nuke";

    /// <summary>
    /// </summary>
    readonly AbsolutePath BuildDir = RootDirectory / "Nuke" / "Output" / "Build";

    /// <summary>
    ///     NDependOutput folder.
    /// </summary>
    readonly AbsolutePath NDependOutput = RootDirectory / "Nuke" / "Output" / "NDependOut";

    /// <summary>
    /// Gitguardian config file.
    /// </summary>
    readonly AbsolutePath GgConfig = RootDirectory / "gitguardian.yml";

    /// <summary>
    ///     Dotnet publish output directory
    /// </summary>
    readonly AbsolutePath PublishFolder = RootDirectory / "Nuke" / "Output" / "Publish";

    /// <summary>
    ///     PVS Studio log output folder.
    /// </summary>
    readonly AbsolutePath PvsStudio = RootDirectory / "Nuke" / "Output" / "PVS";

    /// <summary>
    ///     Path to nupkg file from the project
    /// </summary>
    readonly AbsolutePath NupkgPath = RootDirectory / "Nuke" / "Output" / "Nuget";

    /// <summary>
    ///
    /// </summary>
    readonly AbsolutePath ReportOut = RootDirectory / "Nuke" / "Output" / "Coverlet" / "Report";

    /// <summary>
    ///     Output directory of the SBOM file from CycloneDX
    /// </summary>
    readonly AbsolutePath Sbom = RootDirectory / "Nuke" / "Output" / "SBOM";


    /// <summary>
    /// Filename of changelog file.
    /// </summary>
    string ChangeLogFile => RootDirectory / "changelog.md";

    /// <summary>
    /// </summary>

    AbsolutePath DocFxLibrary => RootDirectory / "docfx_project";

    /// <summary>
    ///     Directory of MSTests project.
    /// </summary>
    AbsolutePath TestsDirectory => RootDirectory.GlobDirectories("*.Tests").Single();

    /// <summary>
    /// Target path.
    /// </summary>
    readonly AbsolutePath TargetPath = RootDirectory / "Nuke" / "Output" / "Coverlet" / "Report";


    #endregion

    /// <summary>
    ///     Git repository object.
    /// </summary>
    [GitRepository]
    readonly GitRepository Repository;

    /// <summary>
    ///     Nerdbank gitversioning tool.
    /// </summary>
    [NerdbankGitVersioning]
    readonly NerdbankGitVersioning NerdbankVersioning;
    
    #region Tools

    //    /// <summary>
    //    /// Auto change log cmd for changelog creation.
    //    /// </summary>
    [PathVariable("auto-changelog")] readonly Tool AutoChangelogTool;

    /// <summary>
    ///     Dotnet-sonarscanner cli tool.
    /// </summary>
    [PathVariable("dotnet-sonarscanner")]
    readonly Tool SonarscannerTool;

    //    /// <summary>
    //    /// TrojanSource Finder.
    //    /// </summary>
    [PathVariable("tsfinder")] readonly Tool TsFinderTool;

    //    /// <summary>
    //    /// NDepend Console exe.
    //    /// </summary>
    [PathVariable(@"NDepend.Console.exe")] readonly Tool NDependConsoleTool;

    /// <summary>
    ///
    /// </summary>
    [PathVariable("pwsh")] readonly Tool Pwsh;

    //    /// <summary>
    //    /// PVS Studio Cmd.
    //    /// </summary>
    [PathVariable(@"PVS-Studio_Cmd.exe")] readonly Tool PvsStudioTool;

    //    /// <summary>
    //    /// PlogConverter tool from PVS-Studio.
    //    /// </summary>
    [PathVariable(@"PlogConverter.exe")] readonly Tool PlogConverter;

    //    /// <summary>
    //    /// Dotnet Reactor Console exe.
    //    /// </summary>
    [PathVariable(@"dotNET_Reactor.Console.exe")] readonly Tool Eziriz;

    //    /// <summary>
    //    /// Go cli.
    //    /// </summary>
    [PathVariable(@"go.exe")] readonly Tool Go;

    //    /// <summary>
    //    /// DependencyTrack-audit CLI tool.
    //    /// </summary>
    [PathVariable(@"dtrack-audit")] readonly Tool DTrackAudit;

    /// <summary>
    ///     Qodana CLI.
    /// </summary>
    [PathVariable("qodana")]
    readonly Tool Qodana;

    // <summary>
    /// <summary>
    ///
    /// </summary>
    /// DocFX CLI.
    /// </summary>
    [PathVariable("docfx")]
    readonly Tool DocFx;


    /// <summary>
    ///     dotnet cli.
    /// </summary>
    [PathVariable("dotnet")]
    readonly Tool DotNet;

    /// <summary>
    ///     Dotnet-format dotnet tool.
    /// </summary>
    [PathVariable("dotnet-format")]
    readonly Tool DotnetFormatTool;

    /// <summary>
    ///     GGShield CLI for detecting secrets.
    /// </summary>
    [PathVariable("ggshield")]
    readonly Tool GgShield;

    /// <summary>
    ///     GitHub cli.
    /// </summary>
    [PathVariable("gh")]
    readonly Tool GitHubCli;

    /// <summary>
    /// ReportGenerator tool.
    /// </summary>
    [PathVariable("reportgenerator")]
    readonly Tool ReportGenerator;

    //    /// <summary>
    //    /// Codecov CLI.
    //    /// </summary>
    [PathVariable("codecov")] readonly Tool Codecov;


    // <summary>
    // Snyk cli.
    // </summary>
    [PathVariable("snyk")] readonly Tool SnykTool;


    /// <summary>
    ///     Git cli.
    /// </summary>
    [PathVariable("nuget")]
    readonly Tool NugetCli;


    #endregion

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    /// <summary>
    /// Visual Studio solution object.
    /// </summary>
    [Solution]
    readonly Solution Sln;

    /// <summary>
    ///     Set output paths.
    /// </summary>
    Target SetVariables => _ => _
    .Executes(() =>
    {
        PulumIDirectory = Sln.GetProject(ProjectName).Directory.ToString();
        PulumiProject = Sln.GetProject(ProjectName).Path.ToString();

        Log.Information(PulumIDirectory);
        Log.Information(PulumiProject);
    });

    /// <summary>
    ///     Sets variable values for our build.
    /// </summary>
    Target SetPaths => _ => _
        .DependsOn(SetPaths)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            Log.Information(RootDirectory);

            if (NukeBuild.IsServerBuild)
            {
                Directory.CreateDirectory(NukeOut.ToString());
            }

            else if (NukeBuild.IsLocalBuild)
            {
                Directory.CreateDirectory(NupkgPath.ToString());
                Directory.CreateDirectory(PublishFolder.ToString());
                Directory.CreateDirectory(NDependOutput.ToString());
                Directory.CreateDirectory(BuildDir.ToString());
                Directory.CreateDirectory(PvsStudio.ToString());
                Directory.CreateDirectory(CoverletOutput.ToString());
                Directory.CreateDirectory(ReportOut.ToString());
                Directory.CreateDirectory(TargetPath.ToString());
                Directory.CreateDirectory(Sbom.ToString());
            }
        });

    Target SecretScan => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            GgShield($"--config-path {GgConfig} secret scan commit-range HEAD~1");
        });

    /// <summary>
    /// Creates documentation.
    /// </summary>
    Target CreateDocFx => _ => _
        .DependsOn(GetVariables)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DocFx("build docfx.json", DocFxLibrary);
        });


    /// <summary>
    ///     Versions the project, using Nerdbank (server build).
    /// </summary>
    Target Version => _ => _
        .DependsOn(CreateDocFx)
        .Description("Versions the project using NerdBank.GitVersioning")
        .AssuredAfterFailure()
        .Executes(async () =>
        {
            if (IsLocalBuild || (IsServerBuild && !Repository.IsOnMainOrMasterBranch()))
            {
                var json = "";

                var stdOutBuffer = new StringBuilder();
                var stdErrBuffer = new StringBuilder();

                var nerdbankVersions = await Cli.Wrap("powershell")
                    .WithArguments(new[] { "nbgv get-version | convertto-json" })
                    .WithStandardOutputPipe(PipeTarget.ToStringBuilder(stdOutBuffer))
                    .WithStandardErrorPipe(PipeTarget.ToStringBuilder(stdErrBuffer))
                    .WithWorkingDirectory(PulumIDirectory)
                    .ExecuteBufferedAsync();

                ExtractVersion(stdOutBuffer, stdErrBuffer);
            }

            // When the code is merged.
            if (IsServerBuild && IsServerBuild)
            {
                var c =
                    new NerdbankGitVersioningCloudSettings();

                c.SetProcessWorkingDirectory(PulumIDirectory);

                NerdbankGitVersioningTasks.NerdbankGitVersioningCloud(c);

                CloudBuildNo = NerdbankVersioning.CloudBuildNumber;
            }
        });


    /// <summary>
    ///     Set changelog file.
    /// </summary>
    Target Changelog => _ => _
        .DependsOn(Version)
        .Description("Creates a changelog of the current commit.")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            if (IsLocalBuild)
                AutoChangelogTool($"-v  {OctopusVersion} -o {ChangeLogFile}",
                    RootDirectory.ToString()); // Use .autochangelog settings in file.
        });

    Target PushToGitHub => _ => _
    .DependsOn(Changelog)
    .Description("Push formatted code and changelog.md to GitHub repo.")
    .AssuredAfterFailure()
    .Executes(async () =>
    {
        if (IsLocalBuild)
        {
            var dbDailyTasks = await Cli.Wrap("powershell")
                .WithArguments(new[] { "Split-Path -Leaf (git remote get-url origin)" })
                .ExecuteBufferedAsync();

            var repoName = dbDailyTasks.StandardOutput.TrimEnd();

            var gitCommand = "git";
            var gitAddArgument = @"add -A";
            var gitCommitArgument = @"commit -m ""chore(ci): checking in changed code from local ci""";
            var gitPushArgument =
                $@"push https://{GitHubToken}@github.com/{Repository.GetGitHubOwner()}/{repoName}";

            Process.Start(gitCommand, gitAddArgument).WaitForExit();
            Process.Start(gitCommand, gitCommitArgument).WaitForExit();
            Process.Start(gitCommand, gitPushArgument).WaitForExit();
        }
    });

    /// <summary>
    ///     Authenticate to Synk service.
    /// </summary>
    Target SnykAuth => _ => _
        .DependsOn(PushToGitHub)
        .Description("Authenticate to Snyk.")
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SnykTool($"auth {SNYK_TOKEN}");
        });

    Target SnykScan => _ => _
        .DependsOn(SnykAuth)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SnykTool($"code test", PulumIDirectory);
        });

    /// <summary>
    ///  Runs dotnet outdated against Nuget packages.
    /// </summary>
    Target RunDotnetOutdated => _ => _
        .DependsOn(SecretScan)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            DotNet($"outdated {PulumIDirectory}");
        });

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });



    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
        });


}
