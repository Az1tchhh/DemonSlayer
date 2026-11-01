using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeZenitsuAssist : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private PrototypeCompanionSelection companions;
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(1)] private int damage = 60;
        [SerializeField, Min(.85f)] private float cooldown = 8f;
        [SerializeField, Min(0)] private float distance = 5f;
        [SerializeField] private LayerMask environmentMask = 1;
        public const float LaunchTime = .22f;
        public const float ContactTime = .28f;
        public const float DashEndTime = .38f;
        public const float Duration = .85f;
        private static readonly int StrikeState = Animator.StringToHash("Base Layer.Assist");
        private readonly RaycastHit2D[] hits = new RaycastHit2D[32];
        private InputActionAsset controls;
        private InputAction summon;
        private CombatHealth health;
        private Collider2D playerShape;
        private Rigidbody2D playerBody;
        private Vector2 origin;
        private float facing, travel, started, nextUse, previousElapsed;
        private bool active, hitApplied;
        public bool IsActive => active;
        public float Remaining => Mathf.Max(0, nextUse - Time.time);

        private void Awake()
        {
            // The Animator belongs to the assigned visual; recover a cleared scene reference.
            if (!animator && visual) animator = visual.GetComponent<Animator>();
            if (!player || !inputTemplate || !animator || !visual || !animator.runtimeAnimatorController)
            {
                Debug.LogError("Zenitsu assist requires player, input, renderer and animation references.", this);
                enabled = false;
                return;
            }
            health = player.GetComponent<CombatHealth>();
            playerShape = player.GetComponent<Collider2D>();
            playerBody = player.GetComponent<Rigidbody2D>();
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
            // Swept contact covers the whole elapsed strike window even at low frame rates.
            if (!hitApplied && elapsed >= ContactTime && previousElapsed <= DashEndTime)
                Strike(PositionAt(previousElapsed < ContactTime ? LaunchTime : previousElapsed), PositionAt(Mathf.Min(elapsed, DashEndTime)));
            visual.transform.position = PositionAt(elapsed);
            animator.Play(StrikeState, 0, Mathf.Clamp01(elapsed / Duration));
            animator.Update(0);
            visual.flipX = facing < 0;
            visual.color = new Color(1, 1, 1, Mathf.Min(Mathf.Clamp01(elapsed / .06f), Mathf.Clamp01((Duration - elapsed) / .15f)));
            previousElapsed = elapsed;
            if (elapsed >= Duration) Cancel();
        }

        public bool TrySummon()
        {
            if (companions && (!companions.isActiveAndEnabled || companions.Selected != PrototypeCompanionSelection.Companion.Zenitsu || companions.HasActiveAssist)) return false;
            if (!isActiveAndEnabled || !player.isActiveAndEnabled || health.IsDead || player.IsBeingRescued || active ||
                Time.time < nextUse || Time.timeScale <= 0 || !player.Grounded ||
                Mathf.Abs(playerBody.linearVelocity.y) > .5f) return false;
            facing = player.Facing;
            origin = new Vector2(player.transform.position.x, playerShape.bounds.min.y);
            travel = distance;
            // The helper is visual-only. Query the existing 2D world so its strike stops at walls.
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = environmentMask, useTriggers = false };
            int count = Physics2D.BoxCast(origin + Vector2.up * .8f, new Vector2(.5f, 1.4f), 0,
                new Vector2(facing, 0), filter, hits, distance);
            for (int i = 0; i < count; i++)
            {
                var shape = hits[i].collider;
                if (shape.GetComponentInParent<PrototypePlayer>() || shape.GetComponentInParent<PrototypeDemon>()) continue;
                travel = Mathf.Min(travel, Mathf.Max(0, hits[i].distance - .05f));
            }
            started = Time.time;
            nextUse = started + cooldown;
            previousElapsed = 0;
            active = true;
            hitApplied = false;
            visual.transform.position = origin;
            visual.flipX = facing < 0;
            visual.color = Color.clear;
            visual.enabled = true;
            animator.Play(StrikeState, 0, 0);
            animator.Update(0);
            return true;
        }

        private Vector2 PositionAt(float elapsed) => origin + new Vector2(facing * travel *
            Mathf.Clamp01((elapsed - LaunchTime) / (DashEndTime - LaunchTime)), 0);

        private void Strike(Vector2 from, Vector2 to)
        {
            if (travel <= .01f) return;
            var filter = new ContactFilter2D { useTriggers = false };
            int count = Physics2D.BoxCast(from + Vector2.up * .85f, new Vector2(.5f, 1.5f), 0,
                new Vector2(facing, 0), filter, hits, Mathf.Abs(to.x - from.x));
            PrototypeDemon target = null;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var demon = hits[i].collider.GetComponentInParent<PrototypeDemon>();
                if (!demon || demon.Health.IsDead || (demon.transform.position.x - origin.x) * facing < 0) continue;
                if (hits[i].distance < nearest) { nearest = hits[i].distance; target = demon; }
            }
            if (!target) return;
            hitApplied = true;
            target.Health.Damage(damage);
        }

        private void Cancel()
        {
            active = false;
            if (visual) { visual.enabled = false; visual.color = Color.white; }
        }
        private void ResetAssist()
        {
            Cancel();
            nextUse = 0;
        }
    }
}
