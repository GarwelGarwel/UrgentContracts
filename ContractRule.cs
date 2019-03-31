using System;
using System.Reflection;
using FinePrint.Contracts;
using FinePrint.Contracts.Parameters;
using Contracts;
using Contracts.Templates;

namespace UrgentContracts
{
    public class ContractRule
    {
        public enum PrecisionType { Default, Seconds, Minutes, Hours, Days }
        PrecisionType Precision { get; set; } = PrecisionType.Default;

        /// <summary>
        /// Type of the contract class
        /// </summary>
        public Type Type { get; set; }

        /// <summary>
        /// Checks if this rule should apply to Contract c
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool AppliesTo(Contract c) => c.GetType() == Type;

        /// <summary>
        /// Min # of seconds for deadline
        /// </summary>
        public double MinDeadline { get; set; }

        /// <summary>
        /// Max # of seconds for deadline
        /// </summary>
        public double MaxDeadline { get; set; }

        /// <summary>
        /// Min # of days for deadline
        /// </summary>
        public int MinDays
        {
            get => (int) MinDeadline / 21600;
            set => MinDeadline = value * 21600;
        }

        /// <summary>
        /// Max # of days for deadline
        /// </summary>
        public int MaxDays
        {
            get => (int)MaxDeadline / 21600;
            set => MaxDeadline = value * 21600;
        }

        /// <summary>
        /// How much time is added to deadline based on body travel time (0 if no travel needed, 1 for one-way, 2 for return)
        /// </summary>
        public double BodyTravelTimeMultiplier { get; set; } = 1;

        public static double GetBodyTravelTime(CelestialBody b) => (b != null) ? Core.BodyTravelTimes[b] : 0;

        public double GetMinDeadline(Contract c) => MinDeadline + BodyTravelTimeMultiplier * GetBodyTravelTime(GetTargetBody(c));
        public double GetMaxDeadline(Contract c) => MaxDeadline + BodyTravelTimeMultiplier * GetBodyTravelTime(GetTargetBody(c));

        /// <summary>
        /// Checks if Contract c needs its deadlines changed to conform to this rule
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool NeedsAdjustment(Contract c) => (c.TimeDeadline < GetMinDeadline(c)) || (c.TimeDeadline > GetMaxDeadline(c));

        /// <summary>
        /// Returns a random deadline between MinDeadline and MaxDeadline, rounded to Precision
        /// </summary>
        /// <returns></returns>
        public double GetDeadline(Contract c)
        {
            double d = GetMinDeadline(c) + Core.rand.NextDouble() * (GetMaxDeadline(c) - GetMinDeadline(c));
            double m = 1;
            switch (Precision)
            {
                case PrecisionType.Default: return d;
                case PrecisionType.Seconds: return Math.Round(d);
                case PrecisionType.Minutes: m = 60; break;
                case PrecisionType.Hours: m = 3600; break;
                case PrecisionType.Days: m = 21600; break;
            }
            return Math.Round(d / m) * m;
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
            bool checkTitle = false;
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
                else
                    checkTitle = true;
            }
            catch (Exception e)
            {
                Core.Log("Exception " + e + " when detecting CelestialBody of contract " + c.Title + ".", Core.LogLevel.Error);
                return null;
            }

            if (checkTitle)
            {
                Core.Log("Couldn't detect CelestialBody from contract parameters, trying to find its name in the title (" + c.Title + ").");
                foreach (CelestialBody b in FlightGlobals.Bodies)
                    if (new System.Text.RegularExpressions.Regex("\b" + b.displayName + "\b").IsMatch(c.Title)) return b;
            }

            Core.Log("CelestialBody could not be detected.");
            return null;
        }

        public ContractRule() { }
        public ContractRule(Type type) => Type = type;
        public ContractRule(Type type, double minDeadline, double maxDeadline, PrecisionType precision = PrecisionType.Default)
        {
            Type = type;
            MinDeadline = minDeadline;
            MaxDeadline = maxDeadline;
            Precision = precision;
        }
    }
}
