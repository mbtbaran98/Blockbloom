namespace Blockbloom.Game
{
    /// <summary>
    /// GDD §8 ve §7.1 sabitleri (yapilandirilabilir). Birim "hucre" = tahta hucre kenari = 1 dunya birimi.
    /// </summary>
    public static class InteractionConfig
    {
        /// <summary>Tepside parca olcegi (tahta olceginin katı).</summary>
        public const float TrayScale = 0.55f;
        /// <summary>Kaldirma: dokunusta bu surede 1,0 olcege buyur (s).</summary>
        public const float LiftDuration = 0.08f;
        /// <summary>SURUKLEME_OFSETI: parcanin alt kenari parmagin bu kadar hucre yukarisinda.</summary>
        public const float DragOffsetCells = 1.2f;
        /// <summary>En yakin gecerli capa bu uzakliktan (hucre) buyukse capa yok.</summary>
        public const float SnapMaxDistance = 0.75f;
        /// <summary>Birakma onizlemesi opakligi.</summary>
        public const float PreviewAlpha = 0.45f;
        /// <summary>Gecersiz birakmada yuvaya donus suresi (s), ease-out.</summary>
        public const float ReturnDuration = 0.15f;
        /// <summary>Hicbir yere sigmayan tepsi parcasi opakligi.</summary>
        public const float UnfitAlpha = 0.40f;
        /// <summary>Patlayacak cizgi vurgusu: dolu hucrenin ustune beyaz parlaklik katmani opakligi (M0).</summary>
        public const float LineHighlightAlpha = 0.38f;
        /// <summary>M0: patlayan hucrelerin kaybolma suresi (s), &lt;= 150 ms.</summary>
        public const float ClearFadeDuration = 0.15f;
        /// <summary>GDD §7.1: oyun sonu bekleme (s).</summary>
        public const float GameOverDelay = 0.6f;
        /// <summary>GDD §7.1: dolu hucrelerin griye solma suresi (s).</summary>
        public const float GameOverGrayDuration = 0.4f;
    }
}
