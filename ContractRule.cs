using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace UrgentContracts
{
    public class ContractRule
    {
        /// <summary>
        /// Type of the contract class
        /// </summary>
        public List<string> Types { get; set; } = new List<string>();

        /// <summary>
        /// List of regex expressions that contract title should match
        /// </summary>
        public List<string> Titles { get; set; } = new List<string>();

        public double GracePeriod { get; set; } = 21600;

        /// <summary>
        /// How much time is added to deadline based on body travel time (0 if no travel needed, 1 for one-way, 2 for return)
        /// </summary>
        public double TravelTimeMultiplier { get; set; } = 1;

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
            Titles = new List<string>(node.GetValues("Title"));
            GracePeriod = node.HasValue("GraceTime") ? node.GetDouble("GraceTime") : (node.GetDouble("GraceDays") * 21600);
            TravelTimeMultiplier = node.GetDouble("TravelTimeMultiplier", 1);
        }

        public void AddTypes(string types) => Types.AddRange(types.Split(", ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

        /// <summary>
        /// Checks if this rule should apply to Contract c
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool AppliesTo(Contract c)
        {
            bool r = Types.Exists(x => x.Equals(c.GetType().Name.ToString(), StringComparison.CurrentCultureIgnoreCase));
            if ((r || Types.Count == 0) && Titles.Count > 0)
                return Titles.Any(t => new Regex(t).IsMatch(c.Title));
            return r;
        }

        /// <summary>
        /// Returns min allowed deadline for this contract (including grace period and travel time)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public double GetMinDeadline(Contract c)
            => GracePeriod + UrgentContractsSettings.Instance.AddGraceDays * 21600 + TravelTimeMultiplier * c.GetTargetBody().GetTravelTimeFromHome();

        public void CheckAndApply(Contract c, double chance = 1)
        {
            double minDeadline = GetMinDeadline(c);
            double d = c.TimeDeadline / minDeadline;
            if (((d < 1) || (d > 1 + UrgentContractsSettings.Instance.RandomFactor)) && (Core.rand.NextDouble() < chance))
            {
                d = minDeadline * (1 + Core.rand.NextDouble() * UrgentContractsSettings.Instance.RandomFactor) + UrgentContractsSettings.Instance.AddGraceDays * 21600;
                double m = 1;
                if (d >= 21600)
                    m = 60;
                if (d >= 21600 * 10)
                    m = 3600;
                if (d >= 21600 * 426)
                    m = 21600;
                d = Math.Round(d / m) * m;
                Core.Log($"Deadline for {c.GetType().Name} (\"{c.Title}\") adjusted from {KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, false)} to {KSPUtil.PrintDateDeltaCompact(d, true, false)}.", LogLevel.Important);
                c.TimeDeadline = d;
            }
            else Core.Log("Deadline is fine.");
        }

        public override string ToString()
        {
            string res = "Types = { ";
            bool needComma = false;
            foreach (string t in Types)
            {
                if (needComma)
                    res += ", ";
                res += t;
                needComma = true;
            }
            res += $" }} GracePeriod = '{KSPUtil.PrintDateDeltaCompact(GracePeriod, true, false)}' TravelTimeMultiplier = {TravelTimeMultiplier:N1}";
            return res;
        }
    }
}
