using System;
using System.Collections.Generic;
using System.Linq;
using Blockbloom.Core;

namespace Blockbloom.Sim
{
    /// <summary>Bir kosunun (bot x zorluk) tum olcumleri.</summary>
    public sealed class RunAgg
    {
        public const int MaxLineBucket = 16;
        private static readonly Dictionary<PieceDef, int> PieceIndex = BuildPieceIndex();

        public string Name;
        public BotKind Bot;
        public bool DifficultyOn;
        public string Engine;
        public int Games;
        public double WallSeconds;

        public readonly List<int> Moves = new List<int>();
        public readonly List<long> Scores = new List<long>();
        public readonly List<int> Lines = new List<int>();
        public readonly List<int> MaxCombos = new List<int>();
        public readonly List<int> PerfectClears = new List<int>();
        public readonly List<int> Trays = new List<int>();
        public int Capped;
        public int EarlyEnd; // <= 9 hamlede biten (bitmis) oyun
        public readonly int[] EndRemaining = new int[4];

        public long TotalMoves;
        public long ClearingMoves;
        public readonly long[] LineHist = new long[MaxLineBucket + 1];
        public long RowAndColMoves; // ayni hamlede hem satir hem sutun

        public readonly long[] ModeTrays = new long[3];
        public readonly long[,] FitHist = new long[3, 4];
        public readonly long[] FilledAtDealSum = new long[3];
        public long S1Violations;
        public long S2Violations;
        public long TrayRuleViolations;
        public long ReliefByFilledOnly, ReliefByTOnly, ReliefByBoth;
        public readonly long[,] PieceFreq = new long[3, 37];
        public readonly long[] NormalNoRepairFreq = new long[37];
        public long NormalNoRepairSlots;
        public readonly long[] RepairTrays = new long[3];
        public long TraysWithKnownRepair;
        public readonly List<double> GenMicros = new List<double>();

        // Muhasebe ve dogrulama (S3, S4)
        public long ScoreSumMismatch;
        public long CellsFormulaMismatch;
        public long StatsMismatch;
        public long GreedyPredMismatch;
        public long ZeroLegalNotOver;
        public long ShadowMismatchGames;
        public long ShadowCheckedGames;
        public long DeterminismChecked, DeterminismMismatch;
        public long ResumeChecked, ResumeMismatch;
        public readonly List<string> Errors = new List<string>();

        public void AddError(string e) { if (Errors.Count < 30) Errors.Add(e); }

        public void OnTray(TrayInfo t)
        {
            int m = (int)t.Mode;
            ModeTrays[m]++;
            FitHist[m, t.Fitting]++;
            FilledAtDealSum[m] += t.Filled;
            if (t.Fitting == 0) S1Violations++;
            if (t.Mode == DifficultyMode.Relief)
            {
                if (t.Fitting < CoreConfig.ReliefMinFitting) S2Violations++;
                bool byFilled = t.Filled >= CoreConfig.ReliefMinFilled;
                bool byT = t.T >= CoreConfig.ReliefMinTraysWithoutClear;
                if (byFilled && byT) ReliefByBoth++;
                else if (byFilled) ReliefByFilledOnly++;
                else ReliefByTOnly++;
            }
            for (int i = 0; i < t.Pieces.Length; i++) PieceFreq[m, PieceIndex[t.Pieces[i]]]++;
            {
                int large = 0, maxSame = 0;
                for (int i = 0; i < t.Pieces.Length; i++)
                {
                    if (t.Pieces[i].Size == SizeClass.Large) large++;
                    int same = 0;
                    for (int j = 0; j < t.Pieces.Length; j++) if (t.Pieces[j] == t.Pieces[i]) same++;
                    if (same > maxSame) maxSame = same;
                }
                if (maxSame > CoreConfig.MaxSamePiecePerTray || large > Difficulty.Profile(t.Mode).MaxLarge)
                {
                    TrayRuleViolations++;
                    AddError("tepsi kurali ihlali: " + string.Join(",", (object[])t.Pieces) + " mod " + t.Mode);
                }
            }
            if (t.Repairs >= 0)
            {
                TraysWithKnownRepair++;
                if (t.Repairs > 0) RepairTrays[m]++;
                if (t.Mode == DifficultyMode.Normal)
                {
                    // NORMAL'de asgari sigan 1: onarim yalniz uc parca da sigmiyorsa olur ve en buyuk indeksli yuvayi (2) degistirir.
                    int countSlots = t.Repairs == 0 ? 3 : 3 - t.Repairs;
                    for (int i = 0; i < countSlots; i++) NormalNoRepairFreq[PieceIndex[t.Pieces[i]]]++;
                    NormalNoRepairSlots += countSlots;
                }
            }
            if (t.GenMicros >= 0) GenMicros.Add(t.GenMicros);
        }

