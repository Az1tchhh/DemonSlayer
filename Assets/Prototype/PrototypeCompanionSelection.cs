using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeCompanionSelection : MonoBehaviour
    {
        public enum Companion { Zenitsu, Inosuke, Nezuko }
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private PrototypeZenitsuAssist zenitsu;
        [SerializeField] private PrototypeInosukeAssist inosuke;
        [SerializeField] private PrototypeNezukoAssist nezuko;
        [SerializeField] private Companion selected = Companion.Zenitsu;
        [SerializeField, Min(0)] private float combatDistance = 9f;
        private readonly Collider2D[] nearby = new Collider2D[64];
        private InputActionAsset controls;
        private InputAction selectZenitsu, selectInosuke, selectNezuko;
        private CombatHealth health;
        public Companion Selected => selected;
        public bool HasActiveAssist => (zenitsu && zenitsu.IsActive) || (inosuke && inosuke.IsActive) || (nezuko && nezuko.IsActive);
        public float Remaining => selected == Companion.Zenitsu ? zenitsu.Remaining : selected == Companion.Inosuke ? inosuke.Remaining : nezuko.Remaining;
        public string DisplayName => selected == Companion.Zenitsu ? "ZENITSU" : selected == Companion.Inosuke ? "INOSUKE" : "NEZUKO";
        public bool SelectionLocked { get; private set; }

        private void Awake()
        {
            if (!player || !inputTemplate || !zenitsu || !inosuke || !nezuko)
            {
                Debug.LogError("Companion selection requires player, input and all three assists.", this);
                enabled = false;
                return;
            }
            health = player.GetComponent<CombatHealth>();
            controls = Instantiate(inputTemplate);
            selectZenitsu = controls.FindAction("Player/Previous", true);
            selectInosuke = controls.FindAction("Player/Next", true);
            selectNezuko = controls.FindAction("Player/SelectNezuko", true);
        }
        private void OnEnable() { selectZenitsu?.Enable(); selectInosuke?.Enable(); selectNezuko?.Enable(); }
        private void OnDisable() { selectZenitsu?.Disable(); selectInosuke?.Disable(); selectNezuko?.Disable(); }
        private void OnDestroy() { if (controls) Destroy(controls); }
        private void Update()
        {
            SelectionLocked = IsCombatLocked();
            if (selectZenitsu.WasPressedThisFrame()) TrySelect(Companion.Zenitsu);
            else if (selectInosuke.WasPressedThisFrame()) TrySelect(Companion.Inosuke);
            else if (selectNezuko.WasPressedThisFrame()) TrySelect(Companion.Nezuko);
        }
        public bool TrySelect(Companion companion)
        {
            if (!isActiveAndEnabled || (companion != Companion.Zenitsu && companion != Companion.Inosuke && companion != Companion.Nezuko)) return false;
            SelectionLocked = IsCombatLocked();
            if (SelectionLocked) return false;
            // Each assist owns its cooldown; switching never resets either one.
            selected = companion;
            return true;
        }
        private bool IsCombatLocked()
        {
            if (!player.isActiveAndEnabled || health.IsDead || Time.timeScale <= 0 || player.IsAttacking || player.IsBeingRescued || HasActiveAssist) return true;
            int count = Physics2D.OverlapCircle(player.transform.position, combatDistance,
                new ContactFilter2D { useTriggers = false }, nearby);
            for (int i = 0; i < count; i++)
            {
                var demon = nearby[i].GetComponentInParent<PrototypeDemon>();
                if (demon && !demon.Health.IsDead) return true;
            }
            return false;
        }
    }
}
