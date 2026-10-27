using System;
using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using SteepingSpirits.Brewing.Integration;

namespace SteepingSpirits.TeaLoop.Tests
{
    /// <summary>
    /// Contract between brewing and the garden: the item a tart cup gives must be one the compost bin takes.
    /// GardenSettings is a ScriptableObject, so its default list is read from the source file.
    /// </summary>
    public class TartCompostTests
    {
        [Test]
        public void TartCup_IsTakenByTheGardenCompostBin()
        {
            string settings = File.ReadAllText(Path.Combine(RepoRoot(), "Assets/_Project/Plantation/Runtime/Data/GardenSettings.cs"));
            int line = settings.IndexOf("compostInputIds", StringComparison.Ordinal);
            Assert.GreaterOrEqual(line, 0, "GardenSettings.compostInputIds not found");
            string declaration = settings.Substring(line, settings.IndexOf(';', line) - line);
            StringAssert.Contains("\"" + OwnTeaInfo.TartItemId + "\"", declaration);
        }

        private static string RepoRoot([CallerFilePath] string here = "")
        {
            return Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here), "..", ".."));
        }
    }
}
