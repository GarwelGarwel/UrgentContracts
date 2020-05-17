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

        public static UrgentContractsSettings Instance => HighLogic.CurrentGame.Parameters.CustomParams<UrgentContractsSettings>();

        [GameParameters.CustomParameterUI("Mod Enabled", toolTip = "Turn Urgent Contracts on/off")]
        public bool ModEnabled = true;

        [GameParameters.CustomFloatParameterUI("Randomization", toolTip = "How longer deadlines can be than the minimum duration (more is easier)", displayFormat = "N2", minValue = 0, maxValue = 1)]
        public float RandomFactor = 0.5f;

        [GameParameters.CustomIntParameterUI("Additional Grace Days", toolTip = "How many days to add to each deadline", minValue = 0, maxValue = 500)]
        public int AddGraceDays = 0;

        [GameParameters.CustomParameterUI("Debug Mode", toolTip = "Log everything to help Garwel see what the mod's doing wrong")]
        public bool DebugMode = false;
    }
}
