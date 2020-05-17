using System;
using System.Collections.Generic;
using System.Reflection;
using FinePrint.Contracts;
using FinePrint.Contracts.Parameters;
using Contracts;
using Contracts.Templates;
using System.Text.RegularExpressions;

namespace UrgentContracts
{
    public class ContractRule
    {
        /// <summary>
        /// Type of the contract class
        /// </summary>
        public List<string> Types { get; set; } = new List<string>();

        public void AddTypes(string types)
        {
            string[] parts = types.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            foreach (string part in parts) Types.Add(part);
        }

        /// <summary>
        /// List of regex expressions that contract title should match
        /// </summary>
        public List<string> Titles { get; set; } = new List<string>();

        /// <summary>
        /// Checks if this rule should apply to Contract c
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool AppliesTo(Contract c)
        {
            bool r = Types.Exists(x => x.Equals(c.GetType().Name.ToString(), StringComparison.CurrentCultureIgnoreCase));
            if ((r || (Types.Count == 0)) && (Titles.Count > 0))
            {
                foreach (string t in Titles)
                    if (new Regex(t).IsMatch(c.Title))
                    {
                        Core.Log("Match found for '" + t + "'!");
                        return true;
                    }
                    else Core.Log("No match for '" + t + "'.");
                return false;
            }
            return r;
        }
        
        public double GracePeriod { get; set; } = 21600;

        /// <summary>
        /// How much time is added to deadline based on body travel time (0 if no travel needed, 1 for one-way, 2 for return)
        /// </summary>
        public double TravelTimeMultiplier { get; set; } = 1;

        /// <summary>
        /// Fetches travel time for current body from cache (or 0 if no b is null)
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public static double GetTravelTime(CelestialBody b) => (b != null) ? Core.BodyTravelTimes[b] : 0;

        /// <summary>
        /// Returns min allowed deadline for this contract (including grace period and travel time)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public double GetMinDeadline(Contract c) => GracePeriod + UrgentContractsSettings.Instance.AddGraceDays * 21600 + TravelTimeMultiplier * GetTravelTime(GetTargetBody(c));

        public void CheckAndApply(Contract c, double chance = 1)
        {
            double minDeadline = GetMinDeadline(c);
            double d = c.TimeDeadline / minDeadline;
            if (((d < 1) || (d > 1 + UrgentContractsSettings.Instance.RandomFactor)) && (Core.rand.NextDouble() < chance))
            {
                d = minDeadline * (1 + Core.rand.NextDouble() * UrgentContractsSettings.Instance.RandomFactor) + UrgentContractsSettings.Instance.AddGraceDays * 21600;
                double m = 1;
                if (d >= 21600) m = 60;
                if (d >= 21600 * 10) m = 3600;
                if (d >= 21600 * 426) m = 21600;
                d = Math.Round(d / m) * m;
                Core.Log("Deadline for " + c.GetType().Name + " (\"" + c.Title + "\") adjusted from " + KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, false) + " to " + KSPUtil.PrintDateDeltaCompact(d, true, false), Core.LogLevel.Important);
                c.TimeDeadline = d;
            }
            else Core.Log("Deadline is fine.");
        }

        /// <summary>
        /// Returns target celestial body of the contract
        /// Implementation based on code from Contract Parser by DMagic
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static CelestialBody GetTargetBody(Contract c)
        {
            Type t = c.GetType();
            try
            {
                if (t == typeof(CollectScience))
                    return ((CollectScience)c).TargetBody;
                else if (t == typeof(PartTest))
                    return typeof(PartTest).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue((PartTest)c) as CelestialBody;
                else if (t == typeof(PlantFlag))
                    return ((PlantFlag)c).TargetBody;
                else if (t == typeof(RecoverAsset))
                    return typeof(RecoverAsset).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue((RecoverAsset)c) as CelestialBody;
                else if (t == typeof(GrandTour))
                    return ((GrandTour)c).TargetBodies[((GrandTour)c).TargetBodies.Count - 1];
                else if (t == typeof(ARMContract))
                    return typeof(ARMContract).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[0].GetValue((ARMContract)c) as CelestialBody;
                else if (t == typeof(BaseContract))
                    return ((BaseContract)c).targetBody;
                else if (t == typeof(ISRUContract))
                    return ((ISRUContract)c).targetBody;
                else if (t == typeof(SatelliteContract))
                    return c.GetParameter<SpecificOrbitParameter>()?.TargetBody;
                else if (t == typeof(StationContract))
                    return ((StationContract)c).targetBody;
                else if (t == typeof(SurveyContract))
                    return ((SurveyContract)c).targetBody;
                else if (t == typeof(TourismContract))
                    return null;
                else if (t == typeof(ExplorationContract))
                    return typeof(ExplorationContract).GetFields(BindingFlags.NonPublic | BindingFlags.Instance)[1].GetValue((ExplorationContract)c) as CelestialBody;
            }
            catch (Exception e) { Core.Log("Exception " + e + " when detecting CelestialBody of contract '" + c.Title + "'.", Core.LogLevel.Error); }

            // Uknown contract type => look for body name in the title
            Core.Log("Couldn't detect CelestialBody from contract parameters, trying to find its name in the title (" + c.Title + ") or description.");
            foreach (CelestialBody b in FlightGlobals.Bodies)
                if (new System.Text.RegularExpressions.Regex("\\b" + b.name + "\\b").IsMatch(c.Title)) return b;

            // No body name in title => look for it in the description
            foreach (CelestialBody b in FlightGlobals.Bodies)
                if (new System.Text.RegularExpressions.Regex("\\b" + b.name + "\\b").IsMatch(c.Description)) return b;

            Core.Log("CelestialBody could not be detected.");
            return null;
        }

        public override string ToString()
        {
            string res = "Types = { ";
            bool needComma = false;
            foreach (string t in Types)
            {
                if (needComma) res += ", ";
                res += t;
                needComma = true;
            }
            res += " } GracePeriod = '" + KSPUtil.PrintDateDeltaCompact(GracePeriod, true, false) + "' TravelTimeMultiplier = " + TravelTimeMultiplier.ToString("N1");
            return res;
        }

        public ContractRule() { }
        public ContractRule(string type) => AddTypes(type);
        public ContractRule(string type, double graceDays, double bodyTravelTimeMultiplier = 1)
        {
            AddTypes(type);
            GracePeriod = graceDays * 21600;
            TravelTimeMultiplier = bodyTravelTimeMultiplier;
        }

        public ContractRule(ConfigNode node)
        {
            foreach (string v in node.GetValues("Type"))
                AddTypes(v);
            foreach (string v in node.GetValues("Title"))
                Titles.Add(v);
            GracePeriod = node.HasValue("GraceTime") ? Core.GetDouble(node, "GraceTime") : (Core.GetDouble(node, "GraceDays") * 21600);
            TravelTimeMultiplier = Core.GetDouble(node, "TravelTimeMultiplier", 1);
        }
    }
}
