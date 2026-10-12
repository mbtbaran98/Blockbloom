using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Blockbloom.Core;

namespace Blockbloom.Sim
{
    public static class Program
    {
        private sealed class Options
        {
            public int Games = 1000;
            public ulong SeedStart = 1;
            public List<BotKind> Bots = new List<BotKind> { BotKind.Random, BotKind.Greedy };
            public List<bool> Difficulty = new List<bool> { true, false };
            public int MaxMoves = 5000;
            public string Out = "docs/reports/m0-balance.json";
            public bool Verify = true;
            public int StraightRerunGames = 100;
            public bool TimingPass = true;
            public bool Sweep = false;
            public List<BotKind> SweepBots = new List<BotKind> { BotKind.Random, BotKind.Greedy, BotKind.Smart };
        }

        private struct GameResult
        {
            public int Moves;
            public long Score;
            public ulong Hash;
            public bool Capped;
        }

        private static readonly JsonSerializerOptions SnapshotJson = new JsonSerializerOptions { IncludeFields = true };

        public static int Main(string[] args)
        {
            Options o;
            try { o = Parse(args); }
            catch (Exception e)
            {
                Console.Error.WriteLine("Hata: " + e.Message);
                PrintHelp();
                return 2;
            }
            if (o == null) { PrintHelp(); return 0; }

            Console.WriteLine($"Blockbloom M0 denge simulasyonu: {o.Games} oyun, tohum {o.SeedStart}..{o.SeedStart + (ulong)o.Games - 1}, " +
                              $"botlar [{string.Join(",", o.Bots)}], zorluk [{string.Join(",", o.Difficulty.Select(d => d ? "on" : "off"))}], tavan {o.MaxMoves}, dogrulama {(o.Verify ? "acik" : "kapali")}");

            // Isinma (JIT); olcumlere girmez.
            for (ulong s = 0; s < 20; s++)
            {
                Play(900_000 + s, BotKind.Greedy, true, true, true, o.MaxMoves, null, -1);
                Play(900_000 + s, BotKind.Random, false, false, false, o.MaxMoves, null, -1);
            }

            // S15: saf kosu suresi (GameState + bot, dogrulama/olcum yok), zorluk acik.
            double pureSeconds = double.NaN;
            int pureGames = 0;
            if (o.TimingPass && o.Difficulty.Contains(true) && o.Bots.Any(x => x == BotKind.Random || x == BotKind.Greedy))
            {
                var sw = Stopwatch.StartNew();
                // Yalniz GDD botlari (rastgele, acgozlu): S15 "2.000 oyun < 60 s" bu ikisini kapsar.
                foreach (BotKind b in o.Bots.Where(x => x == BotKind.Random || x == BotKind.Greedy))
                    for (int i = 0; i < o.Games; i++) { Play(o.SeedStart + (ulong)i, b, true, true, false, o.MaxMoves, null, -1); pureGames++; }
                pureSeconds = sw.Elapsed.TotalSeconds;
                Console.WriteLine($"Saf kosu (GameState + bot, zorluk acik): {pureGames} oyun {pureSeconds:F2} s");
            }

            var runs = new List<RunAgg>();
            var mainResults = new Dictionary<(BotKind, bool), GameResult[]>();
            var thinVerify = new List<Dictionary<string, object>>();
            foreach (bool diff in o.Difficulty)
            {
                foreach (BotKind b in o.Bots)
                {
                    var agg = new RunAgg
                    {
                        Name = b.ToString().ToLowerInvariant() + "-" + (diff ? "on" : "off"),
                        Bot = b,
                        DifficultyOn = diff,
                        Engine = diff ? "GameState" + (o.Verify ? " (+ golge ThinGame)" : "") : "ThinGame (NORMAL profil)",
                    };
                    var results = new GameResult[o.Games];
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < o.Games; i++)
                        results[i] = Play(o.SeedStart + (ulong)i, b, diff, diff, o.Verify && diff, o.MaxMoves, agg, -1);
                    agg.WallSeconds = sw.Elapsed.TotalSeconds;
                    mainResults[(b, diff)] = results;

                    if (o.Verify)
                    {
                        for (int i = 0; i < o.Games; i++)
                        {
                            ulong seed = o.SeedStart + (ulong)i;
                            if (i < o.StraightRerunGames)
                            {
                                GameResult r2 = Play(seed, b, diff, diff, false, o.MaxMoves, null, -1);
                                agg.DeterminismChecked++;
                                if (!Same(results[i], r2)) { agg.DeterminismMismatch++; agg.AddError($"determinizm: tohum {seed}"); }
                            }
                            if (diff && results[i].Moves >= 2)
                            {
                                GameResult r3 = Play(seed, b, true, true, false, o.MaxMoves, null, results[i].Moves / 2);
                                agg.ResumeChecked++;
                                if (!Same(results[i], r3)) { agg.ResumeMismatch++; agg.AddError($"anlik goruntuden devam: tohum {seed}"); }
                            }
                        }
                        if (diff)
                        {
                            // Ince dongu (zorluk ACIK) bagimsiz kosuda GameState ile birebir ayni mi?
                            int n = Math.Min(o.Games, o.StraightRerunGames), same = 0;
                            for (int i = 0; i < n; i++)
                            {
                                GameResult t = Play(o.SeedStart + (ulong)i, b, false, true, false, o.MaxMoves, null, -1);
                                if (Same(results[i], t)) same++;
                            }
                            thinVerify.Add(new Dictionary<string, object> { ["bot"] = b.ToString(), ["seeds"] = n, ["identical"] = same });
                        }
                    }
                    runs.Add(agg);
                    PrintRun(agg);
                }
            }

