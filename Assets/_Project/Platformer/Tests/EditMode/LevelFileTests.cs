using System.IO;
using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class LevelFileTests
    {
        [Test]
        public void AllLevelFiles_Parse()
        {
            var files = LevelFiles.All();
            Assert.IsNotEmpty(files, "no level files found");
            foreach (string f in files)
            {
                LevelLayout l = LevelLayout.Parse(File.ReadAllText(f));
                Assert.IsNotEmpty(l.Setting("id"), Path.GetFileName(f) + " needs '@id'");
                Assert.IsNotEmpty(l.Solids, Path.GetFileName(f));
            }
        }

        [Test]
        public void AllLevelFiles_HaveUniqueIds()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (string f in LevelFiles.All())
            {
                string id = LevelLayout.Parse(File.ReadAllText(f)).Setting("id");
                Assert.IsTrue(ids.Add(id), "duplicate id " + id);
            }
        }
    }
}
