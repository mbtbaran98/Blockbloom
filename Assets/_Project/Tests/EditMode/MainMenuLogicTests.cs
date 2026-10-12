using Blockbloom.Game;
using NUnit.Framework;

namespace Blockbloom.Tests.EditMode
{
    /// <summary>
    /// GDD §7.4 ana menu durum mantigi (saf C#: MainMenuModel, AppNav, TapTracker, FadeTimeline, TouchTarget).
    /// Gecis sinir anlari float yuvarlamasi icin 1e-4 s payla sinanir.
    /// </summary>
    public class MainMenuLogicTests
    {
        // ---------------------------------------------------------------- MainMenuModel

        [Test]
        public void NoSave_NoRecord_OnlyNewGame_AsPrimary()
        {
            MainMenuModel m = MainMenuModel.Create(false, 0);
            Assert.That(m.ShowContinue, Is.False);
            Assert.That(m.ShowNewGame, Is.True);
            Assert.That(m.ShowRecord, Is.False, "rekor 0 ise satir gizli");
            Assert.That(m.Primary, Is.EqualTo(MenuButton.NewGame));
            Assert.That(m.IsPrimary(MenuButton.NewGame), Is.True);
        }

        [Test]
        public void Save_ContinueVisibleAndPrimary_NewGameSecondary()
        {
            MainMenuModel m = MainMenuModel.Create(true, 0);
            Assert.That(m.ShowContinue, Is.True);
            Assert.That(m.ShowNewGame, Is.True, "Yeni oyun her zaman gorunur");
            Assert.That(m.Primary, Is.EqualTo(MenuButton.Continue));
            Assert.That(m.IsPrimary(MenuButton.NewGame), Is.False);
        }

        [TestCase(false, 1)]
        [TestCase(true, 4321)]
        public void PositiveRecord_IsShown(bool hasSave, int best)
        {
            MainMenuModel m = MainMenuModel.Create(hasSave, best);
            Assert.That(m.ShowRecord, Is.True);
            Assert.That(m.Record, Is.EqualTo(best));
            Assert.That(MainMenuModel.RecordText(m.Record), Is.EqualTo("Rekor: " + best));
        }

