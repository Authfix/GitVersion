﻿namespace GitVersion
{
    using System;
    using System.Linq;
    using LibGit2Sharp;

    class OtherBranchVersionFinder 
    {
        public SemanticVersion FindVersion(GitVersionContext context)
        {
            try
            {
                return FindVersion(context, "master");
            }
            catch (Exception)
            {
                return new SemanticVersion();
            }
        }

        SemanticVersion FindVersion(GitVersionContext context, string baseBranchName)
        {
            var versionString = GetUnknownBranchSuffix(context.CurrentBranch);
            if (!versionString.Contains("."))
                return new SemanticVersion();
            var shortVersion = ShortVersionParser.Parse(versionString);

            var semanticVersionPreReleaseTag = context.CurrentBranch.Name.Replace("-" + versionString, string.Empty) + ".1";

            var nbHotfixCommits = BranchCommitDifferenceFinder.NumberOfCommitsInBranchNotKnownFromBaseBranch(context.Repository, context.CurrentBranch, BranchType.Unknown, baseBranchName);
            
            var tagVersion = RecentTagVersionExtractor.RetrieveMostRecentOptionalTagVersion(context.Repository, shortVersion, context.CurrentBranch.Commits.Take(nbHotfixCommits + 1));
            if (tagVersion != null)
            {
                semanticVersionPreReleaseTag = tagVersion;
            }
            return new SemanticVersion
            {
                Major = shortVersion.Major,
                Minor = shortVersion.Minor,
                Patch = shortVersion.Patch,
                PreReleaseTag = semanticVersionPreReleaseTag,
                BuildMetaData = new SemanticVersionBuildMetaData(nbHotfixCommits, context.CurrentBranch.Name, context.CurrentCommit.Sha, context.CurrentCommit.When())
            };

        }

        static string GetUnknownBranchSuffix(Branch branch)
        {
            var unknownBranchSuffix = branch.Name.Split('-', '/');
            if (unknownBranchSuffix.Length == 1)
                return branch.Name;
            return unknownBranchSuffix[1];
        }

    }
}