using Blockbloom.Core;
using NUnit.Framework;

namespace Blockbloom.Tests.EditMode
{
    /// <summary>
    /// PCG32 referans degerleri (pcg_basic demo, seed 42 stream 54) Unity'nin calisma zamaninda da ayni olmali.
    /// Ayni testler dotnet'te (tests/Blockbloom.Core.Tests/Pcg32Tests.cs) de var; burada Mono/IL2CPP davranisi
    /// dogrulanir (IL2CPP icin bu testi cihaz/PlayMode derlemesinde de calistirin).
    /// </summary>
    public class Pcg32ReferenceTests
    {
        [Test]
        public void ReferenceDemoOutput_Seed42_Stream54()
        {
            var rng = new Pcg32(42UL, 54UL);
            uint[] expected = { 0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu };
            for (int i = 0; i < expected.Length; i++)
                Assert.That(rng.NextUInt(), Is.EqualTo(expected[i]), "deger " + i);
        }

        [Test]
        public void SameSeed_SameGame()
        {
            GameState a = GameState.NewGame(123456789UL);
            GameState b = GameState.NewGame(123456789UL);
            for (int i = 0; i < Tray.SlotCount; i++)
            {
                Assert.That(a.Tray[i].Value.Def.Id, Is.EqualTo(b.Tray[i].Value.Def.Id));
                Assert.That(a.Tray[i].Value.Color, Is.EqualTo(b.Tray[i].Value.Color));
            }
        }
    }
}
