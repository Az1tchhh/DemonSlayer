using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeNezukoAssist : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private PrototypeCompanionSelection companions;
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(1)] private int damage = 40;
        [SerializeField, Min(0)] private float knockbackSpeed = 10f;
        [SerializeField, Min(.01f)] private float knockbackDuration = .25f;
        [SerializeField, Min(1)] private float cooldown = 8f;
        [SerializeField, Min(.1f)] private float reach = 2.3f;
        [SerializeField] private LayerMask environmentMask = 1;
        public const float ContactTime = .32f;
        public const float Duration = .95f;
        private static readonly int KickState = Animator.StringToHash("Base Layer.Assist");
        private readonly Collider2D[] targets = new Collider2D[64];
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[32];
        private InputActionAsset controls;
        private InputAction summon;
        private CombatHealth health;
        private Collider2D playerShape;
        private Rigidbody2D body;
        private Vector2 origin;
        private float facing, started, nextUse;
        private bool impactPending;
        private bool active;
        public bool IsActive => active;
        public float Remaining => Mathf.Max(0, nextUse - Time.time);

        private void Awake()
        {
            if (!animator && visual) animator = visual.GetComponent<Animator>();
            if (!player || !companions || !inputTemplate || !animator || !visual || !animator.runtimeAnimatorController)
            {
                Debug.LogError("Nezuko assist requires player, selection, input and an animated Nezuko visual.", this);
                enabled = false;
                return;
            }
            health = player.GetComponent<CombatHealth>();
            playerShape = player.GetComponent<Collider2D>();
            body = player.GetComponent<Rigidbody2D>();
            controls = Instantiate(inputTemplate);
            summon = controls.FindAction("Player/Assist", true);
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0;
            visual.enabled = false;
        }
        private void OnEnable()
        {
            summon?.Enable();
            if (player) player.ResetPerformed += ResetAssist;
            if (health) health.Died += Cancel;
        }
        private void OnDisable()
        {
            summon?.Disable();
            if (player) player.ResetPerformed -= ResetAssist;
            if (health) health.Died -= Cancel;
            Cancel();
        }
        private void OnDestroy() { if (controls) Destroy(controls); }
        private void Update()
        {
            if (!player.isActiveAndEnabled || health.IsDead) { Cancel(); return; }
            if (summon.WasPressedThisFrame()) TrySummon();
            if (!active) return;
            float elapsed = Time.time - started;
            if (impactPending && elapsed >= ContactTime)
            {
                impactPending = false;
                Strike();
            }
            animator.Play(KickState, 0, Mathf.Clamp01(elapsed / Duration));
            animator.Update(0);
            visual.flipX = facing < 0;
            visual.color = new Color(1, 1, 1, Mathf.Min(Mathf.Clamp01(elapsed / .06f), Mathf.Clamp01((Duration - elapsed) / .16f)));
            if (elapsed >= Duration) Cancel();
        }
        public bool TrySummon()
        {
            if (!isActiveAndEnabled || !player.isActiveAndEnabled || health.IsDead || player.IsBeingRescued || active || Time.time < nextUse ||
                Time.timeScale <= 0 || !player.Grounded || Mathf.Abs(body.linearVelocity.y) > .5f ||
                !companions.isActiveAndEnabled || companions.Selected != PrototypeCompanionSelection.Companion.Nezuko || companions.HasActiveAssist) return false;
            facing = player.Facing;
            origin = new Vector2(player.transform.position.x, playerShape.bounds.min.y);
            started = Time.time;
            nextUse = started + cooldown;
            impactPending = true;
            active = true;
            visual.transform.position = origin;
            visual.flipX = facing < 0;
            visual.color = Color.clear;
            visual.enabled = true;
            animator.Play(KickState, 0, 0);
            animator.Update(0);
            return true;
        }
        private void Strike()
        {
            Vector2 from = origin + Vector2.up;
            Vector2 center = from + new Vector2(facing * reach * .5f, 0);
            int count = Physics2D.OverlapBox(center, new Vector2(reach + .4f, 1.8f), 0,
                new ContactFilter2D { useTriggers = false }, targets);
            PrototypeDemon nearest = null;
            float nearestDistance = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var target = targets[i].GetComponentInParent<PrototypeDemon>();
                if (!target || target.Health.IsDead || (target.transform.position.x - origin.x) * facing < 0) continue;
                Vector2 to = targets[i].ClosestPoint(from);
                float distance = Vector2.Distance(from, to);
                if (distance >= nearestDistance) continue;
                int walls = Physics2D.Linecast(from, to, new ContactFilter2D {
                    useLayerMask = true, layerMask = environmentMask, useTriggers = false }, obstacles);
                bool blocked = false;
                for (int j = 0; j < walls; j++)
                    if (!obstacles[j].collider.GetComponentInParent<PrototypePlayer>() && !obstacles[j].collider.GetComponentInParent<PrototypeDemon>())
                    { blocked = true; break; }
                if (blocked) continue;
                nearest = target;
                nearestDistance = distance;
            }
            if (!nearest) return;
            nearest.Health.Damage(damage);
            nearest.ApplyKnockback(facing, knockbackSpeed, knockbackDuration);
        }
        private void Cancel()
        {
            active = false;
            impactPending = false;
            if (visual) { visual.enabled = false; visual.color = Color.white; }
        }
        private void ResetAssist() { Cancel(); nextUse = 0; }
    }
}
