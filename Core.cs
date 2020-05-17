using System;
using System.Collections.Generic;
using UnityEngine;

namespace UrgentContracts
{
    /// <summary>
    /// Provides general static methods and fields for UrgentContracts
    /// </summary>
    public static class Core
    {
        static bool loaded = false;

        public static void LoadConfig()
        {
            if (loaded) return;
            Log("Loading config...", LogLevel.Important);

            BodyTravelTimes = new Dictionary<CelestialBody, double>(FlightGlobals.Bodies.Count);
            CelestialBody homePlanet = GetPlanet(Planetarium.fetch.Home);
            foreach (CelestialBody body in FlightGlobals.Bodies)
            {
                if (body.isHomeWorld)
                    BodyTravelTimes[body] = 0;
                else if (body == Planetarium.fetch.Sun)
                    BodyTravelTimes[body] = homePlanet.orbit.period;
                else if (body.HasParent(homePlanet))
                    BodyTravelTimes[body] = body.orbit.period / 5;
                else BodyTravelTimes[body] = TimeBetweenLaunchWindows(homePlanet.orbit, GetPlanet(body).orbit) + HohmannMultiplier * HohmannTransferTime(homePlanet.orbit, GetPlanet(body).orbit);
                Core.Log("Travel time for " + body.name + " is " + KSPUtil.PrintDateDeltaCompact(BodyTravelTimes[body], true, false), LogLevel.Important);
            }

            ConfigNode[] cfgArray = GameDatabase.Instance.GetConfigNodes("URGENT_CONTRACTS_CONFIG");
            foreach (ConfigNode cfg in cfgArray)
            {
                foreach (ConfigNode n in cfg.GetNodes("CONTRACT_RULE"))
                {
                    ContractRule rule = new ContractRule(n);
                    ContractRules.Add(rule);
                    Core.Log("Added contract from config file: " + rule, LogLevel.Important);
                }

                foreach (ConfigNode n in cfg.GetNodes("TRAVEL_TIME"))
                {
                    CelestialBody body = FlightGlobals.GetBodyByName(GetString(n, "Name"));
                    if (body == null)
                        continue;
                    BodyTravelTimes[body] = n.HasValue("TravelTime") ? GetDouble(n, "TravelTime") : (GetDouble(n, "TravelDays") * 21600);
                    Core.Log("Overriding travel time for " + body.name + " to be " + KSPUtil.PrintDateDeltaCompact(BodyTravelTimes[body], true, false), LogLevel.Important);
                }
            }
            if (cfgArray.Length == 0)
                Core.Log("Config file not found!", LogLevel.Error);

            loaded = true;
        }

        public static List<ContractRule> ContractRules = new List<ContractRule>();

        public static Dictionary<CelestialBody, double> BodyTravelTimes { get; set; }

        /// <summary>
        /// Max allowed difference between max and min deadlines (0.5 = 50% etc.)
        /// </summary>
        //public static double RandomFactor { get; set; } = 0.5;

        /// <summary>
        /// Multiply Hohmann transfer time by this for min travel time
        /// </summary>
        public static double HohmannMultiplier { get; set; } = 1.2;

        public static bool IsPlanet(CelestialBody body) => body?.orbit?.referenceBody == Sun.Instance.sun;

        /// <summary>
        /// Returns average time between two Hohmann transfer windows between two planets
        /// </summary>
        /// <param name="o1"></param>
        /// <param name="o2"></param>
        /// <returns></returns>
        public static double TimeBetweenLaunchWindows(Orbit o1, Orbit o2)
        {
            double pMin, pMax;
            if (o1.period < o2.period)
            {
                pMin = o1.period;
                pMax = o2.period;
            }
            else
            {
                pMin = o2.period;
                pMax = o1.period;
            }
            if (pMin == pMax)
                return 0;  // Periods are the same, so Hohmann transfer is impossible
            return pMin / (1 - pMin / pMax);
        }

        public static double HohmannTransferTime(Orbit o1, Orbit o2)
            => Math.PI * Math.Sqrt(Math.Pow(o1.radius + o2.radius, 3) / (8 * Planetarium.fetch.Sun.gravParameter));

        public static CelestialBody GetPlanet(CelestialBody body)
            => ((body == null) || IsPlanet(body)) ? body : GetPlanet(body?.orbit?.referenceBody);