            var criteria = Criteria(runs, pureSeconds, pureGames, thinVerify);
            List<Dictionary<string, object>> sweep = o.Sweep ? RunSweep(o, mainResults) : null;
            Console.WriteLine();
            Console.WriteLine("Kriterler:");
            foreach (var c in criteria)
                Console.WriteLine($"  {c["id"],-4} {c["result"],-8} deger={c["value"]}  hedef={c["target"]}");

            var json = new Dictionary<string, object>
            {
                ["tool"] = "tools/Blockbloom.Sim",
                ["generatedUtc"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                ["environment"] = new Dictionary<string, object>
                {
                    ["dotnet"] = Environment.Version.ToString(),
                    ["os"] = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    ["processorCount"] = Environment.ProcessorCount,
                },
                ["options"] = new Dictionary<string, object>
                {
                    ["games"] = o.Games,
                    ["seedStart"] = o.SeedStart,
                    ["bots"] = o.Bots.Select(b => b.ToString()).ToArray(),
                    ["difficulty"] = o.Difficulty.Select(d => d ? "on" : "off").ToArray(),
                    ["maxMoves"] = o.MaxMoves,
                    ["verify"] = o.Verify,
                    ["randomBotSeedOffset"] = RandomBot.SeedOffset,
                },
                ["pureTiming"] = new Dictionary<string, object> { ["games"] = pureGames, ["seconds"] = double.IsNaN(pureSeconds) ? (object)null : Math.Round(pureSeconds, 3) },
                ["thinLoopVsGameState"] = thinVerify,
                ["criteria"] = criteria,
                ["runs"] = runs.Select(r => r.ToJson()).ToList(),
                ["sweep"] = sweep,
            };
            string dir = Path.GetDirectoryName(Path.GetFullPath(o.Out));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(o.Out, JsonSerializer.Serialize(json, new JsonSerializerOptions
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            }) + Environment.NewLine);
            Console.WriteLine("JSON yazildi: " + o.Out);

            bool hardFail = criteria.Any(c => (string)c["type"] == "Kesin" && (string)c["result"] == "KALDI");
            return hardFail ? 1 : 0;
        }