        [Test]
        public void NegativeRecord_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => MainMenuModel.Create(false, -1));
        }

        [Test]
        public void IsPrimary_None_IsFalse()
        {
            Assert.That(MainMenuModel.Create(true, 5).IsPrimary(MenuButton.None), Is.False);
        }

        // ---------------------------------------------------------------- AppNav

        [Test]
        public void Back_InGame_GoesToMenu()
        {
            Assert.That(AppNav.OnBack(AppScreen.Game, false, false), Is.EqualTo(BackAction.GoToMenu));
        }

        [Test]
        public void Back_InMenu_Quits()
        {
            Assert.That(AppNav.OnBack(AppScreen.Menu, false, false), Is.EqualTo(BackAction.Quit));
        }

        [Test]
        public void Back_DuringTransitionOrGameOverWait_Ignored()
        {
            Assert.That(AppNav.OnBack(AppScreen.Menu, true, false), Is.EqualTo(BackAction.None));
            Assert.That(AppNav.OnBack(AppScreen.Game, true, false), Is.EqualTo(BackAction.None));
            Assert.That(AppNav.OnBack(AppScreen.Game, false, true), Is.EqualTo(BackAction.None));
        }

        [Test]
        public void Drag_OnlyInGame_AndNotDuringTransition()
        {
            Assert.That(AppNav.DragAllowed(AppScreen.Game, false), Is.True);
            Assert.That(AppNav.DragAllowed(AppScreen.Menu, false), Is.False, "menu acikken surukleme yok");
            Assert.That(AppNav.DragAllowed(AppScreen.Game, true), Is.False);
            Assert.That(AppNav.DragAllowed(AppScreen.Menu, true), Is.False);
        }

        // ---------------------------------------------------------------- TapTracker

        [Test]
        public void Tap_DownAndUpOnSameButton_Fires()
        {
            var t = new TapTracker();
            Assert.That(t.Down(1), Is.True);
            Assert.That(t.Up(1), Is.EqualTo(1));
            Assert.That(t.Armed, Is.EqualTo(TapTracker.NoButton));
        }

        [Test]
        public void Tap_ReleasedOutsideOrOnOtherButton_DoesNotFire()
        {
            var t = new TapTracker();
            t.Down(0);
            Assert.That(t.Up(TapTracker.NoButton), Is.EqualTo(TapTracker.NoButton));
            t.Down(0);
            Assert.That(t.Up(1), Is.EqualTo(TapTracker.NoButton));
        }

        [Test]
        public void Tap_StartedOutside_DoesNotFire_EvenIfReleasedOnButton()
        {
            var t = new TapTracker();
            Assert.That(t.Down(TapTracker.NoButton), Is.False);
            Assert.That(t.Up(0), Is.EqualTo(TapTracker.NoButton));
            Assert.That(t.Up(0), Is.EqualTo(TapTracker.NoButton), "Down olmadan Up");
        }

        [Test]
        public void Tap_Reset_Disarms()
        {
            var t = new TapTracker();
            t.Down(1);
            t.Reset();
            Assert.That(t.Up(1), Is.EqualTo(TapTracker.NoButton));
        }

        // ---------------------------------------------------------------- FadeTimeline

        [Test]
        public void Fade_TotalAtMost200ms()
        {
            Assert.That(FadeTimeline.TotalDuration, Is.LessThanOrEqualTo(0.2f));
        }

        [Test]
        public void Fade_Transition_CoversThenReveals_SwitchesAtMidpoint()
        {
            const float t0 = 10f;
            FadeTimeline f = FadeTimeline.Transition(t0);
            Assert.That(f.Active, Is.True);
            Assert.That(f.Cover(t0), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(f.PastMidpoint(t0), Is.False);
            Assert.That(f.Cover(t0 + FadeTimeline.OutDuration * 0.5f), Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(f.PastMidpoint(t0 + FadeTimeline.OutDuration + 1e-4f), Is.True);
            Assert.That(f.Cover(t0 + FadeTimeline.OutDuration), Is.EqualTo(1f).Within(1e-3f));
            Assert.That(f.Cover(t0 + FadeTimeline.OutDuration + FadeTimeline.InDuration * 0.5f), Is.EqualTo(0.5f).Within(1e-3f));
            Assert.That(f.Finished(t0 + FadeTimeline.TotalDuration - 0.001f), Is.False);
            Assert.That(f.Finished(t0 + FadeTimeline.TotalDuration + 1e-4f), Is.True);
            Assert.That(f.Cover(t0 + FadeTimeline.TotalDuration + 1f), Is.EqualTo(0f));
        }

        [Test]
        public void Fade_RevealOnly_StartsCovered_AlreadyPastMidpoint()
        {
            FadeTimeline f = FadeTimeline.RevealOnly(3f);
            Assert.That(f.PastMidpoint(3f), Is.True);
            Assert.That(f.Cover(3f), Is.EqualTo(1f).Within(1e-3f));
            Assert.That(f.Finished(3f + FadeTimeline.InDuration + 1e-4f), Is.True);
        }

        [Test]
        public void Fade_Inactive_NoCover()
        {
            var f = new FadeTimeline();
            Assert.That(f.Active, Is.False);
            Assert.That(f.Cover(1f), Is.EqualTo(0f));
            f = FadeTimeline.Transition(0f);
            f.Stop();
            Assert.That(f.Cover(0.05f), Is.EqualTo(0f));
        }

        // ---------------------------------------------------------------- TouchTarget

        [TestCase(160f, 1080f, 44f)]
        [TestCase(480f, 1080f, 132f)]
        [TestCase(96f, 1080f, 44f)]     // dusuk DPI: en az 44 px
        [TestCase(0f, 1080f, 132f)]     // DPI bilinmiyor: kisa kenar 360 pt sayilir
        [TestCase(0f, 200f, 44f)]
        public void MinTouchPixels(float dpi, float shortSide, float expected)
        {
            Assert.That(TouchTarget.MinPixels(dpi, shortSide), Is.EqualTo(expected).Within(1e-3f));
        }
    }
}
