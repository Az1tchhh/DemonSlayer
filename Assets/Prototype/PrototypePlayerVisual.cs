using UnityEngine;

namespace DemonSlayer.Prototype
{
    // The visual child owns sprite animation only. Physics stays on the player root.
    [RequireComponent(typeof(Animator), typeof(SpriteRenderer))]
    public sealed class PrototypePlayerVisual : MonoBehaviour
    {
        [SerializeField] private PrototypePlayer player;
        [SerializeField, Min(.1f)] private float runStrideLength = 3f;
        [SerializeField, Min(.01f)] private float landingDuration = .12f;
        [SerializeField, Min(.01f)] private float hurtDuration = .2f;
        [SerializeField, Min(.01f)] private float deathDuration = .8f;
        private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
        private static readonly int SlashState = Animator.StringToHash("Base Layer.BasicSlash");
        private static readonly int WaterState = Animator.StringToHash("Base Layer.WaterSlash");
        private static readonly int WheelState = Animator.StringToHash("Base Layer.WaterWheel");
        private static readonly int DanceState = Animator.StringToHash("Base Layer.FlowingDance");
        private static readonly int SkyState = Animator.StringToHash("Base Layer.ClearBlueSky");
        private static readonly int HinokamiState = Animator.StringToHash("Base Layer.HinokamiDance");
        private static readonly int RunState = Animator.StringToHash("Base Layer.Run");
        private static readonly int RiseState = Animator.StringToHash("Base Layer.JumpRise");
        private static readonly int ApexState = Animator.StringToHash("Base Layer.JumpApex");
        private static readonly int FallState = Animator.StringToHash("Base Layer.Fall");
        private static readonly int LandState = Animator.StringToHash("Base Layer.Land");
        private static readonly int HurtState = Animator.StringToHash("Base Layer.Hurt");
        private static readonly int DeathState = Animator.StringToHash("Base Layer.Death");
        private Animator animator;
        private SpriteRenderer visual;
        private Rigidbody2D body;
        private float idleTime;
        private float runPhase, landedAt = float.NegativeInfinity;
        private bool wasGrounded, awaitingFirstGround = true;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            visual = GetComponent<SpriteRenderer>();
            body = player.GetComponent<Rigidbody2D>();
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Sample against gameplay time, so a hit and its pose cannot drift apart.
            animator.speed = 0;
        }

        private void OnEnable() => player.ResetPerformed += ResetVisual;
        private void OnDisable() => player.ResetPerformed -= ResetVisual;

        private void LateUpdate()
        {
            bool alive = !player.Health.IsDead && player.isActiveAndEnabled;
            bool slashing = alive && player.IsAttacking;
            Vector2 velocity = body.linearVelocity;
            // The ground probe still touches the floor on the first jump tick.
            // Require settled vertical motion to avoid a false landing during take-off.
            bool grounded = player.Grounded && Mathf.Abs(velocity.y) < .5f;
            if (alive && grounded && !wasGrounded && !awaitingFirstGround) landedAt = Time.time;
            if (grounded) awaitingFirstGround = false;
            if (!grounded || !alive || slashing) landedAt = float.NegativeInfinity;
            wasGrounded = grounded;
            bool landing = Time.time - landedAt < landingDuration;
            bool running = alive && grounded && Mathf.Abs(velocity.x) > .1f;
            bool idle = alive && grounded && !running && !slashing && !landing;
            idleTime = idle ? idleTime + Time.deltaTime : 0;
            runPhase = running ? Mathf.Repeat(runPhase + Mathf.Abs(velocity.x) * Time.deltaTime / runStrideLength, 1f) : 0;

            int state = IdleState;
            float progress = Mathf.Repeat(idleTime, 1f);
            if (player.Health.IsDead)
            {
                state = DeathState;
                progress = Mathf.Clamp01((Time.time - player.Health.DeathTime) / deathDuration);
            }
            else if (slashing)
            {
                if (player.IsClearBlueSky) { state = SkyState; progress = player.ClearBlueSkyProgress; }
                else if (player.IsHinokamiDancing) { state = HinokamiState; progress = player.HinokamiDanceProgress; }
                else if (player.IsFlowingDancing) { state = DanceState; progress = player.FlowingDanceProgress; }
                else if (player.IsWaterWheeling) { state = WheelState; progress = player.WaterWheelProgress; }
                else if (player.IsWaterSlashing) { state = WaterState; progress = player.WaterSlashProgress; }
                else { state = SlashState; progress = player.BasicSlashProgress; }
            }
            else if (alive && Time.time - player.Health.LastDamageTime < hurtDuration)
            {
                state = HurtState;
                progress = (Time.time - player.Health.LastDamageTime) / hurtDuration;
            }
            else if (alive && !grounded)
            {
                state = velocity.y > 1.2f ? RiseState : velocity.y < -1.2f ? FallState : ApexState;
                progress = 0;
            }
            else if (alive && landing) { state = LandState; progress = (Time.time - landedAt) / landingDuration; }
            else if (running) { state = RunState; progress = runPhase; }
            animator.Play(state, 0, progress);
            animator.Update(0);
            visual.flipX = player.Facing < 0;
            visual.color = Color.white;
            // Rotate only the tucked drawing around the body center; physics stays upright.
            bool spinning = slashing && player.IsWaterWheeling && progress >= PrototypePlayer.WheelLaunch && progress < PrototypePlayer.WheelSpinEnd;
            bool skySpinning = slashing && player.IsClearBlueSky && progress >= PrototypePlayer.SkyLaunch && progress < PrototypePlayer.SkySpinEnd;
            transform.localPosition = spinning || skySpinning ? Vector3.zero : new Vector3(0, -.85f, 0);
            float angle = spinning ? -360 * player.Facing * Mathf.InverseLerp(PrototypePlayer.WheelLaunch, PrototypePlayer.WheelSpinEnd, progress) : 0;
            if (skySpinning) angle = -360 * player.Facing * Mathf.InverseLerp(PrototypePlayer.SkyLaunch, PrototypePlayer.SkySpinEnd, progress);
            transform.localRotation = Quaternion.Euler(0, 0, angle);
        }

        private void ResetVisual()
        {
            idleTime = 0;
            runPhase = 0;
            landedAt = float.NegativeInfinity;
            wasGrounded = false;
            awaitingFirstGround = true;
            animator.Rebind();
            animator.Play(IdleState, 0, 0);
            animator.Update(0);
            visual.flipX = false;
            visual.color = Color.white;
            transform.localPosition = new Vector3(0, -.85f, 0);
            transform.localRotation = Quaternion.identity;
        }
    }
}