        /// <summary>Zorluk parametresi duyarlilik taramasi (GDD §10 ayar yonleri). Ince dongu + parametreli kopya kurallar.</summary>
        private static List<Dictionary<string, object>> RunSweep(Options o, Dictionary<(BotKind, bool), GameResult[]> main)
        {
            var output = new List<Dictionary<string, object>>();
            Console.WriteLine();
            Console.WriteLine("Duyarlilik taramasi (her varyant x bot " + o.Games + " oyun):");
            Console.WriteLine($"  {"varyant",-11} {"bot",-7} {"ort.hamle",9} {"med",6} {"ort.skor",10} {"tavan%",7} {"<=9%",6} {"patl%",6} {"komboMed",8} {"ACIK%",6} {"NORM%",6} {"RAHAT%",6}");
            foreach (SimDifficulty v in SimDifficulty.SweepVariants())
            {
                foreach (BotKind b in o.SweepBots)
                {
                    var agg = new RunAgg { Name = v.Name + "/" + b.ToString().ToLowerInvariant(), Bot = b, DifficultyOn = !v.AlwaysNormal, Engine = "ThinGame + SimDifficulty" };
                    var res = new GameResult[o.Games];
                    var sw = Stopwatch.StartNew();
                    for (int i = 0; i < o.Games; i++) res[i] = Play(o.SeedStart + (ulong)i, b, false, true, false, o.MaxMoves, agg, -1, v);
                    agg.WallSeconds = sw.Elapsed.TotalSeconds;

                    // Parametreli kopya Core ile ayni mi? (baseline = zorluk acik GameState, off = zorluk kapali ince dongu)
                    string crossCheck = null;
                    GameResult[] refRes = null;
                    if (v.Name == "baseline") main.TryGetValue((b, true), out refRes);
                    if (v.Name == "off") main.TryGetValue((b, false), out refRes);
                    if (refRes != null)
                    {
                        int same = 0;
                        for (int i = 0; i < o.Games; i++) if (Same(res[i], refRes[i])) same++;
                        crossCheck = same + "/" + o.Games;
                    }

                    double early = 100.0 * agg.EarlyEnd / agg.Games, cap = 100.0 * agg.Capped / agg.Games;
                    Console.WriteLine($"  {v.Name,-11} {b,-7} {agg.MeanMoves,9:F1} {RunAgg.Percentile(agg.Moves.Select(x => (double)x).OrderBy(x => x).ToList(), 50),6:F0} {agg.MeanScore,10:F1} {cap,7:F2} {early,6:F2} {agg.ClearingRate,6:F1} {agg.MedianMaxCombo,8:F0} {agg.ModeShare(DifficultyMode.Open),6:F1} {agg.ModeShare(DifficultyMode.Normal),6:F1} {agg.ModeShare(DifficultyMode.Relief),6:F1}" + (crossCheck != null ? "  (Core ile ayni: " + crossCheck + ")" : ""));
                    output.Add(new Dictionary<string, object>
                    {
                        ["variant"] = v.Name,
                        ["description"] = v.Description,
                        ["bot"] = b.ToString(),
                        ["games"] = agg.Games,
                        ["meanMoves"] = Math.Round(agg.MeanMoves, 3),
                        ["moves"] = RunAgg.Dist(agg.Moves.Select(x => (double)x)),
                        ["meanScore"] = Math.Round(agg.MeanScore, 3),
                        ["score"] = RunAgg.Dist(agg.Scores.Select(x => (double)x)),
                        ["cappedPct"] = Math.Round(cap, 3),
                        ["earlyEndLe9MovesPct"] = Math.Round(early, 3),
                        ["clearingMovePct"] = Math.Round(agg.ClearingRate, 3),
                        ["maxComboMedian"] = agg.MedianMaxCombo,
                        ["maxComboP99"] = RunAgg.Percentile(agg.MaxCombos.Select(x => (double)x).OrderBy(x => x).ToList(), 99),
                        ["modeSharePct"] = new Dictionary<string, double>
                        {
                            ["Open"] = Math.Round(agg.ModeShare(DifficultyMode.Open), 3),
                            ["Normal"] = Math.Round(agg.ModeShare(DifficultyMode.Normal), 3),
                            ["Relief"] = Math.Round(agg.ModeShare(DifficultyMode.Relief), 3),
                        },
                        ["s1_traysNoFit"] = agg.S1Violations,
                        ["s2_reliefTraysFitLt2"] = agg.S2Violations,
                        ["accountingErrors"] = agg.ScoreSumMismatch + agg.CellsFormulaMismatch + agg.StatsMismatch + agg.GreedyPredMismatch + agg.ZeroLegalNotOver,
                        ["identicalToCoreRun"] = crossCheck,
                        ["wallSeconds"] = Math.Round(agg.WallSeconds, 3),
                    });
                }
            }
            return output;
        }

        private static bool Same(GameResult a, GameResult b) => a.Hash == b.Hash && a.Score == b.Score && a.Moves == b.Moves && a.Capped == b.Capped;

