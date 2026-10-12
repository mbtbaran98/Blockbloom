using System;

namespace Blockbloom.Game
{
    /// <summary>Uygulamanin ust duzey ekrani (GDD §7.4).</summary>
    public enum AppScreen
    {
        Menu,
        Game,
    }

    /// <summary>Ana menu dugmeleri.</summary>
    public enum MenuButton
    {
        None,
        Continue,
        NewGame,
    }

    /// <summary>Geri tusunun (Android) sonucu.</summary>
    public enum BackAction
    {
        None,
        GoToMenu,
        Quit,
    }

    /// <summary>
    /// GDD §7.4 ana menu icerigi: hangi satir/dugme gorunur, hangisi birincil. Saf C# (UnityEngine yok); EditMode'da test edilir.
    /// </summary>
    public struct MainMenuModel
    {
        /// <summary>"Rekor: N" satiri (rekor 0 ise gizli).</summary>
        public bool ShowRecord;
        public int Record;
        /// <summary>"Devam et" yalniz surdurulebilir yarim oyun varsa.</summary>
        public bool ShowContinue;
        /// <summary>"Yeni oyun" her zaman gorunur.</summary>
        public bool ShowNewGame;
        /// <summary>Vurgulu dugme: kayit varsa Devam et, yoksa Yeni oyun.</summary>
        public MenuButton Primary;

        public static MainMenuModel Create(bool hasResumableGame, int bestScore)
        {
            if (bestScore < 0) throw new ArgumentOutOfRangeException(nameof(bestScore));
            var m = new MainMenuModel();
            m.Record = bestScore;
            m.ShowRecord = bestScore > 0;
            m.ShowContinue = hasResumableGame;
            m.ShowNewGame = true;
            m.Primary = hasResumableGame ? MenuButton.Continue : MenuButton.NewGame;
            return m;
        }

        public bool IsPrimary(MenuButton b) => b != MenuButton.None && b == Primary;

        public static string RecordText(int record) => "Rekor: " + record;
    }

    /// <summary>Ekranlar arasi gezinme kurallari (GDD §7.4). Saf C#.</summary>
    public static class AppNav
    {
        /// <summary>
        /// Geri tusu: oyunda menu dugmesiyle ayni (menuye don), menude uygulamadan cik.
        /// Gecis surerken ya da oyun sonu beklemesinde (GDD §7.1: girdi kilitli) yok sayilir.
        /// </summary>
        public static BackAction OnBack(AppScreen screen, bool transitioning, bool inputLocked)
        {
            if (transitioning) return BackAction.None;
            if (screen == AppScreen.Menu) return BackAction.Quit;
            return inputLocked ? BackAction.None : BackAction.GoToMenu;
        }

        /// <summary>Sürükleme yalniz oyun ekraninda ve gecis yokken mumkun.</summary>
        public static bool DragAllowed(AppScreen screen, bool transitioning) => screen == AppScreen.Game && !transitioning;
    }

    /// <summary>
    /// Isabet testi tabanli dugme basimi (EventSystem yok): basilan dugme temas basladiginda "kurulur", ayni dugmenin
    /// ustunde birakilirsa tetiklenir. Disarida birakmak iptal eder. Dugme kimligi: 0..n-1, -1 = hicbiri. Bellek ayirmaz.
    /// </summary>
    public sealed class TapTracker
    {
        public const int NoButton = -1;

        public int Armed { get; private set; } = NoButton;

        /// <summary>Temas basladi; hit = temas noktasindaki dugme (ya da NoButton). Bir dugme kurulduysa true.</summary>
        public bool Down(int hit)
        {
            Armed = hit < 0 ? NoButton : hit;
            return Armed != NoButton;
        }

        /// <summary>Temas bitti; hit = birakma noktasindaki dugme. Tetiklenen dugme ya da NoButton.</summary>
        public int Up(int hit)
        {
            int armed = Armed;
            Armed = NoButton;
            return armed != NoButton && armed == hit ? armed : NoButton;
        }

        public void Reset() => Armed = NoButton;
    }

    /// <summary>
    /// Ekran gecisi "zemine solup geri acilma": once ortu 0 -> 1 (FadeOut), ortadaki anda ekran degisir, sonra 1 -> 0 (FadeIn).
    /// Toplam sure &lt;= 200 ms (GDD §7.4). Saf hesap; bellek ayirmaz.
    /// </summary>
    public struct FadeTimeline
    {
        public const float OutDuration = 0.09f;
        public const float InDuration = 0.09f;
        public const float TotalDuration = OutDuration + InDuration;

        private float _start;
        private float _out; // solma suresi (yalniz acilmada 0)

        public bool Active { get; private set; }

        /// <summary>Tam gecis (solma + acilma).</summary>
        public static FadeTimeline Transition(float now)
        {
            var f = new FadeTimeline();
            f._start = now;
            f._out = OutDuration;
            f.Active = true;
            return f;
        }

        /// <summary>Yalniz acilma (uygulama acilisi: ortu 1 -> 0).</summary>
        public static FadeTimeline RevealOnly(float now)
        {
            var f = new FadeTimeline();
            f._start = now;
            f._out = 0f;
            f.Active = true;
            return f;
        }

        /// <summary>Ortu opakligi 0..1 (1 = ekran tamamen zemin rengi).</summary>
        public float Cover(float now)
        {
            if (!Active) return 0f;
            float t = now - _start;
            if (t < _out) return t <= 0f ? 0f : t / _out;
            float u = (t - _out) / InDuration;
            if (u <= 0f) return 1f;
            return u >= 1f ? 0f : 1f - u;
        }

        /// <summary>Ekranin degismesi gereken an gecti mi (solmanin sonu).</summary>
        public bool PastMidpoint(float now) => now - _start >= _out;

        /// <summary>Gecis bitti mi.</summary>
        public bool Finished(float now) => now - _start >= _out + InDuration;

        public void Stop() => Active = false;
    }

    /// <summary>Asgari dokunma alani (GDD §7.4: &gt;= 44 pt). Saf hesap.</summary>
    public static class TouchTarget
    {
        public const float MinPoints = 44f;
        /// <summary>1 pt = DPI / 160 piksel (Android dp; iOS pt ile pratikte ayni boy).</summary>
        public const float PointsPerInchBase = 160f;
        /// <summary>DPI bilinmiyorsa (Screen.dpi = 0): ekranin kisa kenari tipik telefon genisligi 360 pt sayilir.</summary>
        public const float FallbackShortSidePoints = 360f;

        /// <summary>44 pt'nin piksel karsiligi. dpi &lt;= 0 ise kisa kenara gore tahmin.</summary>
        public static float MinPixels(float dpi, float screenShortSidePx)
        {
            float pxPerPoint = dpi > 0f
                ? dpi / PointsPerInchBase
                : screenShortSidePx / FallbackShortSidePoints;
            return MinPoints * Math.Max(1f, pxPerPoint); // en az 44 px
        }
    }
}