        public void OnMove(MoveOutcome o)
        {
            TotalMoves++;
            if (o.Lines > 0)
            {
                ClearingMoves++;
                LineHist[Math.Min(o.Lines, MaxLineBucket)]++;
                if (o.RowsCleared > 0 && o.ColsCleared > 0) RowAndColMoves++;
                int expected = 8 * o.RowsCleared + 8 * o.ColsCleared - o.RowsCleared * o.ColsCleared;
                if (o.CellsCleared != expected) { CellsFormulaMismatch++; AddError("CellsCleared formulu tutmadi"); }
            }
        }

        public void OnGameEnd(int moves, long score, GameStats st, bool capped, int remaining)
        {
            Games++;
            Moves.Add(moves);
            Scores.Add(score);
            Lines.Add(st.LinesCleared);
            MaxCombos.Add(st.MaxCombo);
            PerfectClears.Add(st.PerfectClears);
            Trays.Add(st.TraysDealt);
            if (capped) Capped++;
            else
            {
                EndRemaining[remaining]++;
                if (moves <= 9) EarlyEnd++;
            }
        }

        private static Dictionary<PieceDef, int> BuildPieceIndex()
        {
            var d = new Dictionary<PieceDef, int>(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < PieceCatalog.All.Count; i++) d.Add(PieceCatalog.All[i], i);
            return d;
        }

        // ---------- Ozet ----------

        public static double Percentile(IList<double> sorted, double p)
        {
            if (sorted.Count == 0) return double.NaN;
            int rank = (int)Math.Ceiling(p / 100.0 * sorted.Count);
            if (rank < 1) rank = 1;
            return sorted[Math.Min(rank, sorted.Count) - 1];
        }

        public static Dictionary<string, object> Dist(IEnumerable<double> values)
        {
            var s = values.OrderBy(v => v).ToList();
            return new Dictionary<string, object>
            {
                ["mean"] = Math.Round(s.Count == 0 ? double.NaN : s.Average(), 3),
                ["median"] = Percentile(s, 50),
                ["p10"] = Percentile(s, 10),
                ["p90"] = Percentile(s, 90),
                ["p99"] = Percentile(s, 99),
                ["min"] = s.Count == 0 ? double.NaN : s[0],
                ["max"] = s.Count == 0 ? double.NaN : s[s.Count - 1],
            };
        }

        public long TotalTrays => ModeTrays.Sum();
        public double MeanMoves => Moves.Average();
        public double MeanScore => Scores.Average();
        public double MedianMaxCombo => Percentile(MaxCombos.Select(x => (double)x).OrderBy(x => x).ToList(), 50);
        public double ModeShare(DifficultyMode m) => TotalTrays == 0 ? 0 : 100.0 * ModeTrays[(int)m] / TotalTrays;
        public double ClearingRate => TotalMoves == 0 ? 0 : 100.0 * ClearingMoves / TotalMoves;

        /// <summary>S14: NORMAL tepsilerde (onarim haric) her parcanin frekansi ile taban/124 farki (yuzde puani).</summary>
        public double[] NormalDeviationPp()
        {
            var dev = new double[37];
            if (NormalNoRepairSlots == 0) return null;
            for (int i = 0; i < 37; i++)
            {
                double f = 100.0 * NormalNoRepairFreq[i] / NormalNoRepairSlots;
                double b = 100.0 * PieceCatalog.All[i].BaseWeight / PieceCatalog.TotalBaseWeight;
                dev[i] = f - b;
            }
            return dev;
        }