        /// <summary>
        /// Tek oyun. useGameState: GameState (aksi ThinGame). shadow: GameState yanina golge ThinGame (zorluk acik).
        /// resumeAt &gt;= 0: o hamlede anlik goruntu -&gt; JSON -&gt; FromSnapshot ile devam (yalniz GameState).
        /// </summary>
        private static GameResult Play(ulong seed, BotKind bot, bool useGameState, bool difficultyOn, bool shadow,
            int maxMoves, RunAgg agg, int resumeAt, SimDifficulty cfg = null)
        {
            ISimGame g = cfg != null ? new ThinGame(seed, cfg)
                : useGameState ? new GameStateAdapter(GameState.NewGame(seed), shadow ? new ThinGame(seed, true) : null)
                : new ThinGame(seed, difficultyOn);
            IBot b = bot == BotKind.Random ? new RandomBot(seed) : bot == BotKind.Greedy ? new GreedyBot()
                : bot == BotKind.Smart ? new SmartBot() : (IBot)new PlannerBot();
            var list = new List<(int slot, Cell anchor)>(256);
            agg?.OnTray(g.LastTray);

            ulong hash = 14695981039346656037UL;
            long sum = 0;
            int moves = 0;
            bool capped = false;
            while (!g.IsGameOver)
            {
                if (moves >= maxMoves) { capped = true; break; }
                if (moves == resumeAt && g is GameStateAdapter ga)
                {
                    string js = JsonSerializer.Serialize(ga.G.ToSnapshot(), SnapshotJson);
                    GameSnapshot back = JsonSerializer.Deserialize<GameSnapshot>(js, SnapshotJson);
                    g = new GameStateAdapter(GameState.FromSnapshot(back), null);
                }
                int n = g.LegalMoves(list);
                if (n == 0)
                {
                    if (agg != null) { agg.ZeroLegalNotOver++; agg.AddError($"tohum {seed}: yasal hamle yok ama oyun bitmedi"); }
                    break;
                }
                int idx = b.Choose(g, list, out int predicted);
                (int s, Cell a) = list[idx];
                MoveOutcome mo = g.Place(s, a.X, a.Y);
                moves++;
                sum += mo.Total;
                hash = Mix(hash, (ulong)s);
                hash = Mix(hash, (ulong)a.X);
                hash = Mix(hash, (ulong)a.Y);
                hash = Mix(hash, (ulong)mo.Total);
                if (agg != null)
                {
                    if (predicted >= 0 && predicted != mo.Total) { agg.GreedyPredMismatch++; agg.AddError($"tohum {seed} hamle {moves}: acgozlu tahmin {predicted} != {mo.Total}"); }
                    agg.OnMove(mo);
                    if (mo.Refilled) agg.OnTray(g.LastTray);
                }
            }

            if (agg != null)
            {
                if (sum != g.Score) { agg.ScoreSumMismatch++; agg.AddError($"tohum {seed}: hamle toplami {sum} != skor {g.Score}"); }
                GameStats st = g.Stats;
                if (st.Moves != moves || st.MaxCombo < 0 || st.TraysDealt != 1 + moves / Tray.SlotCount)
                {
                    agg.StatsMismatch++;
                    agg.AddError($"tohum {seed}: istatistik tutarsiz (moves {st.Moves}/{moves}, trays {st.TraysDealt})");
                }
                if (g is GameStateAdapter gsa && gsa.HasShadow) agg.ShadowCheckedGames++;
                if (g is GameStateAdapter gsb && gsb.ShadowMismatch != null)
                {
                    agg.ShadowMismatchGames++;
                    agg.AddError($"tohum {seed}: golge farki ({gsb.ShadowMismatch})");
                }
                if (shadow && g is GameStateAdapter gsc && !gsc.HasShadow && gsc.ShadowMismatch == null)
                    agg.AddError($"tohum {seed}: golge kayboldu");
                agg.OnGameEnd(moves, sum, g.Stats, capped, g.RemainingInTray);
            }
            return new GameResult { Moves = moves, Score = sum, Hash = Mix(hash, (ulong)g.Score), Capped = capped };
        }

        private static ulong Mix(ulong h, ulong v)
        {
            unchecked
            {
                for (int i = 0; i < 8; i++)
                {
                    h ^= (v >> (i * 8)) & 0xFF;
                    h *= 1099511628211UL;
                }
            }
            return h;
        }

