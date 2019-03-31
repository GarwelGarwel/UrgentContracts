using Contracts;
using FinePrint.Contracts;
using Contracts.Templates;
using System.Collections.Generic;

namespace UrgentContracts
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.SPACECENTER)]
    public class UrgentContractsScenario : ScenarioModule
    {
        List<ContractRule> contractRules;

        public void Start()
        {
            Core.Log("Start()");
            if (!Core.Loaded) Core.LoadConfig();

            contractRules = new List<ContractRule>();
            contractRules.Add(new ContractRule(typeof(SurveyContract), 21600, 21600, 1, ContractRule.PrecisionType.Hours));
            contractRules.Add(new ContractRule(typeof(PartTest), 21600, 21600, 1, ContractRule.PrecisionType.Hours));

            GameEvents.Contract.onContractsListChanged.Add(OnContractsListChanged);
        }

        public void OnDisable()
        {
            GameEvents.Contract.onContractsListChanged.Remove(OnContractsListChanged);
        }

        bool loaded = false;
        public void Update()
        {
            if (!loaded && (ContractSystem.Instance.Contracts.Count > 0)) ProcessContracts();
        }

        public void OnContractsListChanged()
        {
            Core.Log("OnContractsListChanged()");
            ProcessContracts();
        }

        void ProcessContracts()
        {
            Core.Log("UT: " + Planetarium.GetUniversalTime());
            Core.Log(ContractSystem.Instance.Contracts.Count + " total contracts.", Core.LogLevel.Important);
            for (int i = 0; i < ContractSystem.Instance.Contracts.Count; i++)
            {
                Contract c = ContractSystem.Instance.Contracts[i];
                Core.Log("Title: " + c.Title);
                Core.Log("Type: " + c.GetType());
                Core.Log("State: " + c.ContractState);
                Core.Log("Target body: " + (ContractRule.GetTargetBody(c)?.name ?? "N/A"));
                Core.Log("Deadline: " + c.TimeDeadline + " (" + KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, false) + ")");
                foreach (ContractRule rule in contractRules)
                    if (rule.AppliesTo(c))
                        if (rule.NeedsAdjustment(c))
                        {
                            double d = rule.GetDeadline(c);
                            c.TimeDeadline = d;
                            Core.Log("Deadline adjusted to " + d + " (" + KSPUtil.PrintDateDeltaCompact(d, true, false) + ")");
                        }
                        else Core.Log("Deadline is fine.");
                Core.Log("---");
            }
            loaded = true;
        }
    }
}
