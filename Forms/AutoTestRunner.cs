using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using SIGTool.Configs;
using SIGTool.Datas;
using SIGTool.Forms.UIs;
using SIGTool.Modules.Core;
using SIGTool.Modules.Logics;

namespace SIGTool.Forms
{
    /// <summary>
    /// SIGTool 内置无人值守白盒自动化测试与真机截图引擎（--autotest）：
    /// • 读取 JSON 测试书（TestSpec_Latest.json），在隔离内存态下驱动 SIGEngine 与全部 7 大页签/子窗体；
    /// • 调用原生 Win32 PrintWindow(PW_RENDERFULLCONTENT) 截取真实 GDI 双缓冲画面保存为 PNG（避开 DrawToBitmap 逆序缺陷）；
    /// • 内置顶栏结界数值变动像素差分校验（证明结界数字更新的同时右侧槽位 0 像素位移）与全控件截断/纯灰度巡检；
    /// • 输出 LatestReport.json 与可读总结 LatestSummary.md 供云盘回传及 Web 端 AI 闭环审阅。
    /// </summary>
    public static class AutoTestRunner
    {
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hwnd, IntPtr hdcBlt, uint nFlags);

        private const uint PW_RENDERFULLCONTENT = 0x00000002;

        private static int _lastTruncatedButtons = 0;
        private static int _lastNonGrayscaleColors = 0;
        private static int _lastHeaderBarrierDiffPx = 0;
        private static int _lastHeaderRightDiffPx = 0;

