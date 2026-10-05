using NUnit.Framework;
using SteepingSpirits.Platforming.Core;

namespace SteepingSpirits.Platforming.Tests
{
    public class KeyBindingsTests
    {
        [Test]
        public void Defaults_MatchTheGame()
        {
            var b = KeyBindings.Defaults();
            Assert.AreEqual("Space", b.KeysFor(BindAction.Jump)[0]);
            Assert.AreEqual("A", b.KeysFor(BindAction.Left)[0]);
            Assert.IsTrue(b.IsDefault);
            Assert.IsEmpty(b.Unbound());
        }

        [Test]
        public void Binding_MovesAKeyAwayFromItsOldAction()
        {
            var b = KeyBindings.Defaults();
            BindAction? lost = b.Bind(BindAction.Jump, 0, "A");
            Assert.AreEqual(BindAction.Left, lost);
            Assert.AreEqual("A", b.KeysFor(BindAction.Jump)[0]);
            Assert.AreEqual("", b.KeysFor(BindAction.Left)[0]);
            Assert.AreEqual("LeftArrow", b.KeysFor(BindAction.Left)[1], "the second key still works");
            Assert.IsFalse(b.IsDefault);
        }

        [Test]
        public void SerializeParse_RoundTrip()
        {
            var b = KeyBindings.Defaults();
            b.Bind(BindAction.Dash, 1, "J");
            KeyBindings back = KeyBindings.Parse(b.Serialize());
            Assert.AreEqual(b.Serialize(), back.Serialize());
            Assert.AreEqual("J", back.KeysFor(BindAction.Dash)[1]);
        }

        [Test]
        public void Parse_NeverLeavesAnActionWithoutKeys()
        {
            KeyBindings b = KeyBindings.Parse("Jump=,;Bogus=Q;Left");
            Assert.AreEqual("Space", b.KeysFor(BindAction.Jump)[0]);
            Assert.IsEmpty(b.Unbound());
        }
    }
}