        // ---------- Kriterler (GDD §10) ----------

        private static List<Dictionary<string, object>> Criteria(List<RunAgg> runs, double pureSeconds, int pureGames,
            List<Dictionary<string, object>> thinVerify)
        {
            RunAgg R(BotKind b, bool d) => runs.FirstOrDefault(r => r.Bot == b && r.DifficultyOn == d);
            RunAgg rOn = R(BotKind.Random, true), gOn = R(BotKind.Greedy, true), gOff = R(BotKind.Greedy, false);
            var list = new List<Dictionary<string, object>>();
            void Add(string id, string metric, string target, string type, object value, bool? pass)
            {
                list.Add(new Dictionary<string, object>
                {
                    ["id"] = id, ["metric"] = metric, ["target"] = target, ["type"] = type,
                    ["value"] = value, ["result"] = pass == null ? "OLCULMEDI" : pass.Value ? "GECTI" : "KALDI",
                });
            }
            string F(double x, int d = 2) => x.ToString("F" + d, CultureInfo.InvariantCulture);

            var onRuns = runs.Where(r => r.DifficultyOn).ToList();
            long s1 = runs.Sum(r => r.S1Violations);
            long trays = runs.Sum(r => r.TotalTrays);
            Add("S1", "Yeni tepside hic sigan yok orani", "= 0", "Kesin", $"{s1}/{trays}", trays > 0 ? s1 == 0 : (bool?)null);
            long tr = runs.Sum(r => r.TrayRuleViolations);
            Add("V2", "Tepsi kurallari (ayni kimlik <= 2, mod B siniri)", "= 0", "Dogrulama", $"{tr}/{trays}", trays > 0 ? tr == 0 : (bool?)null);
            long s2 = onRuns.Sum(r => r.S2Violations), relief = onRuns.Sum(r => r.ModeTrays[(int)DifficultyMode.Relief]);
            Add("S2", "RAHATLATMA'da <2 sigan orani", "= 0", "Kesin", $"{s2}/{relief}", onRuns.Count > 0 ? s2 == 0 : (bool?)null);

            long detC = runs.Sum(r => r.DeterminismChecked), detM = runs.Sum(r => r.DeterminismMismatch);
            long resC = runs.Sum(r => r.ResumeChecked), resM = runs.Sum(r => r.ResumeMismatch);
            Add("S3", "Determinizm + anlik goruntuden devam", "%100", "Kesin",
                $"tekrar {detC - detM}/{detC}, devam {resC - resM}/{resC}", detC + resC > 0 ? detM + resM == 0 : (bool?)null);

            long acc = runs.Sum(r => r.ScoreSumMismatch + r.CellsFormulaMismatch + r.StatsMismatch + r.GreedyPredMismatch + r.ZeroLegalNotOver);
            long mv = runs.Sum(r => r.TotalMoves);
            Add("S4", "Puan muhasebesi + kesisim formulu", "%100", "Kesin", $"{acc} hata / {mv} hamle", mv > 0 ? acc == 0 : (bool?)null);

            if (rOn != null) Add("S5", "Rastgele - ortalama hamle", "20-60", "Tahmin", F(rOn.MeanMoves), rOn.MeanMoves >= 20 && rOn.MeanMoves <= 60);
            else Add("S5", "Rastgele - ortalama hamle", "20-60", "Tahmin", null, null);
            if (gOn != null)
            {
                Add("S6", "Acgozlu - ortalama hamle", "150-800", "Tahmin", F(gOn.MeanMoves), gOn.MeanMoves >= 150 && gOn.MeanMoves <= 800);
                double ratio = rOn != null ? gOn.MeanScore / rOn.MeanScore : double.NaN;
                Add("S7", "Acgozlu/rastgele ortalama skor", ">= 5", "Tahmin", F(ratio), rOn != null ? ratio >= 5 : (bool?)null);
                double early = 100.0 * gOn.EarlyEnd / gOn.Games;
                Add("S8", "Acgozlu <= 9 hamlede biten", "<= %2", "Tahmin", F(early) + "%", early <= 2);
                double cap = 100.0 * gOn.Capped / gOn.Games;
                Add("S9", "Acgozlu bitmedi (tavan)", "<= %1", "Tahmin", F(cap) + "%", cap <= 1);
                double rel = gOn.ModeShare(DifficultyMode.Relief), open = gOn.ModeShare(DifficultyMode.Open);
                Add("S10", "Acgozlu mod paylari", "RAHAT %10-35, ACIK %10-40", "Tahmin", $"RAHAT {F(rel)}%, ACIK {F(open)}%, NORMAL {F(gOn.ModeShare(DifficultyMode.Normal))}%",
                    rel >= 10 && rel <= 35 && open >= 10 && open <= 40);
                Add("S11", "Acgozlu patlatan hamle orani", "%20-45", "Tahmin", F(gOn.ClearingRate) + "%", gOn.ClearingRate >= 20 && gOn.ClearingRate <= 45);
                Add("S12", "Acgozlu en yuksek kombo medyani", ">= 3", "Tahmin", F(gOn.MedianMaxCombo, 0), gOn.MedianMaxCombo >= 3);
                double r13 = gOff != null ? gOn.MeanMoves / gOff.MeanMoves : double.NaN;
                Add("S13", "Zorluk acik/kapali acgozlu hamle orani", "1,15-2,0", "Tahmin", F(r13, 3), gOff != null ? r13 >= 1.15 && r13 <= 2.0 : (bool?)null);
            }

            // S14: tum kosulardaki NORMAL tepsiler (onarim haric) birlikte.
            var freq = new long[37];
            long slots = 0;
            foreach (var r in runs) { for (int i = 0; i < 37; i++) freq[i] += r.NormalNoRepairFreq[i]; slots += r.NormalNoRepairSlots; }
            if (slots > 0)
            {
                double worst = 0; string wid = "";
                for (int i = 0; i < 37; i++)
                {
                    double d = Math.Abs(100.0 * freq[i] / slots - 100.0 * PieceCatalog.All[i].BaseWeight / PieceCatalog.TotalBaseWeight);
                    if (d > worst) { worst = d; wid = PieceCatalog.All[i].Id; }
                }
                Add("S14", "NORMAL parca frekansi - taban/124", "<= 1,5 yp (her parca)", "Kesin", $"en buyuk {F(worst, 3)} yp ({wid}), {slots} yuva", worst <= 1.5);
            }

            var gen = runs.SelectMany(r => r.GenMicros).ToList();
            double genMean = gen.Count > 0 ? gen.Average() : double.NaN;
            bool? s15 = gen.Count > 0 && !double.IsNaN(pureSeconds) ? genMean < 100 && pureSeconds < 60 : (bool?)null;
            Add("S15", "Tepsi uretimi ort. < 0,1 ms; 2.000 oyun < 60 s", "< 100 us; < 60 s", "Kesin",
                $"ort {F(genMean, 2)} us ({gen.Count} tepsi); saf kosu {pureGames} oyun {F(pureSeconds)} s", s15);

            int tvSeeds = thinVerify.Sum(t => (int)t["seeds"]), tvSame = thinVerify.Sum(t => (int)t["identical"]);
            long shC = runs.Sum(r => r.ShadowCheckedGames), shM = runs.Sum(r => r.ShadowMismatchGames);
            Add("V1", "Ince dongu (zorluk acik) = GameState", "%100", "Dogrulama",
                $"bagimsiz {tvSame}/{tvSeeds}; golge {shC - shM}/{shC}", tvSeeds + shC > 0 ? tvSame == tvSeeds && shM == 0 : (bool?)null);
            return list;
        }

