using GitVersion;
using GitVersion.Helpers;
using GitVersionCore.Tests;
using NUnit.Framework;
using Shouldly;
using System;
using System.IO;

[TestFixture]
public class DefaultConfigFileLocatorTests : TestBase
{
    private const string DefaultRepoPath = @"c:\MyGitRepo";
    private const string DefaultWorkingPath = @"c:\MyGitRepo\Working";

    string repoPath;
    string workingPath;
    IFileSystem fileSystem;
    DefaultConfigFileLocator configFileLocator;

    [SetUp]
    public void Setup()
    {
        fileSystem = new TestFileSystem();
        configFileLocator = new DefaultConfigFileLocator();
        repoPath = DefaultRepoPath;
        workingPath = DefaultWorkingPath;

        ShouldlyConfiguration.ShouldMatchApprovedDefaults.LocateTestMethodUsingAttribute<TestAttribute>();
    }
    
    [TestCase(DefaultRepoPath)]
    [TestCase(DefaultWorkingPath)]
    public void WarnOnExistingGitVersionConfigYamlFile(string path)
    {
        SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.ObsoleteFileName, path);

        var logOutput = string.Empty;
        Action<string> action = info => { logOutput = info; };
        using (Logger.AddLoggersTemporarily(action, action, action, action))
        {
            configFileLocator.Verify(workingPath, repoPath, fileSystem);
        }
        var configFileDeprecatedWarning = string.Format("{0}' is deprecated, use '{1}' instead", DefaultConfigFileLocator.ObsoleteFileName, DefaultConfigFileLocator.DefaultFileName);
        logOutput.Contains(configFileDeprecatedWarning).ShouldBe(true);
    }

    [TestCase(DefaultRepoPath)]
    [TestCase(DefaultWorkingPath)]
    public void WarnOnAmbiguousConfigFilesAtTheSameProjectRootDirectory(string path)
    {
        SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.ObsoleteFileName, path);
        SetupConfigFileContent(string.Empty, DefaultConfigFileLocator.DefaultFileName, path);

        var logOutput = string.Empty;
        Action<string> action = info => { logOutput = info; };
        using (Logger.AddLoggersTemporarily(action, action, action, action))
        {
            configFileLocator.Verify(workingPath, repoPath, fileSystem);
        }

        var configFileDeprecatedWarning = string.Format("Ambiguous config files at '{0}'", path);
        logOutput.Contains(configFileDeprecatedWarning).ShouldBe(true);
    }

    [TestCase(DefaultConfigFileLocator.DefaultFileName, DefaultConfigFileLocator.DefaultFileName)]
    [TestCase(DefaultConfigFileLocator.DefaultFileName, DefaultConfigFileLocator.ObsoleteFileName)]
    [TestCase(DefaultConfigFileLocator.ObsoleteFileName, DefaultConfigFileLocator.DefaultFileName)]
    [TestCase(DefaultConfigFileLocator.ObsoleteFileName, DefaultConfigFileLocator.ObsoleteFileName)]
    public void ThrowsExceptionOnAmbiguousConfigFileLocation(string repoConfigFile, string workingConfigFile)
    {
        var repositoryConfigFilePath = SetupConfigFileContent(string.Empty, repoConfigFile, repoPath);
        var workingDirectoryConfigFilePath = SetupConfigFileContent(string.Empty, workingConfigFile, workingPath);

        WarningException exception = Should.Throw<WarningException>(() => { configFileLocator.Verify(workingPath, repoPath, fileSystem); });

        var expecedMessage = string.Format("Ambiguous config file selection from '{0}' and '{1}'", workingDirectoryConfigFilePath, repositoryConfigFilePath);
        exception.Message.ShouldBe(expecedMessage);
    }

    [Test]
    public void NoWarnOnGitVersionYmlFile()
    {
        SetupConfigFileContent(string.Empty);

        var s = string.Empty;
        Action<string> action = info => { s = info; };
        using (Logger.AddLoggersTemporarily(action, action, action, action))
        {
            ConfigurationProvider.Provide(repoPath, fileSystem, configFileLocator);
        }
        s.Length.ShouldBe(0);
    }

    string SetupConfigFileContent(string text, string fileName = DefaultConfigFileLocator.DefaultFileName)
    {
        return SetupConfigFileContent(text, fileName, repoPath);
    }

    string SetupConfigFileContent(string text, string fileName, string path)
    {
        var fullPath = Path.Combine(path, fileName);
        fileSystem.WriteAllText(fullPath, text);

        return fullPath;
    }

    [Test]
    public void WarnOnObsoleteIsDevelopBranchConfigurationSetting()
    {
        const string text = @"
assembly-versioning-scheme: MajorMinorPatch
branches:
  master:
    tag: beta
    is-develop: true";

        OldConfigurationException exception = Should.Throw<OldConfigurationException>(() =>
        {
            LegacyConfigNotifier.Notify(new StringReader(text));
        });

        const string expectedMessage = @"'is-develop' is deprecated, use 'tracks-release-branches' instead.";
        exception.Message.ShouldContain(expectedMessage);
    }
}
