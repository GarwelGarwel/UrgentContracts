using Contracts;
using System.Collections.Generic;

namespace UrgentContracts
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.SPACECENTER)]
    public class UrgentContractsScenario : ScenarioModule
    {
        public void Start()
        {
            Core.Log("Start()");
            Core.LoadConfig();
            GameEvents.Contract.onOffered.Add(OnContractOffered);
        }

        public void OnDisable()
        {
            GameEvents.Contract.onOffered.Remove(OnContractOffered);
        }

        bool loaded = false;
        public void Update()
        {
            if (!loaded) ProcessContracts();
        }

        public void OnContractOffered(Contract c)
        {
            Core.Log("OnContractOffered(" + c.GetType() + ")");
            LogContractInfo(c);
            ProcessContract(c);
        }

        void LogContractInfo(Contract c)
        {
            Core.Log("Title: " + c.Title);
            Core.Log("Type: " + c.GetType().Name);
            Core.Log("State: " + c.ContractState);
            Core.Log("Target body: " + (ContractRule.GetTargetBody(c)?.name ?? "N/A"));
            Core.Log("Deadline: " + KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, true));
        }

        void ProcessContract(Contract c)
        {
            foreach (ContractRule rule in Core.ContractRules)
                if (rule.AppliesTo(c))
                    if (rule.CheckAndApply(c))
                        Core.Log("Deadline for " + c.GetType().Name + " adjusted to " + KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, true), Core.LogLevel.Important);
                    else Core.Log("Deadline is fine.");
        }

        void ProcessContracts()
        {
            if (!ContractSystem.loaded)
            {
                Core.Log("ContractSystem not yet loaded.");
                return;
            }
            Core.Log(ContractSystem.Instance.Contracts.Count + " total contracts.", Core.LogLevel.Important);
            for (int i = 0; i < ContractSystem.Instance.Contracts.Count; i++)
            {
                Contract c = ContractSystem.Instance.Contracts[i];
                LogContractInfo(c);
                if (c.ContractState == Contract.State.Offered)
                    ProcessContract(c);
                Core.Log("---");
            }
            loaded = true;
        }
    }
}
