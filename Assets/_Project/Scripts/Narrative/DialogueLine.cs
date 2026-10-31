using UnityEngine;

namespace NightTrolleybus.Narrative
{
    /// <summary>Що спонукає Вогника заговорити.</summary>
    public enum DialogueTrigger
    {
        BecameBright,
        BecameWeak,
        BecameCritical,
        FuelReceived,
        ColdInCabin,
        Frightened,
        Flash,
        PowerOut,
        BusBroken,
        LeftBus,
        RouteDepot,
        RouteSupermarket,
        RouteGunShop,
        RouteEvent,
        RouteEmptyRoad,
        RouteFinal
    }

    /// <summary>Одна репліка Вогника з умовами показу.</summary>
    [CreateAssetMenu(menuName = "Night Trolleybus/Dialogue Line", fileName = "Line")]
    public class DialogueLine : ScriptableObject
    {
        [SerializeField] private DialogueTrigger trigger;
        [TextArea]
        [SerializeField] private string text;
        [Tooltip("Репліка звучить, лише коли сила полум'я в цих межах")]
        [SerializeField] private float minFlame;
        [SerializeField] private float maxFlame = 100f;
        [Tooltip("Сказати не більше одного разу за партію")]
        [SerializeField] private bool onlyOnce;

        public DialogueTrigger Trigger => trigger;
        public string Text => text;
        public bool OnlyOnce => onlyOnce;

        public bool Matches(float flame) => flame >= minFlame && flame <= maxFlame;
    }
}