        private static void PrintRun(RunAgg a)
        {
            string P(List<double> s) => $"ort {s.Average():F1} med {RunAgg.Percentile(s, 50):F0} p10 {RunAgg.Percentile(s, 10):F0} p90 {RunAgg.Percentile(s, 90):F0} p99 {RunAgg.Percentile(s, 99):F0}";
            Console.WriteLine();
            Console.WriteLine($"== {a.Name} ({a.Engine}) {a.Games} oyun, {a.WallSeconds:F2} s");
            Console.WriteLine("  hamle : " + P(a.Moves.Select(x => (double)x).OrderBy(x => x).ToList()));
            Console.WriteLine("  skor  : " + P(a.Scores.Select(x => (double)x).OrderBy(x => x).ToList()));
            Console.WriteLine("  kombo : " + P(a.MaxCombos.Select(x => (double)x).OrderBy(x => x).ToList()));
            Console.WriteLine($"  patlatan hamle %{a.ClearingRate:F1}; tavan {a.Capped}; <=9 hamle {a.EarlyEnd}; mod ACIK %{a.ModeShare(DifficultyMode.Open):F1} NORMAL %{a.ModeShare(DifficultyMode.Normal):F1} RAHAT %{a.ModeShare(DifficultyMode.Relief):F1}");
            Console.WriteLine($"  S1 {a.S1Violations} S2 {a.S2Violations}; hatalar: {a.Errors.Count}");
            foreach (string e in a.Errors.Take(5)) Console.WriteLine("    ! " + e);
        }