        public Dictionary<string, object> ToJson()
        {
            var modeNames = new[] { "Open", "Normal", "Relief" };
            var modes = new Dictionary<string, object>();
            for (int m = 0; m < 3; m++)
            {
                long n = ModeTrays[m];
                modes[modeNames[m]] = new Dictionary<string, object>
                {
                    ["trays"] = n,
                    ["sharePct"] = Math.Round(ModeShare((DifficultyMode)m), 3),
                    ["meanFilledAtDeal"] = n == 0 ? 0 : Math.Round((double)FilledAtDealSum[m] / n, 2),
                    ["fittingAtDeal_0_1_2_3"] = new[] { FitHist[m, 0], FitHist[m, 1], FitHist[m, 2], FitHist[m, 3] },
                    ["traysWithRepair"] = RepairTrays[m],
                    ["pieceFreqPct"] = PieceFreqPct(m),
                };
            }

            var lineHist = new Dictionary<string, object>();
            long l4plus = 0;
            for (int i = 4; i <= MaxLineBucket; i++) l4plus += LineHist[i];
            lineHist["1"] = Pct(LineHist[1], ClearingMoves);
            lineHist["2"] = Pct(LineHist[2], ClearingMoves);
            lineHist["3"] = Pct(LineHist[3], ClearingMoves);
            lineHist["4+"] = Pct(l4plus, ClearingMoves);
            var lineRaw = new Dictionary<string, long>();
            for (int i = 1; i <= MaxLineBucket; i++) if (LineHist[i] > 0) lineRaw[i.ToString()] = LineHist[i];

            var comboHist = MaxCombos.GroupBy(c => c).OrderBy(g => g.Key).ToDictionary(g => g.Key.ToString(), g => g.Count());

            var gen = GenMicros.OrderBy(x => x).ToList();
            double[] dev = NormalDeviationPp();
            object s14 = null;
            if (dev != null)
            {
                int worst = 0;
                for (int i = 1; i < 37; i++) if (Math.Abs(dev[i]) > Math.Abs(dev[worst])) worst = i;
                s14 = new Dictionary<string, object>
                {
                    ["slotsCounted"] = NormalNoRepairSlots,
                    ["maxAbsDevPp"] = Math.Round(Math.Abs(dev[worst]), 4),
                    ["worstPiece"] = PieceCatalog.All[worst].Id,
                    ["devPpByPiece"] = Enumerable.Range(0, 37).ToDictionary(i => PieceCatalog.All[i].Id, i => Math.Round(dev[i], 4)),
                };
            }

            return new Dictionary<string, object>
            {
                ["name"] = Name,
                ["bot"] = Bot.ToString(),
                ["difficulty"] = DifficultyOn ? "on" : "off",
                ["engine"] = Engine,
                ["games"] = Games,
                ["wallSeconds"] = Math.Round(WallSeconds, 3),
                ["moves"] = Dist(Moves.Select(x => (double)x)),
                ["score"] = Dist(Scores.Select(x => (double)x)),
                ["linesCleared"] = Dist(Lines.Select(x => (double)x)),
                ["maxCombo"] = Dist(MaxCombos.Select(x => (double)x)),
                ["perfectClears"] = Dist(PerfectClears.Select(x => (double)x)),
                ["traysDealt"] = Dist(Trays.Select(x => (double)x)),
                ["maxComboHistogram"] = comboHist,
                ["gamesWithPerfectClearPct"] = Math.Round(100.0 * PerfectClears.Count(x => x > 0) / Math.Max(1, Games), 3),
                ["perfectClearPerMovePct"] = Pct(PerfectClears.Sum(), TotalMoves),
                ["cappedGames"] = Capped,
                ["cappedPct"] = Math.Round(100.0 * Capped / Math.Max(1, Games), 3),
                ["earlyEndLe9MovesPct"] = Math.Round(100.0 * EarlyEnd / Math.Max(1, Games), 3),
                ["endRemainingPieces_0_1_2_3"] = EndRemaining,
                ["totalMoves"] = TotalMoves,
                ["clearingMovePct"] = Math.Round(ClearingRate, 3),
                ["lineCountShareOfClearingMovesPct"] = lineHist,
                ["lineCountRaw"] = lineRaw,
                ["rowAndColSameMovePctOfClearing"] = Pct(RowAndColMoves, ClearingMoves),
                ["totalTrays"] = TotalTrays,
                ["modes"] = modes,
                ["reliefTrigger"] = new Dictionary<string, object>
                {
                    ["filledOnly"] = ReliefByFilledOnly,
                    ["tOnly"] = ReliefByTOnly,
                    ["both"] = ReliefByBoth,
                },
                ["s1_traysNoFit"] = S1Violations,
                ["s2_reliefTraysFitLt2"] = S2Violations,
                ["trayRuleViolations"] = TrayRuleViolations,
                ["trayGenMicros"] = gen.Count == 0 ? null : new Dictionary<string, object>
                {
                    ["count"] = gen.Count,
                    ["mean"] = Math.Round(gen.Average(), 3),
                    ["median"] = Math.Round(Percentile(gen, 50), 3),
                    ["p99"] = Math.Round(Percentile(gen, 99), 3),
                    ["max"] = Math.Round(gen[gen.Count - 1], 3),
                },
                ["s14_normalFreqNoRepair"] = s14,
                ["checks"] = new Dictionary<string, object>
                {
                    ["scoreSumMismatch"] = ScoreSumMismatch,
                    ["cellsClearedFormulaMismatch"] = CellsFormulaMismatch,
                    ["statsMismatch"] = StatsMismatch,
                    ["greedyPredictionMismatch"] = GreedyPredMismatch,
                    ["zeroLegalMovesButNotOver"] = ZeroLegalNotOver,
                    ["shadowCheckedGames"] = ShadowCheckedGames,
                    ["shadowMismatchGames"] = ShadowMismatchGames,
                    ["determinismChecked"] = DeterminismChecked,
                    ["determinismMismatch"] = DeterminismMismatch,
                    ["resumeChecked"] = ResumeChecked,
                    ["resumeMismatch"] = ResumeMismatch,
                    ["errors"] = Errors,
                },
            };
        }

        private Dictionary<string, double> PieceFreqPct(int m)
        {
            long total = 0;
            for (int i = 0; i < 37; i++) total += PieceFreq[m, i];
            var d = new Dictionary<string, double>();
            for (int i = 0; i < 37; i++) d[PieceCatalog.All[i].Id] = total == 0 ? 0 : Math.Round(100.0 * PieceFreq[m, i] / total, 3);
            return d;
        }

        private static double Pct(long a, long b) => b == 0 ? 0 : Math.Round(100.0 * a / b, 3);
    }
}