        public static int RunFromCli(string specPath, string outDir)
        {
            var totalSw = Stopwatch.StartNew();
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "QA_Output");
            }
            string shotDir = Path.Combine(outDir, "Screenshots");
            Directory.CreateDirectory(outDir);
            Directory.CreateDirectory(shotDir);

            // 清理上一轮残留截图，确保云盘回传图片始终对应最新一次运行
            foreach (var oldPng in Directory.GetFiles(shotDir, "*.png"))
            {
                try { File.Delete(oldPng); } catch { }
            }

            var report = new AutoTestReportRoot
            {
                ToolVersion = "SIGTool v0.9.2",
                TimestampLocal = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                BuildSucceeded = true
            };

            try
            {
                AutoTestSpecRoot spec = LoadOrCreateDefaultSpec(specPath);
                report.SpecTitle = spec.Title;

                _lastTruncatedButtons = 0;
                _lastNonGrayscaleColors = 0;
                _lastHeaderBarrierDiffPx = 0;
                _lastHeaderRightDiffPx = 0;

                using (var form = new MainSIGForm(isAutoTestMode: true, autoTestSeed: spec.RandomSeed))
                {
                    form.Show();
                    PumpUi(form, 80);

                    if (spec.Steps != null)
                    {
                        for (int i = 0; i < spec.Steps.Count; i++)
                        {
                            var step = spec.Steps[i];
                            var stepSw = Stopwatch.StartNew();
                            string detail;
                            bool stepOk = ExecuteStep(form, step, shotDir, report, out detail);
                            stepSw.Stop();

                            report.StepResults.Add(new AutoTestStepResult
                            {
                                StepId = !string.IsNullOrEmpty(step.StepId) ? step.StepId : ("STEP_" + (i + 1)),
                                Action = step.Action,
                                Ok = stepOk,
                                Detail = detail,
                                ElapsedMs = stepSw.ElapsedMilliseconds
                            });
                        }
                    }

                    // 采集最终内存快照
                    report.MemorySnapshot = BuildMemorySnapshot(form.Engine.Save);

                    // 执行全部断言
                    bool allAssertOk = true;
                    if (spec.Assertions != null)
                    {
                        for (int i = 0; i < spec.Assertions.Count; i++)
                        {
                            var a = spec.Assertions[i];
                            double actual = ReadMetricValue(form.Engine.Save, a.MetricPath);
                            bool pass = CompareValues(actual, a.Op, a.Expected);
                            if (!pass) allAssertOk = false;

                            report.AssertionResults.Add(new AutoTestAssertionResult
                            {
                                MetricPath = a.MetricPath,
                                Op = a.Op,
                                Actual = Math.Round(actual, 4),
                                Expected = a.Expected,
                                Passed = pass,
                                Desc = a.Desc
                            });
                        }
                    }

                    bool allStepsOk = report.StepResults.All(s => s.Ok);
                    report.AllPassed = allStepsOk && allAssertOk;
                    totalSw.Stop();
                    report.TotalElapsedMs = totalSw.ElapsedMilliseconds;
                    report.SummaryMessage = string.Format(
                        "{0}：共执行 {1} 步，断言通过 {2}/{3} 项，生成截图 {4} 张（耗时 {5} ms）。",
                        report.AllPassed ? "[PASS] 自动化测试全绿通过" : "[FAIL] 存在未通过断言或步骤",
                        report.StepResults.Count,
                        report.AssertionResults.Count(x => x.Passed),
                        report.AssertionResults.Count,
                        report.Screenshots.Count,
                        report.TotalElapsedMs);

                    form.AutoTestExit();
                    PumpUi(null, 30);
                }

                WriteReportFiles(outDir, report);
                return report.AllPassed ? 0 : 2;
            }
            catch (Exception ex)
            {
                totalSw.Stop();
                report.AllPassed = false;
                report.TotalElapsedMs = totalSw.ElapsedMilliseconds;
                report.SummaryMessage = "[CRASH] 自动化测试运行异常：" + ex.Message;
                report.StepResults.Add(new AutoTestStepResult
                {
                    StepId = "FATAL",
                    Action = "UnhandledException",
                    Ok = false,
                    Detail = ex.ToString(),
                    ElapsedMs = totalSw.ElapsedMilliseconds
                });
                WriteReportFiles(outDir, report);
                return 3;
            }
        }

        private static bool ExecuteStep(
            MainSIGForm form,
            AutoTestStepSpec step,
            string shotDir,
            AutoTestReportRoot report,
            out string detail)
        {
            detail = "";
            var eng = form.Engine;
            var save = eng.Save;
            string act = (step.Action ?? "").Trim();

            if (string.Equals(act, "ResetNewSave", StringComparison.OrdinalIgnoreCase))
            {
                int seed = step.IntParam != 0 ? step.IntParam : 20261113;
                eng.InitializeForAutoTest(seed);
                form.AutoTestRefreshAll();
                PumpUi(form, 40);
                detail = string.Format("已重置隔离内存新宗门（Seed={0}，结界 {1:F0}/{2:F0}）。",
                    seed, eng.Save.Sect.BarrierCurHp, eng.Save.Sect.BarrierMaxHp);
                return true;
            }

            if (string.Equals(act, "GrantAllResources", StringComparison.OrdinalIgnoreCase))
            {
                double amt = step.Amount > 0 ? step.Amount : 5000.0;
                DevBalanceLogic.GrantAllResources(save, amt, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "GrantAllItems", StringComparison.OrdinalIgnoreCase))
            {
                int cnt = step.IntParam > 0 ? step.IntParam : 5;
                DevBalanceLogic.GrantAllCatalogItems(save, cnt, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "UnlockAndMaxAll", StringComparison.OrdinalIgnoreCase))
            {
                DevBalanceLogic.UnlockAndMaxAllFacilities(save, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "SetLeaderRealm", StringComparison.OrdinalIgnoreCase))
            {
                RealmTier tier;
                if (!Enum.TryParse(step.TargetKey, true, out tier)) tier = RealmTier.Foundation;
                int sub = Math.Max(1, step.IntParam);
                DevBalanceLogic.SetLeaderRealmAndState(save, tier, sub, step.BoolParam, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "FastForwardMonths", StringComparison.OrdinalIgnoreCase))
            {
                int months = Math.Max(1, step.IntParam);
                var sum = eng.FastForwardMonths(months);
                form.AutoTestRefreshAll();
                PumpUi(form, 40);
                detail = string.Format("快进 {0} 个月至 {1}，掌门修为 +{2:F0}，胜场 +{3}。",
                    months, sum.EndDateText, sum.LeaderQiDelta, sum.ExpeditionWinsDelta);
                return true;
            }

            if (string.Equals(act, "ImpulseAcupoint", StringComparison.OrdinalIgnoreCase))
            {
                string acuId = !string.IsNullOrEmpty(step.TargetKey) ? step.TargetKey : "ACU_DU_1";
                int tries = step.IntParam > 0 ? step.IntParam : 10;
                bool ok = MeridianLogic.TryImpulseBatchUntilUnlocked(save, save.Leader, acuId, tries, eng.Rng, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return ok;
            }

            if (string.Equals(act, "TriggerWorldShiftAndEncounter", StringComparison.OrdinalIgnoreCase))
            {
                detail = DevBalanceLogic.TriggerTestEncounterAndShiftTrend(save, eng.Rng, eng.EventBus);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "ResolveFirstEncounter", StringComparison.OrdinalIgnoreCase))
            {
                if (save.World.PendingEncounters.Count == 0)
                {
                    WorldEvolutionLogic.TriggerRandomMonthlyEncounter(save, eng.Rng, eng.EventBus);
                }
                int optIdx = Math.Max(0, step.IntParam);
                WorldEvolutionLogic.ResolveEncounterOption(save, "", optIdx, eng.Rng, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return true;
            }

            if (string.Equals(act, "BuyMarketListing", StringComparison.OrdinalIgnoreCase))
            {
                string lid = !string.IsNullOrEmpty(step.TargetKey) ? step.TargetKey : "MKT_RES_HERB";
                int batch = Math.Max(1, step.IntParam);
                bool ok = WorldEvolutionLogic.TryBuyMarketListing(save, lid, batch, eng.EventBus, out detail);
                form.AutoTestRefreshAll();
                PumpUi(form, 30);
                return ok;
            }

            if (string.Equals(act, "SetTheme", StringComparison.OrdinalIgnoreCase))
            {
                UiThemeMode mode;
                if (!Enum.TryParse(step.TargetKey, true, out mode)) mode = UiThemeMode.MinimalDarkGray;
                form.AutoTestSetTheme(mode);
                PumpUi(form, 60);
                detail = "已切换界面皮肤至 " + mode;
                return true;
            }

            if (string.Equals(act, "VerifyHeaderAntiJitter", StringComparison.OrdinalIgnoreCase))
            {
                return RunHeaderAntiJitterPixelCheck(form, shotDir, report, out detail);
            }

            if (string.Equals(act, "AuditControlLayout", StringComparison.OrdinalIgnoreCase))
            {
                return RunFullLayoutAndGrayscaleAudit(form, report, out detail);
            }

            if (string.Equals(act, "CaptureAllTabs", StringComparison.OrdinalIgnoreCase))
            {
                string suffix = !string.IsNullOrEmpty(step.TargetKey) ? ("_" + step.TargetKey) : "";
                string[] tabSlugs =
                {
                    "Tab1_Cultivation", "Tab2_SectBuild", "Tab3_DiscipleCraft",
                    "Tab4_CombatExplore", "Tab5_ArtifactArray", "Tab6_Meridian", "Tab7_WorldEvolution"
                };
                int count = Math.Min(form.MainTabControl.TabPages.Count, tabSlugs.Length);
                for (int t = 0; t < count; t++)
                {
                    form.AutoTestSelectTab(t);
                    PumpUi(form, 65);
                    string fn = string.Format("{0:D2}_{1}{2}.png", report.Screenshots.Count + 1, tabSlugs[t], suffix);
                    string full = Path.Combine(shotDir, fn);
                    CaptureWindowPng(form, full);
                    report.Screenshots.Add(fn);
                }
                detail = string.Format("已使用 Win32 PrintWindow 完整截取全部 {0} 个页签。", count);
                return true;
            }

            if (string.Equals(act, "CaptureTab", StringComparison.OrdinalIgnoreCase))
            {
                int t = Math.Max(0, Math.Min(form.MainTabControl.TabPages.Count - 1, step.IntParam));
                form.AutoTestSelectTab(t);
                PumpUi(form, 65);
                string fn = !string.IsNullOrEmpty(step.OutputFile)
                    ? step.OutputFile
                    : string.Format("{0:D2}_Tab{1}.png", report.Screenshots.Count + 1, t + 1);
                CaptureWindowPng(form, Path.Combine(shotDir, fn));
                report.Screenshots.Add(fn);
                detail = "已截取页签 #" + (t + 1) + " -> " + fn;
                return true;
            }

            if (string.Equals(act, "CaptureSubForm", StringComparison.OrdinalIgnoreCase))
            {
                Form sub = form.AutoTestOpenSubWindow(step.TargetKey);
                if (sub == null)
                {
                    detail = "未知子窗口标识：" + step.TargetKey;
                    return false;
                }
                PumpUi(sub, 80);
                string fn = !string.IsNullOrEmpty(step.OutputFile)
                    ? step.OutputFile
                    : string.Format("{0:D2}_SubForm_{1}.png", report.Screenshots.Count + 1, step.TargetKey);
                CaptureWindowPng(sub, Path.Combine(shotDir, fn));
                report.Screenshots.Add(fn);
                form.AutoTestCloseSubWindows();
                PumpUi(form, 40);
                detail = "已截取独立子窗口 [" + step.TargetKey + "] -> " + fn;
                return true;
            }

            detail = "未识别的测试动作：" + act;
            return false;
        }

        /// <summary>
        /// 像素级防跳字实证测试：
        /// 将结界由窄数字 "611" 变更为宽数字 "688"，连截两帧真实主窗口并比对顶栏第一行：
        /// 1. 结界槽区域像素必须发生变化（证明画面确实刷新了 611 -> 688）；
        /// 2. 结界右侧【| 兽潮: xx月】及全部顶栏按钮区域像素差异必须严格等于 0！
        /// </summary>
        private static bool RunHeaderAntiJitterPixelCheck(
            MainSIGForm form,
            string shotDir,
            AutoTestReportRoot report,
            out string detail)
        {
            var save = form.Engine.Save;
            double origCur = save.Sect.BarrierCurHp;
            double origMax = save.Sect.BarrierMaxHp;

            save.Sect.BarrierMaxHp = Math.Max(995.0, origMax);
            save.Sect.BarrierCurHp = 611.0;
            form.AutoTestRefreshAll();
            PumpUi(form, 60);

            using (Bitmap bmpA = CaptureWindowBitmap(form))
            {
                save.Sect.BarrierCurHp = 688.0;
                form.AutoTestRefreshAll();
                PumpUi(form, 60);

                using (Bitmap bmpB = CaptureWindowBitmap(form))
                {
                    // 计算主窗体边框偏移（将 Client 坐标换算到整窗 Bitmap 坐标）
                    Point clientOriginOnScreen = form.PointToScreen(Point.Empty);
                    int dx = clientOriginOnScreen.X - form.Bounds.X;
                    int dy = clientOriginOnScreen.Y - form.Bounds.Y;

                    var hdr = form.HeaderBarControl;
                    int rowTop = Math.Max(0, dy + hdr.Top + 2);
                    int rowBot = Math.Min(bmpA.Height - 1, dy + hdr.Top + 28);

                    int barrierStartX = Math.Max(0, dx + hdr.Left + hdr.PlaqueBarrierSlotX);
                    int raidStartX = Math.Max(barrierStartX + 10, dx + hdr.Left + hdr.PlaqueRaidSlotX);
                    int rightEnd = Math.Min(bmpA.Width - 4, dx + form.ClientSize.Width - 4);

                    int barrierDiff = CountDiffPixels(bmpA, bmpB, barrierStartX, rowTop, raidStartX - 2, rowBot);
                    int rightDiff = CountDiffPixels(bmpA, bmpB, raidStartX, rowTop, rightEnd, rowBot);

                    _lastHeaderBarrierDiffPx = barrierDiff;
                    _lastHeaderRightDiffPx = rightDiff;

                    string shotName = string.Format("{0:D2}_AntiJitter_HeaderCheck.png", report.Screenshots.Count + 1);
                    bmpB.Save(Path.Combine(shotDir, shotName), ImageFormat.Png);
                    report.Screenshots.Add(shotName);

                    save.Sect.BarrierCurHp = origCur;
                    save.Sect.BarrierMaxHp = origMax;
                    form.AutoTestRefreshAll();

                    detail = string.Format("结界 611->688 像素比对：结界数字区变更 {0} px，右侧【兽潮+按钮区】位移差异 {1} px。",
                        barrierDiff, rightDiff);
                    return barrierDiff > 0 && rightDiff == 0;
                }
            }
        }

        private static int CountDiffPixels(Bitmap a, Bitmap b, int x1, int y1, int x2, int y2)
        {
            int diff = 0;
            int maxX = Math.Min(Math.Min(a.Width, b.Width) - 1, x2);
            int maxY = Math.Min(Math.Min(a.Height, b.Height) - 1, y2);
            for (int y = Math.Max(0, y1); y <= maxY; y++)
            {
                for (int x = Math.Max(0, x1); x <= maxX; x++)
                {
                    if (a.GetPixel(x, y).ToArgb() != b.GetPixel(x, y).ToArgb())
                    {
                        diff++;
                    }
                }
            }
            return diff;
        }

        private static bool RunFullLayoutAndGrayscaleAudit(
            MainSIGForm form,
            AutoTestReportRoot report,
            out string detail)
        {
            int truncated = 0;
            int nonGray = 0;
            bool checkGray = UiStyleHelper.Current.IsGrayscale;

            for (int t = 0; t < form.MainTabControl.TabPages.Count; t++)
            {
                form.AutoTestSelectTab(t);
                PumpUi(form, 35);
                InspectControlTreeRecursive(form, checkGray, ref truncated, ref nonGray, report.LayoutWarnings);
            }

            _lastTruncatedButtons = truncated;
            _lastNonGrayscaleColors = nonGray;
            detail = string.Format("7 页签全控件树巡检完毕：疑似截断按钮 {0} 处，非纯灰度色值 {1} 处。", truncated, nonGray);
            return truncated == 0 && nonGray == 0;
        }

        private static void InspectControlTreeRecursive(
            Control root,
            bool checkGray,
            ref int truncated,
            ref int nonGray,
            List<string> warnings)
        {
            if (root == null || !root.Visible) return;

            if (checkGray)
            {
                Color bg = root.BackColor;
                if (bg.A > 0 && !(bg.R == bg.G && bg.G == bg.B))
                {
                    nonGray++;
                    if (warnings.Count < 20)
                        warnings.Add(string.Format("[NonGray Bg] {0} ({1}): RGB({2},{3},{4})", root.GetType().Name, root.Text, bg.R, bg.G, bg.B));
                }
            }

            var btn = root as Button;
            if (btn != null && btn.AutoSize && !string.IsNullOrEmpty(btn.Text) && !btn.Text.Contains("\n"))
            {
                if (btn.Right > (btn.Parent != null ? btn.Parent.ClientSize.Width + 6 : 99999))
                {
                    truncated++;
                    if (warnings.Count < 20)
                        warnings.Add(string.Format("[Clipped Button] '{0}' Right={1} > ParentW={2}",
                            btn.Text, btn.Right, btn.Parent != null ? btn.Parent.ClientSize.Width : -1));
                }
            }

            for (int i = 0; i < root.Controls.Count; i++)
            {
                InspectControlTreeRecursive(root.Controls[i], checkGray, ref truncated, ref nonGray, warnings);
            }
        }

        private static void CaptureWindowPng(Form window, string outputPath)
        {
            using (Bitmap bmp = CaptureWindowBitmap(window))
            {
                bmp.Save(outputPath, ImageFormat.Png);
            }
        }

        private static Bitmap CaptureWindowBitmap(Form window)
        {
            int w = Math.Max(10, window.Width);
            int h = Math.Max(10, window.Height);
            var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb);

            bool captured = false;
            using (Graphics g = Graphics.FromImage(bmp))
            {
                IntPtr hdc = g.GetHdc();
                try
                {
                    captured = PrintWindow(window.Handle, hdc, PW_RENDERFULLCONTENT);
                    if (!captured)
                    {
                        captured = PrintWindow(window.Handle, hdc, 0);
                    }
                }
                finally
                {
                    g.ReleaseHdc(hdc);
                }
            }

            if (!captured)
            {
                window.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
            }
            return bmp;
        }

        private static void PumpUi(Control target, int sleepMs)
        {
            Application.DoEvents();
            if (target != null && !target.IsDisposed)
            {
                target.PerformLayout();
                target.Refresh();
            }
            Application.DoEvents();
            if (sleepMs > 0)
            {
                Thread.Sleep(sleepMs);
                Application.DoEvents();
            }
        }

        private static AutoTestMemorySnapshot BuildMemorySnapshot(SIGSaveData save)
        {
            var rCfg = SIGConfigTable.GetRealm(save.Leader.Realm);
            return new AutoTestMemorySnapshot
            {
                SIGYear = save.SIGYear,
                SIGMonth = save.SIGMonth,
                LeaderRealmText = EnumTextMap.RealmFullCn(save.Leader.Realm, save.Leader.SubStage, rCfg.MaxSubStage),
                LeaderCurQi = Math.Round(save.Leader.CurQi, 1),
                LeaderMaxQi = Math.Round(save.Leader.MaxQi, 1),
                LeaderCombatPower = save.Leader.EstimateCombatPower(),
                LeaderAttack = save.Leader.BattleStats.Attack,
                UnlockedAcupoints = save.Leader.UnlockedAcupointIds != null ? save.Leader.UnlockedAcupointIds.Count : 0,
                BarrierCurHp = Math.Round(save.Sect.BarrierCurHp, 1),
                BarrierMaxHp = Math.Round(save.Sect.BarrierMaxHp, 1),
                SpiritStone = Math.Round(save.Sect.GetResource(ResourceType.SpiritStone), 1),
                SpiritHerb = Math.Round(save.Sect.GetResource(ResourceType.SpiritHerb), 1),
                ArtifactVaultCount = save.Sect.ArtifactVault != null ? save.Sect.ArtifactVault.Count : 0,
                WorldTrendCn = save.World != null ? WorldEvolutionLogic.TrendNameCn(save.World.CurrentTrend) : "",
                PendingEncounters = (save.World != null && save.World.PendingEncounters != null) ? save.World.PendingEncounters.Count : 0,
                ResolvedEncounters = save.World != null ? save.World.TotalResolvedEncounters : 0,
                TruncatedButtonCount = _lastTruncatedButtons,
                NonGrayscaleColorCount = _lastNonGrayscaleColors,
                HeaderBarrierDigitsDiffPixels = _lastHeaderBarrierDiffPx,
                HeaderRightRegionDiffPixels = _lastHeaderRightDiffPx
            };
        }

        private static double ReadMetricValue(SIGSaveData save, string path)
        {
            switch (path)
            {
                case "Save.SIGYear": return save.SIGYear;
                case "Save.SIGMonth": return save.SIGMonth;
                case "Save.TotalMonths": return (save.SIGYear - 1) * 12 + save.SIGMonth;
                case "Leader.RealmInt": return (int)save.Leader.Realm;
                case "Leader.SubStage": return save.Leader.SubStage;
                case "Leader.CurQi": return save.Leader.CurQi;
                case "Leader.MaxQi": return save.Leader.MaxQi;
                case "Leader.CombatPower": return save.Leader.EstimateCombatPower();
                case "Leader.Attack": return save.Leader.BattleStats.Attack;
                case "Leader.UnlockedAcupoints": return save.Leader.UnlockedAcupointIds != null ? save.Leader.UnlockedAcupointIds.Count : 0;
                case "Sect.BarrierCurHp": return save.Sect.BarrierCurHp;
                case "Sect.BarrierMaxHp": return save.Sect.BarrierMaxHp;
                case "Sect.BarrierMissingHp": return Math.Max(0.0, save.Sect.BarrierMaxHp - save.Sect.BarrierCurHp);
                case "Sect.ArtifactCount": return save.Sect.ArtifactVault != null ? save.Sect.ArtifactVault.Count : 0;
                case "Sect.Res.SpiritStone": return save.Sect.GetResource(ResourceType.SpiritStone);
                case "Sect.Res.SpiritHerb": return save.Sect.GetResource(ResourceType.SpiritHerb);
                case "World.FactionCount": return (save.World != null && save.World.Factions != null) ? save.World.Factions.Count : 0;
                case "World.MarketCount": return (save.World != null && save.World.MarketSlots != null) ? save.World.MarketSlots.Count : 0;
                case "World.PendingEncounterCount": return (save.World != null && save.World.PendingEncounters != null) ? save.World.PendingEncounters.Count : 0;
                case "World.ResolvedEncounterCount": return save.World != null ? save.World.TotalResolvedEncounters : 0;
                case "UI.TruncatedButtonCount": return _lastTruncatedButtons;
                case "UI.NonGrayscaleColorCount": return _lastNonGrayscaleColors;
                case "UI.HeaderBarrierDigitsDiffPixels": return _lastHeaderBarrierDiffPx;
                case "UI.HeaderRightRegionDiffPixels": return _lastHeaderRightDiffPx;
                default: return double.NaN;
            }
        }

        private static bool CompareValues(double actual, string op, double expected)
        {
            if (double.IsNaN(actual)) return false;
            switch ((op ?? "==").Trim())
            {
                case "==": return Math.Abs(actual - expected) <= 0.001;
                case "!=": return Math.Abs(actual - expected) > 0.001;
                case ">=": return actual >= expected - 0.001;
                case "<=": return actual <= expected + 0.001;
                case ">": return actual > expected;
                case "<": return actual < expected;
                default: return false;
            }
        }

        private static AutoTestSpecRoot LoadOrCreateDefaultSpec(string specPath)
        {
            if (!string.IsNullOrWhiteSpace(specPath) && File.Exists(specPath))
            {
                string raw = File.ReadAllText(specPath, Encoding.UTF8);
                var parsed = SimpleJsonSerializer.Deserialize<AutoTestSpecRoot>(raw);
                if (parsed != null) return parsed;
            }

            return new AutoTestSpecRoot
            {
                SpecVersion = "0.9.2",
                Title = "SIGTool 内置兜底自检测试书",
                RandomSeed = 20261113,
                Steps = new List<AutoTestStepSpec>
                {
                    new AutoTestStepSpec { StepId = "S1", Action = "SetTheme", TargetKey = "MinimalDarkGray" },
                    new AutoTestStepSpec { StepId = "S2", Action = "VerifyHeaderAntiJitter" },
                    new AutoTestStepSpec { StepId = "S3", Action = "AuditControlLayout" },
                    new AutoTestStepSpec { StepId = "S4", Action = "CaptureAllTabs", TargetKey = "Dark" }
                },
                Assertions = new List<AutoTestAssertionSpec>
                {
                    new AutoTestAssertionSpec { MetricPath = "UI.HeaderRightRegionDiffPixels", Op = "==", Expected = 0, Desc = "结界数字跳变时顶栏右侧区域 0 像素位移" },
                    new AutoTestAssertionSpec { MetricPath = "UI.TruncatedButtonCount", Op = "==", Expected = 0, Desc = "全页签按钮无越界截断" }
                }
            };
        }

        private static void WriteReportFiles(string outDir, AutoTestReportRoot report)
        {
            string jsonPath = Path.Combine(outDir, "LatestReport.json");
            File.WriteAllText(jsonPath, SimpleJsonSerializer.Serialize(report, true), new UTF8Encoding(false));

            var md = new StringBuilder();
            md.AppendLine("# SIGTool 自动化测试与真机截图验收报告");
            md.AppendLine();
            md.AppendLine(string.Format("- **执行时间**：{0}", report.TimestampLocal));
            md.AppendLine(string.Format("- **测试书标题**：{0}", report.SpecTitle));
            md.AppendLine(string.Format("- **总判定结果**：**{0}**", report.SummaryMessage));
            md.AppendLine();

            if (report.MemorySnapshot != null)
            {
                var m = report.MemorySnapshot;
                md.AppendLine("## 1. 终态内存快照（In-Process Memory Readout）");
                md.AppendLine();
                md.AppendLine(string.Format("- **仙历进度**：第 {0} 年 {1} 月 | **岁运天象**：{2}", m.SIGYear, m.SIGMonth, m.WorldTrendCn));
                md.AppendLine(string.Format("- **掌门面板**：{0} | 气海 {1}/{2} | 战力 {3} (攻 {4}) | 已通穴位 {5}/24",
                    m.LeaderRealmText, m.LeaderCurQi, m.LeaderMaxQi, m.LeaderCombatPower, m.LeaderAttack, m.UnlockedAcupoints));
                md.AppendLine(string.Format("- **宗门库藏**：护山结界 {0}/{1} | 灵石 {2} | 灵草 {3} | 词条阵宝 {4} 件 | 已决奇遇 {5} 桩",
                    m.BarrierCurHp, m.BarrierMaxHp, m.SpiritStone, m.SpiritHerb, m.ArtifactVaultCount, m.ResolvedEncounters));
                md.AppendLine(string.Format("- **UI 像素与布局体检**：结界区像素更新 `{0} px` | 顶栏右侧防抖位移 `{1} px` | 越界按钮 `{2}` | 非纯灰控件 `{3}`",
                    m.HeaderBarrierDigitsDiffPixels, m.HeaderRightRegionDiffPixels, m.TruncatedButtonCount, m.NonGrayscaleColorCount));
                md.AppendLine();
            }

            md.AppendLine("## 2. 断言核验明细");
            md.AppendLine();
            md.AppendLine("| 状态 | 监控指标 (`MetricPath`) | 运算符 | 期望值 | 实测内存值 | 说明 |");
            md.AppendLine("| :--- | :--- | :--- | :--- | :--- | :--- |");
            for (int i = 0; i < report.AssertionResults.Count; i++)
            {
                var a = report.AssertionResults[i];
                md.AppendLine(string.Format("| {0} | `{1}` | `{2}` | `{3}` | `{4}` | {5} |",
                    a.Passed ? "PASS" : "FAIL", a.MetricPath, a.Op, a.Expected, a.Actual, a.Desc));
            }
            md.AppendLine();

            md.AppendLine("## 3. 步骤流水与截图清单");
            md.AppendLine();
            for (int i = 0; i < report.StepResults.Count; i++)
            {
                var s = report.StepResults[i];
                md.AppendLine(string.Format("- `[{0}]` **{1}** (`{2}`, {3}ms)：{4}",
                    s.Ok ? "OK" : "ERR", s.StepId, s.Action, s.ElapsedMs, s.Detail));
            }
            if (report.Screenshots.Count > 0)
            {
                md.AppendLine();
                md.AppendLine("- **已生成真机 PNG 截图**：" + string.Join(", ", report.Screenshots.ToArray()));
            }

            string mdPath = Path.Combine(outDir, "LatestSummary.md");
            File.WriteAllText(mdPath, md.ToString(), new UTF8Encoding(false));
        }
    }
}
