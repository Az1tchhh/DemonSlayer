using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DemonSlayer.Prototype
{
    public sealed class PrototypeInosukeAssist : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField] private PrototypeCompanionSelection companions;
        [SerializeField] private InputActionAsset inputTemplate;
        [SerializeField] private Animator animator;
        [SerializeField] private SpriteRenderer visual;
        [SerializeField, Min(1)] private int damagePerHit = 15;
        [SerializeField, Min(1)] private float cooldown = 8f;
        [SerializeField, Min(.1f)] private float reach = 2.5f;
        [SerializeField] private LayerMask environmentMask = 1;
        public const float FirstContact = .24f;
        public const float HitInterval = .16f;
        public const int HitCount = 3;
        public const float Duration = 1f;
        private static readonly int FlurryState = Animator.StringToHash("Base Layer.Assist");
        private readonly Collider2D[] targets = new Collider2D[64];
        private readonly RaycastHit2D[] obstacles = new RaycastHit2D[32];
        private readonly HashSet<PrototypeDemon> hitDemons = new HashSet<PrototypeDemon>();
        private InputActionAsset controls;
        private InputAction summon;
        private CombatHealth health;
        private Collider2D playerShape;
        private Rigidbody2D body;
        private Vector2 origin;
        private float facing, started, nextUse;
        private int nextHit;
        private bool active;
        public bool IsActive => active;
        public float Remaining => Mathf.Max(0, nextUse - Time.time);

        private void Awake()
        {
            if (!animator && visual) animator = visual.GetComponent<Animator>();
            if (!player || !companions || !inputTemplate || !animator || !visual || !animator.runtimeAnimatorController)
            {
                Debug.LogError("Inosuke assist requires player, selection, input and an animated Inosuke visual.", this);
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
            while (nextHit < HitCount && elapsed >= FirstContact + nextHit * HitInterval)
            {
                Strike();
                nextHit++;
            }
            animator.Play(FlurryState, 0, Mathf.Clamp01(elapsed / Duration));
            animator.Update(0);
            visual.flipX = facing < 0;
            visual.color = new Color(1, 1, 1, Mathf.Min(Mathf.Clamp01(elapsed / .06f), Mathf.Clamp01((Duration - elapsed) / .16f)));
            if (elapsed >= Duration) Cancel();
        }
        public bool TrySummon()
        {
            if (!isActiveAndEnabled || !player.isActiveAndEnabled || health.IsDead || player.IsBeingRescued || active || Time.time < nextUse ||
                Time.timeScale <= 0 || !player.Grounded || Mathf.Abs(body.linearVelocity.y) > .5f ||
                !companions.isActiveAndEnabled || companions.Selected != PrototypeCompanionSelection.Companion.Inosuke || companions.HasActiveAssist) return false;
            facing = player.Facing;
            origin = new Vector2(player.transform.position.x, playerShape.bounds.min.y);
            started = Time.time;
            nextUse = started + cooldown;
            nextHit = 0;
            active = true;
            visual.transform.position = origin;
            visual.flipX = facing < 0;
            visual.color = Color.clear;
            visual.enabled = true;
            animator.Play(FlurryState, 0, 0);
            animator.Update(0);
            return true;
        }
        private void Strike()
        {
            // Recheck each impact: moving out of the planted flurry avoids later hits.
            Vector2 center = origin + new Vector2(facing * reach * .5f, 1f);
            int count = Physics2D.OverlapBox(center, new Vector2(reach + .4f, 2f), 0,
                new ContactFilter2D { useTriggers = false }, targets);
            hitDemons.Clear();
            for (int i = 0; i < count; i++)
            {
                var target = targets[i].GetComponentInParent<PrototypeDemon>();
                if (!target || target.Health.IsDead || (target.transform.position.x - origin.x) * facing < 0 || hitDemons.Contains(target)) continue;
                Vector2 from = origin + Vector2.up;
                Vector2 to = targets[i].ClosestPoint(from);
                int walls = Physics2D.Linecast(from, to, new ContactFilter2D {
                    useLayerMask = true, layerMask = environmentMask, useTriggers = false }, obstacles);
                bool blocked = false;
                for (int j = 0; j < walls; j++)
                    if (!obstacles[j].collider.GetComponentInParent<PrototypePlayer>() && !obstacles[j].collider.GetComponentInParent<PrototypeDemon>())
                    { blocked = true; break; }
                if (blocked) continue;
                hitDemons.Add(target);
                target.Health.Damage(damagePerHit);
            }
        }
        private void Cancel()
        {
            active = false;
            hitDemons.Clear();
            if (visual) { visual.enabled = false; visual.color = Color.white; }
        }
        private void ResetAssist() { Cancel(); nextUse = 0; }
    }
}
