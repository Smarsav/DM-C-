using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using DMToCSharp.Core;

namespace DMToCSharp.Runtime.MC
{
    public class MasterController
    {
        public static readonly MasterController Instance = new MasterController();

        public List<DMSubsystem> Subsystems { get; private set; }
        public bool IsRunning { get; private set; }
        public int CurrentIteration { get; private set; }
        public double TargetDeltaTimeMs { get; set; }
        public double AverageTickTimeMs { get; private set; }

        private readonly Stopwatch _clock = new Stopwatch();
        private readonly List<double> _tickTimes = new List<double>();

        public MasterController()
        {
            Subsystems = new List<DMSubsystem>();
            IsRunning = false;
            CurrentIteration = 0;
            TargetDeltaTimeMs = 50.0; // 20 FPS (50ms per tick)
            AverageTickTimeMs = 0.0;
        }

        public void RegisterSubsystem(DMSubsystem ss)
        {
            if (ss == null || Subsystems.Contains(ss)) return;
            Subsystems.Add(ss);
            // Sort by priority (higher priority runs first)
            Subsystems.Sort(delegate(DMSubsystem a, DMSubsystem b)
            {
                return b.Priority.CompareTo(a.Priority);
            });
        }

        public void InitializeAll()
        {
            _clock.Start();
            foreach (var ss in Subsystems)
            {
                if ((ss.SubsystemFlags & SubsystemFlags.NoInit) == 0)
                {
                    ss.Initialize(new DMValue(_clock.ElapsedMilliseconds));
                }
            }
        }

        public void Tick()
        {
            CurrentIteration++;
            double now = _clock.ElapsedMilliseconds;
            Stopwatch tickTimer = Stopwatch.StartNew();

            foreach (var ss in Subsystems)
            {
                if ((ss.SubsystemFlags & SubsystemFlags.NoFire) != 0) continue;
                if (now < ss.NextFireTime) continue;

                Stopwatch ssTimer = Stopwatch.StartNew();
                ss.State = SubsystemState.Running;

                try
                {
                    ss.Fire(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[MC ERROR] Subsystem {0} crashed: {1}", ss.SubsystemName, ex.Message));
                    ss.Recover();
                }

                ssTimer.Stop();
                ss.Cost = ssTimer.Elapsed.TotalMilliseconds;
                ss.LastFireTime = now;
                ss.NextFireTime = now + ss.WaitMilliseconds;
                ss.State = SubsystemState.Idle;
            }

            tickTimer.Stop();
            double tickCost = tickTimer.Elapsed.TotalMilliseconds;

            _tickTimes.Add(tickCost);
            if (_tickTimes.Count > 100) _tickTimes.RemoveAt(0);

            double sum = 0;
            for (int i = 0; i < _tickTimes.Count; i++) sum += _tickTimes[i];
            AverageTickTimeMs = sum / _tickTimes.Count;
        }

        public void RunLoop(int maxIterations = -1, int sleepMs = 10)
        {
            IsRunning = true;
            InitializeAll();

            int iter = 0;
            while (IsRunning)
            {
                Tick();
                iter++;
                if (maxIterations > 0 && iter >= maxIterations)
                {
                    break;
                }
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }
            }

            IsRunning = false;
        }

        public void StopLoop()
        {
            IsRunning = false;
        }

        public string GetDiagnosticsReport()
        {
            List<string> lines = new List<string>();
            lines.Add(string.Format("=== MASTER CONTROLLER REPORT (Iteration {0}) ===", CurrentIteration));
            lines.Add(string.Format("Avg Tick: {0}ms, Subsystems: {1}", AverageTickTimeMs.ToString("F2"), Subsystems.Count));
            foreach (var ss in Subsystems)
            {
                lines.Add("  - " + ss.StatEntry());
            }
            return string.Join(Environment.NewLine, lines.ToArray());
        }
    }
}