        public static string GetString(ConfigNode n, string key, string defaultValue = null) => n.HasValue(key) ? n.GetValue(key) : defaultValue;

        public static double GetDouble(ConfigNode n, string key, double defaultValue = 0)
        {
            double res;
            try { res = Double.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static int GetInt(ConfigNode n, string key, int defaultValue = 0)
        {
            int res;
            try { res = Int32.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        public static bool GetBool(ConfigNode n, string key, bool defaultValue = false)
        {
            bool res;
            try { res = Boolean.Parse(n.GetValue(key)); }
            catch (Exception) { res = defaultValue; }
            return res;
        }

        /// <summary>
        /// Parses UT into a string (e.g. "2 d 3 h 15 m 59 s"), hides zero elements
        /// </summary>
        /// <param name="time">Time in seconds</param>
        /// <param name="showSeconds">If false, seconds will be displayed only if time is less than 1 minute; otherwise always</param>
        /// <param name="daysTimeLimit">If time is longer than this number of days, time value will be skipped; -1 to alwys show time</param>
        /// <returns></returns>
        public static string ParseUT(double time, bool showSeconds = true, int daysTimeLimit = -1)
        {
            if (Double.IsNaN(time) || (time == 0))
                return "—";
            if (time > KSPUtil.dateTimeFormatter.Year * 10)
                return "10y+";
            double t = time;
            int y, d, m, h;
            string res = "";
            bool show0 = false;
            if (t >= KSPUtil.dateTimeFormatter.Year)
            {
                y = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Year);
                t -= y * KSPUtil.dateTimeFormatter.Year;
                res += y + " y ";
                show0 = true;
            }
            if ((t >= KSPUtil.dateTimeFormatter.Day) || (show0 && (t >= 1)))
            {
                d = (int)Math.Floor(t / KSPUtil.dateTimeFormatter.Day);
                t -= d * KSPUtil.dateTimeFormatter.Day;
                res += d + " d ";
                show0 = true;
            }
            if ((daysTimeLimit == -1) || (time < KSPUtil.dateTimeFormatter.Day * daysTimeLimit))
            {
                if ((t >= 3600) || show0)
                {
                    h = (int)Math.Floor(t / 3600);
                    t -= h * 3600;
                    res += h + " h ";
                    show0 = true;
                }
                if ((t >= 60) || show0)
                {
                    m = (int)Math.Floor(t / 60);
                    t -= m * 60;
                    res += m + " m ";
                }
                if ((time < 60) || (showSeconds && (Math.Floor(t) > 0)))
                    res += t.ToString("F0") + " s";
            }
            else if (time < KSPUtil.dateTimeFormatter.Day)
                res = "0 d";
            return res.TrimEnd();
        }

        /// <summary>
        /// Mod-wide random number generator
        /// </summary>
        public static System.Random rand = new System.Random();

        /// <summary>
        /// Log levels:
        /// <list type="bullet">
        /// <item><definition>None: do not log</definition></item>
        /// <item><definition>Error: log only errors</definition></item>
        /// <item><definition>Important: log only errors and important information</definition></item>
        /// <item><definition>Debug: log all information</definition></item>
        /// </list>
        /// </summary>
        public enum LogLevel { None, Error, Important, Debug };

        /// <summary>
        /// Current <see cref="LogLevel"/>: either Debug or Important
        /// </summary>
        public static LogLevel Level => UrgentContractsSettings.Instance.DebugMode ? LogLevel.Debug : LogLevel.Important;

        /// <summary>
        /// Returns true if current logging allows logging of messages at messageLevel
        /// </summary>
        /// <param name="messageLevel"></param>
        /// <returns></returns>
        public static bool IsLogging(LogLevel messageLevel = LogLevel.Debug) => messageLevel <= Level;

        /// <summary>
        /// Write into output_log.txt
        /// </summary>
        /// <param name="message">Text to log</param>
        /// <param name="messageLevel"><see cref="LogLevel"/> of the entry</param>
        public static void Log(string message, LogLevel messageLevel = LogLevel.Debug)
        {
            if (IsLogging(messageLevel) && (message.Length != 0))
                Debug.Log("[UrgentContracts] " + (messageLevel == LogLevel.Error ? "ERROR: " : "") + message);
        }
    }
}
