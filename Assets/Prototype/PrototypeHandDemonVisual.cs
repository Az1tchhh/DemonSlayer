using UnityEngine;

namespace DemonSlayer.Prototype
{
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PrototypeHandDemonVisual : MonoBehaviour
    {
        [SerializeField] private PrototypeDemon demon;
        [SerializeField, Min(.1f)] private float walkStrideLength = 1.5f;
        [SerializeField, Min(.01f)] private float idleDuration = 1.4f;
        [SerializeField, Min(.01f)] private float hitFlashDuration = .12f;
        [SerializeField, Min(.01f)] private float deathFadeDuration = .7f;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int WalkState = Animator.StringToHash("Base Layer.Walk");
        private static readonly int AttackState = Animator.StringToHash("Base Layer.Attack");
        private Animator animator;
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private Color baseColor;
        private float idleTime, walkPhase;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            if (!demon || !animator.runtimeAnimatorController)
            {
                Debug.LogError("Hand Demon visual requires an enemy and animation controller.", this);
                enabled = false;
                return;
            }
            body = demon.GetComponent<Rigidbody2D>();
            baseColor = visual.color;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 0;
        }

        private void OnEnable() { if (demon) demon.ResetPerformed += ResetVisual; }
        private void OnDisable() { if (demon) demon.ResetPerformed -= ResetVisual; }

        private void LateUpdate()
        {
            idleTime += Time.deltaTime;
            bool walking = demon.isActiveAndEnabled && !demon.IsKnockedBack && Mathf.Abs(body.linearVelocity.x) > .1f;
            walkPhase = walking ? Mathf.Repeat(walkPhase + Mathf.Abs(body.linearVelocity.x) * Time.deltaTime / walkStrideLength, 1f) : 0;
            int state = IdleState;
            float progress = Mathf.Repeat(idleTime / idleDuration, 1f);
            Color color = baseColor;
            if (demon.Health.IsDead)
            {
                progress = 0;
                color.a *= 1 - Mathf.Clamp01((Time.time - demon.Health.DeathTime) / deathFadeDuration);
            }
            else
            {
                if (demon.IsAttacking) { state = AttackState; progress = demon.AttackProgress; }
                else if (walking) { state = WalkState; progress = walkPhase; }
                if (Time.time - demon.Health.LastDamageTime < hitFlashDuration)
                    color = new Color(1f, .45f, .45f, baseColor.a);
            }
            animator.Play(state, 0, progress);
            animator.Update(0);
            visual.flipX = demon.Facing < 0;
            visual.color = color;
        }

        private void ResetVisual()
        {
            idleTime = walkPhase = 0;
            animator.Rebind();
            animator.Play(IdleState, 0, 0);
            animator.Update(0);
            visual.flipX = demon.Facing < 0;
            visual.color = baseColor;
        }
    }
}
