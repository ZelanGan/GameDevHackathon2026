namespace NightTrolleybus.Vehicle
{
    /// <summary>Несправності тролейбуса. Кожну усуває своя міні-гра.</summary>
    public enum FaultType
    {
        /// <summary>Штанги злетіли з дротів: тролейбус не їде.</summary>
        PolesOff,
        /// <summary>Згорів запобіжник: обігрівач і світло не працюють.</summary>
        FuseBlown,
        /// <summary>Роз'єдналися проводи: енергія витікає.</summary>
        WiresLoose
    }
}
