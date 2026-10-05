using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace SteepingSpirits.Platforming.Tests
{
    /// <summary>Finds the real level texts both under Unity (cwd = project) and under dotnet (bin folder).</summary>
    public static class LevelFiles
    {
        public const string RelativeFolder = "Assets/_Project/Platformer/Levels";

        public static string Folder()
        {
            foreach (string start in new[] { Directory.GetCurrentDirectory(), TestContext.CurrentContext.TestDirectory })
            {
                var dir = new DirectoryInfo(start);
                while (dir != null)
                {
                    string candidate = Path.Combine(dir.FullName, RelativeFolder);
                    if (Directory.Exists(candidate))
                    {
                        return candidate;
                    }

                    dir = dir.Parent;
                }
            }

            return null;
        }

        public static List<string> All()
        {
            string folder = Folder();
            var list = new List<string>();
            if (folder != null)
            {
                list.AddRange(Directory.GetFiles(folder, "*.txt"));
                list.Sort();
            }

            return list;
        }

        public static string Read(string fileStart)
        {
            foreach (string f in All())
            {
                if (Path.GetFileName(f).StartsWith(fileStart))
                {
                    return File.ReadAllText(f);
                }
            }

            Assert.Fail("Level file not found: " + fileStart);
            return null;
        }
    }
}
