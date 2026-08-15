using System.Runtime.CompilerServices;
using Wording.Core.Packs;

namespace Wording.Core.Tests;

/// <summary>
/// Checks every pack published in learning_data/ with the parser the app uses.
/// <para>
/// These packs are meant to be contributed by anyone, so the only thing keeping the
/// directory honest is that a file which would be refused on someone's machine fails the
/// build here first. The Swift port runs the same check from its side.
/// </para>
/// </summary>
public class PublishedPackTests
{
    public static TheoryData<string> PublishedPacks()
    {
        var data = new TheoryData<string>();

        foreach (var path in Directory.EnumerateFiles(Directory_(), "*.json"))
        {
            data.Add(path);
        }

        return data;
    }

    [Fact]
    public void TheDirectoryIsWhereItIsExpected()
    {
        // Otherwise an empty theory would pass while checking nothing at all.
        Assert.True(Directory.Exists(Directory_()), $"could not find {Directory_()}");
        Assert.NotEmpty(Directory.EnumerateFiles(Directory_(), "*.json"));
    }

    [Theory]
    [MemberData(nameof(PublishedPacks))]
    public void EveryPublishedPackWouldImportCleanly(string path)
    {
        var pack = WordPackReader.Read(File.ReadAllBytes(path));

        // The file name has to match the id, or the set lands under a name nobody chose.
        Assert.Equal(Path.GetFileNameWithoutExtension(path), pack.Id);
        Assert.NotEmpty(pack.Words);
    }

    [Theory]
    [MemberData(nameof(PublishedPacks))]
    public void NoPublishedPackRepeatsAWord(string path)
    {
        var pack = WordPackReader.Read(File.ReadAllBytes(path));

        var duplicates = pack.Words
            .GroupBy(word => word.Original.ToLowerInvariant())
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        Assert.Empty(duplicates);
    }

    [Fact]
    public void TheGeneratorPromptQuotesTheLimitsThatAreActuallyEnforced()
    {
        // learning_data/PROMPT.md tells contributors - and their AI - what a pack may
        // contain. A prompt that has drifted from PackLimits produces files that look
        // right and are refused on import, which is worse than having no prompt.
        var prompt = File.ReadAllText(Path.Combine(Directory_(), "PROMPT.md"));

        Assert.Contains(PackLimits.MaxWords.ToString(), prompt, StringComparison.Ordinal);
        Assert.Contains(PackLimits.MaxFieldLength.ToString(), prompt, StringComparison.Ordinal);
        Assert.Contains(PackLimits.MaxNameLength.ToString(), prompt, StringComparison.Ordinal);
        Assert.Contains(PackLimits.MaxDescriptionLength.ToString(), prompt, StringComparison.Ordinal);
        Assert.Contains(PackLimits.MaxIdLength.ToString(), prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void TheLimitsMatchTheSwiftPort()
    {
        // Spelled out rather than read from PackLimits, so changing one port fails here
        // and says plainly that WordingKit/PackLimits.swift has to change with it. The
        // two apps import the same published packs; limits that disagree mean a pack one
        // of them accepts and the other refuses.
        Assert.Equal(2 * 1024 * 1024, PackLimits.MaxPayloadBytes);
        Assert.Equal(5_000, PackLimits.MaxWords);
        Assert.Equal(200, PackLimits.MaxFieldLength);
        Assert.Equal(80, PackLimits.MaxNameLength);
        Assert.Equal(300, PackLimits.MaxDescriptionLength);
        Assert.Equal(64, PackLimits.MaxIdLength);
    }

    /// <summary>
    /// Located from this source file rather than the working directory, which differs
    /// between `dotnet test`, the IDE and CI.
    /// </summary>
    static string Directory_([CallerFilePath] string thisFile = "") =>
        Path.Combine(
            Path.GetDirectoryName(thisFile)!,
            "..", "..", "learning_data");
}
