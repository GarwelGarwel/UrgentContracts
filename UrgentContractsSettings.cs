namespace UrgentContracts
{
    class UrgentContractsSettings : GameParameters.CustomParameterNode
    {
        public override string Section => "UrgentContracts";
        public override string DisplaySection => "Urgent Contracts";
        public override string Title => "Urgent Contracts Settings";
        public override int SectionOrder => 1;
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.CAREER;
        public override bool HasPresets => false;

        [GameParameters.CustomParameterUI("Mod Enabled", toolTip = "Turn Urgent Contracts on/off")]
        public bool modEnabled = true;
        public static bool ModEnabled => HighLogic.CurrentGame.Parameters.CustomParams<UrgentContractsSettings>().modEnabled;

        [GameParameters.CustomFloatParameterUI("Randomization", toolTip = "How longer deadlines can be than the minimum duration (more is easier)", displayFormat = "N2", minValue = 0, maxValue = 1)]
        public float randomFactor = 0.5f;
        public static float RandomFactor => HighLogic.CurrentGame.Parameters.CustomParams<UrgentContractsSettings>().randomFactor;

        [GameParameters.CustomIntParameterUI("Additional Grace Days", toolTip = "How many days to add to each deadline", minValue = 0, maxValue = 500)]
        public int addGraceDays = 0;
        public static int AddGraceDays => HighLogic.CurrentGame.Parameters.CustomParams<UrgentContractsSettings>().addGraceDays;

        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "Log everything to help Garwel see what the mod's doing wrong")]
        public bool debugMode = false;
        public static bool DebugMode => HighLogic.CurrentGame.Parameters.CustomParams<UrgentContractsSettings>().debugMode;
    }
}
