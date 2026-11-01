using System.Collections.Generic;
using UnityEngine;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeGiyuProtection : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private SpriteRenderer waterEffects;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private LineRenderer entranceFlash;
        [SerializeField] private LineRenderer entranceCore;
        [SerializeField, Min(1)] private float cooldown = 12f;
        [SerializeField, Min(.1f)] private float triggerRange = 4f;
        [SerializeField, Min(.1f)] private float knockbackRadius = 4f;
        [SerializeField, Min(0)] private float knockbackSpeed = 12f;
        [SerializeField, Min(.01f)] private float knockbackDuration = .85f;
        [SerializeField, Range(0, 75)] private float knockbackAngle = 45f;
        [SerializeField, Min(0)] private float retreatDistance = 3.5f;
        [SerializeField] private LayerMask environmentMask = 1;
        public const float Duration = 1.4f;
        public const float EntranceDuration = .12f;
        public const float FirstSwingTime = .28f;
        public const float CounterTime = .62f;
        public const float RetreatTime = .82f;
        public const float RetreatDuration = .4f;
        private static readonly int ProtectionState = Animator.StringToHash("Base Layer.Protection");
        private readonly Collider2D[] targets = new Collider2D[64];
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[32];
        private readonly HashSet<PrototypeDemon> pushed = new HashSet<PrototypeDemon>();
        private CombatHealth health;
        private Collider2D playerShape;
        private Vector2 origin, entranceStart;
        private float facing, started, nextUse;
        private bool active, counterPending;
        public bool IsActive => active && isActiveAndEnabled && player && player.isActiveAndEnabled &&
            health && !health.IsDead && Time.time < started + Duration;
        public float Remaining => Mathf.Max(0, nextUse - Time.time);

        private void Awake()
        {
            if (!animator && visual) animator = visual.GetComponent<Animator>();
            if (!player || !animator || !visual || !waterEffects || !animator.runtimeAnimatorController || !gameplayCamera || !entranceFlash || !entranceCore)
            {
                Debug.LogError("Giyu protection requires Tanjiro, an animated visual, water effects, gameplay camera and entrance streak renderers.", this);
                enabled = false;
                return;
            }
            health = player.GetComponent<CombatHealth>();
            playerShape = player.GetComponent<Collider2D>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0;
            visual.enabled = false;
            waterEffects.enabled = false;
            entranceFlash.enabled = entranceCore.enabled = false;
        }
        private void OnEnable()
        {
            if (player) player.ResetPerformed += ResetProtection;
            if (health) health.Died += CancelProtection;
        }
        private void OnDisable()
        {
            if (player) player.ResetPerformed -= ResetProtection;
            if (health) health.Died -= CancelProtection;
            CancelProtection();
        }
        // Called only by Tanjiro's incoming melee-hit path, before health is changed.
        public bool TryProtect(Vector2 sourcePosition)
        {
            if (!isActiveAndEnabled || !player.isActiveAndEnabled || health.IsDead || Time.timeScale <= 0) return false;
            if (IsActive) return true;
            if (Time.time < nextUse || Vector2.Distance(playerShape.ClosestPoint(sourcePosition), sourcePosition) > triggerRange) return false;
            Vector2 from = playerShape.bounds.center;
            if (WallBetween(from, sourcePosition)) return false;
            float delta = sourcePosition.x - player.transform.position.x;
            facing = Mathf.Abs(delta) > .01f ? Mathf.Sign(delta) : player.Facing;
            origin = new Vector2(player.transform.position.x, playerShape.bounds.min.y);
            Vector3 destination = origin + Vector2.right * facing * .65f;
            Vector3 screen = gameplayCamera.WorldToViewportPoint(destination);
            // Enter from behind Tanjiro, beyond the current camera edge, in either facing direction.
            entranceStart = gameplayCamera.ViewportToWorldPoint(new Vector3(facing > 0 ? -.12f : 1.12f, screen.y, screen.z));
            started = Time.time;
            nextUse = started + cooldown;
            active = counterPending = true;
            player.BeginRescueRetreat(-facing, retreatDistance, RetreatTime, RetreatDuration);
            visual.enabled = true;
            SampleVisual(0);
            return true;
        }
        private void Update()
        {
            if (!active) return;
            if (!IsActive) { CancelProtection(); return; }
            float elapsed = Time.time - started;
            if (counterPending && elapsed >= CounterTime)
            {
                counterPending = false;
                Counter();
            }
            SampleVisual(elapsed);
        }
        private void SampleVisual(float elapsed)
        {
            // Stand between the attacker and Tanjiro, then escort his physical retreat.
            Vector2 feet = elapsed < RetreatTime ? origin : new Vector2(player.transform.position.x, playerShape.bounds.min.y);
            Vector2 destination = feet + Vector2.right * facing * .65f;
            float arrival = Mathf.Clamp01(elapsed / EntranceDuration);
            Vector2 position = Vector2.Lerp(entranceStart, destination, 1 - Mathf.Pow(1 - arrival, 3));
            visual.transform.position = position;
            SampleEntrance(elapsed, position);
            visual.flipX = facing < 0;
            visual.color = new Color(1, 1, 1, Mathf.Clamp01((Duration - elapsed) / .12f));
            animator.Play(ProtectionState, 0, Mathf.Clamp01(elapsed / Duration));
            animator.Update(0);
            // The same clip samples the separate water layer, so pauses and pose changes stay aligned.
            waterEffects.flipX = visual.flipX;
            waterEffects.color = visual.color;
            waterEffects.enabled = visual.enabled && waterEffects.sprite != null && elapsed < RetreatTime;
        }
        private void SampleEntrance(float elapsed, Vector2 position)
        {
            float alpha = elapsed < EntranceDuration ? 1 : Mathf.Clamp01(1 - (elapsed - EntranceDuration) / .08f);
            entranceFlash.enabled = entranceCore.enabled = alpha > 0;
            if (alpha <= 0) return;
            Vector3 from = entranceStart + Vector2.up;
            Vector3 to = position + Vector2.up;
            entranceFlash.SetPosition(0, from);
            entranceFlash.SetPosition(1, to);
            entranceCore.SetPosition(0, from);
            entranceCore.SetPosition(1, to);
            entranceFlash.startColor = new Color(.15f, .65f, 1, alpha * .3f);
            entranceFlash.endColor = new Color(.45f, .95f, 1, alpha);
            entranceCore.startColor = new Color(1, 1, 1, alpha * .2f);
            entranceCore.endColor = new Color(1, 1, 1, alpha);
        }
        private void Counter()
        {
            Vector2 center = playerShape.bounds.center;
            int count = Physics2D.OverlapCircle(center, knockbackRadius, new ContactFilter2D { useTriggers = false }, targets);
            pushed.Clear();
            for (int i = 0; i < count; i++)
            {
                var demon = targets[i].GetComponentInParent<PrototypeDemon>();
                if (!demon || !demon.isActiveAndEnabled || demon.Health.IsDead || pushed.Contains(demon) || WallBetween(center, targets[i].ClosestPoint(center))) continue;
                pushed.Add(demon);
                float delta = demon.transform.position.x - center.x;
                demon.ApplyKnockback(Mathf.Abs(delta) > .01f ? Mathf.Sign(delta) : facing, knockbackSpeed, knockbackDuration, knockbackAngle);
            }
        }
        private bool WallBetween(Vector2 from, Vector2 to)
        {
            int count = Physics2D.Linecast(from, to,
                new ContactFilter2D { useLayerMask = true, layerMask = environmentMask, useTriggers = false }, obstacles);
            for (int i = 0; i < count; i++)
                if (!obstacles[i].collider.GetComponentInParent<PrototypePlayer>() && !obstacles[i].collider.GetComponentInParent<PrototypeDemon>()) return true;
            return false;
        }
        public void CancelProtection()
        {
            active = counterPending = false;
            if (player) player.CancelRescueRetreat();
            if (visual) { visual.enabled = false; visual.color = Color.white; }
            if (waterEffects) { waterEffects.enabled = false; waterEffects.sprite = null; waterEffects.color = Color.white; }
            if (entranceFlash) entranceFlash.enabled = false;
            if (entranceCore) entranceCore.enabled = false;
        }
        private void ResetProtection() { CancelProtection(); nextUse = 0; }
    }
}