        // ---------- Komut satiri ----------

        private static Options Parse(string[] args)
        {
            var o = new Options();
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i];
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException(a + " bir deger bekler.");
                switch (a)
                {
                    case "-h": case "--help": return null;
                    case "--games": o.Games = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--seed-start": o.SeedStart = ulong.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--max-moves": o.MaxMoves = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--out": o.Out = Next(); break;
                    case "--no-verify": o.Verify = false; break;
                    case "--no-timing": o.TimingPass = false; break;
                    case "--rerun-games": o.StraightRerunGames = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--bot":
                        o.Bots = ParseBots(Next());
                        break;
                    case "--sweep": o.Sweep = true; break;
                    case "--sweep-bots": o.SweepBots = ParseBots(Next()); break;
                    case "--difficulty":
                        string dv = Next().ToLowerInvariant();
                        o.Difficulty = dv switch
                        {
                            "on" => new List<bool> { true },
                            "off" => new List<bool> { false },
                            "all" => new List<bool> { true, false },
                            _ => throw new ArgumentException("--difficulty on|off|all"),
                        };
                        break;
                    default: throw new ArgumentException("Bilinmeyen secenek: " + a);
                }
            }
            if (o.Games < 1) throw new ArgumentException("--games >= 1 olmali.");
            if (o.MaxMoves < 1) throw new ArgumentException("--max-moves >= 1 olmali.");
            return o;
        }

        private static List<BotKind> ParseBots(string v)
        {
            var list = new List<BotKind>();
            foreach (string part in v.ToLowerInvariant().Split(','))
            {
                switch (part.Trim())
                {
                    case "random": list.Add(BotKind.Random); break;
                    case "greedy": list.Add(BotKind.Greedy); break;
                    case "smart": list.Add(BotKind.Smart); break;
                    case "planner": list.Add(BotKind.Planner); break;
                    case "all": list.Add(BotKind.Random); list.Add(BotKind.Greedy); break;
                    default: throw new ArgumentException("bot: random | greedy | smart | all (virgulle liste olabilir)");
                }
            }
            return list.Distinct().ToList();
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"Kullanim: dotnet run -c Release --project tools/Blockbloom.Sim -- [secenekler]
  --games N          bot x zorluk basina oyun sayisi (varsayilan 1000)
  --seed-start S     ilk tohum; tohumlar S..S+N-1 (varsayilan 1)
  --bot B            random | greedy | smart | all; virgulle liste (varsayilan all = random,greedy)
                     smart, planner: GDD disi, bilgi amacli guclu botlar (S kriterlerine girmez;
                     planner tum tepsiyi arar, yavastir)
  --difficulty D     on | off | all (varsayilan all). on: GameState; off: ince dongu, her tepsi NORMAL
  --max-moves M      oyun basina hamle tavani (varsayilan 5000); asan oyun 'bitmedi' sayilir
  --out PATH         JSON ozet dosyasi (varsayilan docs/reports/m0-balance.json)
  --no-verify        golge karsilastirma, determinizm ve anlik goruntu denetimlerini atla
  --no-timing        S15 icin saf zamanlama kosusunu atla
  --rerun-games K    duz tekrar ve ince dongu dogrulamasi icin ilk K tohum (varsayilan 100)
  --sweep            ek olarak zorluk parametresi duyarlilik taramasi (Core degismez; ince dongu + varyant profiller)
  --sweep-bots B     tarama botlari (varsayilan random,greedy,smart)
Cikis kodu: 0 = kesin kriterler gecti, 1 = bir kesin kriter kaldi, 2 = arguman hatasi.");
        }
    }
}
