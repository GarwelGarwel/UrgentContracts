using Contracts;
using System.Linq;

namespace UrgentContracts
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.SPACECENTER)]
    public class UrgentContractsScenario : ScenarioModule
    {
        bool loaded = false;

        public void Start()
        {
            Core.Log("Start");
            Core.LoadConfig();
            GameEvents.Contract.onOffered.Add(OnContractOffered);
        }

        public void OnDisable()
        {
            GameEvents.Contract.onOffered.Remove(OnContractOffered);
        }

        public void Update()
        {
            if (!loaded)
                ProcessContracts();
        }

        public void OnContractOffered(Contract c)
        {
            Core.Log($"OnContractOffered({c.GetType()})");
            LogContractInfo(c);
            ProcessContract(c);
        }

        void LogContractInfo(Contract c)
        {
            if (!Core.IsLogging())
                return;
            Core.Log($"Title: {c.Title}");
            Core.Log($"Type: {c.GetType().Name}");
            Core.Log($"State: {c.ContractState}");
            Core.Log($"Target body: {(c.GetTargetBody()?.name ?? "N/A")}");
            Core.Log($"Deadline: {KSPUtil.PrintDateDeltaCompact(c.TimeDeadline, true, true)}");
        }

        void ProcessContract(Contract c)
        {
            foreach (ContractRule rule in Core.ContractRules.Where(rule => rule.AppliesTo(c)))
                rule.CheckAndApply(c);
        }

        void ProcessContracts()
        {
            if (!ContractSystem.loaded)
            {
                Core.Log("ContractSystem not yet loaded.");
                return;
            }
            Core.Log($"{ContractSystem.Instance.Contracts.Count} total contracts.", LogLevel.Important);
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
