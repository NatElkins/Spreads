﻿// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Spreads.Utils
{
    public class ChaosMonkeyException : Exception
    {
    }

    /// <summary>
    /// When CHAOS_MONKEY conditional-compilation directive is set,
    /// calling the methods will raise an error with a given probability
    /// </summary>
    public static class ChaosMonkey
    {
#if CHAOS_MONKEY
        public const bool Enabled = true;
#else
        public const bool Enabled = false;
#endif

        [ThreadStatic]
        private static Random _rng;

        private static readonly Dictionary<string, object> _traceData = new Dictionary<string, object>();
        private static bool _force;
        private static int _scenario;

        // Forces any exception regardless of probability
        public static bool Force
        {
            get { return _force; }
            set { Volatile.Write(ref _force, value); }
        }

        public static int Scenario
        {
            get { return _scenario; }
            set { Volatile.Write(ref _scenario, value); }
        }

        public static Dictionary<string, object> TraceData => _traceData;

        [Conditional("CHAOS_MONKEY")]
        public static void SetTraceData(string key, object value)
        {
            _traceData[key] = value;
        }

        [Conditional("CHAOS_MONKEY")]
        public static void OutOfMemory(double probability = 0.0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (_rng == null) _rng = new Random();
            if (_rng.NextDouble() > probability) return;
            Force = false;
            throw new OutOfMemoryException();
        }

        [Conditional("CHAOS_MONKEY")]
        public static void StackOverFlow(double probability = 0.0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (_rng == null) _rng = new Random();
            if (_rng.NextDouble() > probability) return;
            Force = false;
#if NET451
            throw new StackOverflowException();
#else
            throw new ChaosMonkeyException();
#endif
        }

        [Conditional("CHAOS_MONKEY")]
        public static void Exception(double probability = 0.0, int scenario = 0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (scenario > 0 && scenario != Scenario) return;
            if (scenario > 0 && scenario == Scenario) Scenario = 0;
            if (_rng == null) _rng = new Random();
            if (_rng.NextDouble() > probability) return;
            Force = false;
            throw new ChaosMonkeyException();
        }

        [Conditional("CHAOS_MONKEY")]
        public static void ThreadAbort(double probability = 0.0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (_rng == null) _rng = new Random();
            if (_rng.NextDouble() > probability) return;
            Force = false;
#if NET451
            Thread.CurrentThread.Abort();
#else
            throw new ChaosMonkeyException();
#endif
        }

        [Conditional("CHAOS_MONKEY")]
        public static void Slowpoke(double probability = 0.0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (_rng == null) _rng = new Random();
            if (_rng.NextDouble() > probability) return;
            Force = false;
            Thread.Sleep(50);
        }

        [Conditional("CHAOS_MONKEY")]
        public static void Chaos(double probability = 0.0)
        {
            if (Force) { probability = 1.0; }
            if (probability == 0.0) return;
            if (_rng == null) _rng = new Random();
            var rn = _rng.NextDouble();
            if (rn > probability) return;
            Force = false;
            if (rn < probability / 3.0)
            {
                throw new OutOfMemoryException();
            }
            if (rn < probability * 2.0 / 3.0)
            {
#if NET451
            throw new StackOverflowException();
#else
                throw new ChaosMonkeyException();
#endif
            }
#if NET451
            Thread.CurrentThread.Abort();
#else
            throw new ChaosMonkeyException();
#endif
        }
    }
}