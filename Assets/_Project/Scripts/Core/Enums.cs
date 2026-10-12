namespace Blockbloom.Core
{
    /// <summary>Parca sinifi: K (Small, &lt;=3 hucre), O (Medium, 4), B (Large, &gt;=5). GDD §4.</summary>
    public enum SizeClass
    {
        Small,
        Medium,
        Large
    }

    /// <summary>Gorunmez zorluk modu (GDD §5.3). Oyuncuya gosterilmez.</summary>
    public enum DifficultyMode
    {
        /// <summary>ACIK</summary>
        Open,
        /// <summary>NORMAL</summary>
        Normal,
        /// <summary>RAHATLATMA</summary>
        Relief
    }
}
