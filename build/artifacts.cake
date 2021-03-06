public class BuildPackages
{
    public ICollection<BuildPackage> All { get; private set; }
    public ICollection<BuildPackage> Nuget { get; private set; }
    public ICollection<BuildPackage> Chocolatey { get; private set; }

    public static BuildPackages GetPackages(
        DirectoryPath nugetRooPath,
        string semVersion,
        string[] packageIds,
        string[] chocolateyPackageIds)
    {
        var toNugetPackage = BuildPackage(nugetRooPath, semVersion);
        var toChocolateyPackage = BuildPackage(nugetRooPath, semVersion, isChocolateyPackage: true);
        var nugetPackages = packageIds.Select(toNugetPackage).ToArray();
        var chocolateyPackages = chocolateyPackageIds.Select(toChocolateyPackage).ToArray();

        return new BuildPackages {
            All = nugetPackages.Union(chocolateyPackages).ToArray(),
            Nuget = nugetPackages,
            Chocolatey = chocolateyPackages
        };
    }

    private static Func<string, BuildPackage> BuildPackage(
        DirectoryPath nugetRooPath,
        string version,
        bool isChocolateyPackage = false)
    {
        return package => new BuildPackage(
            id: package,
            nuspecPath: string.Concat("./nuspec/", package, ".nuspec"),
            packagePath: nugetRooPath.CombineWithFilePath(string.Concat(package, ".", version, ".nupkg")),
            isChocolateyPackage: isChocolateyPackage);
    }
}

public class BuildPackage
{
    public string Id { get; private set; }
    public FilePath NuspecPath { get; private set; }
    public FilePath PackagePath { get; private set; }
    public bool IsChocolateyPackage { get; private set; }
    public string PackageName { get; private set; }


    public BuildPackage(
        string id,
        FilePath nuspecPath,
        FilePath packagePath,
        bool isChocolateyPackage)
    {
        Id = id;
        NuspecPath = nuspecPath;
        PackagePath = packagePath;
        IsChocolateyPackage = isChocolateyPackage;
        PackageName = PackagePath.GetFilename().ToString();
    }
}

public class BuildArtifacts
{
    public ICollection<BuildArtifact> All { get; private set; }

    public static BuildArtifacts GetArtifacts(FilePath[] artifacts)
    {
        var toBuildArtifact = BuildArtifact("build-artifact");
        var buildArtifacts = artifacts.Select(toBuildArtifact).ToArray();

        return new BuildArtifacts {
            All = buildArtifacts.ToArray(),
        };
    }

    private static Func<FilePath, BuildArtifact> BuildArtifact(string containerName)
    {
        return artifactPath => new BuildArtifact(containerName: containerName, artifactPath: artifactPath);
    }
}

public class BuildArtifact
{
    public string ContainerName { get; private set; }
    public FilePath ArtifactPath { get; private set; }
    public string ArtifactName { get; private set; }

    public BuildArtifact(
        string containerName,
        FilePath artifactPath)
    {
        ContainerName = containerName;
        ArtifactPath = artifactPath.FullPath;
        ArtifactName = ArtifactPath.GetFilename().ToString();
    }
}

public class DockerImages
{
    public ICollection<DockerImage> Images { get; private set; }

    public static DockerImages GetDockerImages(BuildParameters parameters, FilePath[] dockerfiles)
    {
        var toDockerImage = DockerImage();
        var dockerImages = dockerfiles.Select(toDockerImage).ToArray();

        var windowsImages = dockerImages.Where(x => x.OS == "windows").ToArray();
        var linuxImages = dockerImages.Where(x => x.OS == "linux").ToArray();

        var images = parameters.IsDockerForWindows
            ? windowsImages
            : parameters.IsDockerForLinux
                ? linuxImages
                : Array.Empty<DockerImage>();

        return new DockerImages {
            Images = images
        };
    }

    private static Func<FilePath, DockerImage> DockerImage()
    {
        return dockerFile => {
            var segments = dockerFile.Segments.Reverse().ToArray();
            var distro = segments[1];
            var os = segments[2];
            var targetFramework = segments[3];
            return new DockerImage(os: os, distro: distro, targetFramework: targetFramework);
        };
    }
}

public class DockerImage
{
    public string OS { get; private set; }
    public string Distro { get; private set; }
    public string TargetFramework { get; private set; }

    public DockerImage(string os, string distro, string targetFramework)
    {
        OS = os;
        Distro = distro;
        TargetFramework = targetFramework;
    }

    public void Deconstruct(out string os, out string distro, out string targetFramework)
    {
        os = OS;
        distro = Distro;
        targetFramework = TargetFramework;
    }
}
