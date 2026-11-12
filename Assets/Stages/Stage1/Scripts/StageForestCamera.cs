using DemonSlayer.Core;
using UnityEngine;

namespace DemonSlayer.Stages
{
    /// <summary>Frames a horizontal route and blends its supplied painted scenery.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class StageForestCamera : MonoBehaviour
    {
        [SerializeField] private CorePlayer player;
        [SerializeField] private SpriteRenderer entrance;
        [SerializeField] private SpriteRenderer wisteria;
        [SerializeField] private SpriteRenderer moonlit;
        [SerializeField] private Vector2 levelBounds = new Vector2(-12, 108);
        [SerializeField] private Vector2 wisteriaTransition = new Vector2(24, 36);
        [SerializeField] private Vector2 moonlitTransition = new Vector2(60, 72);
        [SerializeField] private float moonlitCenter = 88;
        [SerializeField] private float groundHeight = -.9f;
        [SerializeField] private float cameraHeight = 3.5f;
        [SerializeField] private float lookAhead = 2f;
        [SerializeField, Min(.01f)] private float followTime = .18f;
        [SerializeField, Range(0, 1)] private float backgroundMotion = .15f;
        private Camera view;
        private Rigidbody2D playerBody;
        private float horizontalVelocity;

        private void Awake()
        {
            view = GetComponent<Camera>();
            if (player) playerBody = player.GetComponent<Rigidbody2D>();
            if (!player || !playerBody || !entrance || !wisteria || !moonlit ||
                !entrance.sprite || !wisteria.sprite || !moonlit.sprite)
            {
                Debug.LogError("Stage forest camera requires Tanjiro and all three scenery sprites.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (player) player.ResetPerformed += SnapToPlayer;
        }

        private void OnDisable()
        {
            if (player) player.ResetPerformed -= SnapToPlayer;
        }

        private void Start() => SnapToPlayer();

        private float ClampToLevel(float x)
        {
            float halfWidth = view.orthographicSize * view.aspect;
            if (halfWidth * 2 >= levelBounds.y - levelBounds.x)
                return (levelBounds.x + levelBounds.y) * .5f;
            return Mathf.Clamp(x, levelBounds.x + halfWidth, levelBounds.y - halfWidth);
        }

        private void SnapToPlayer()
        {
            if (!view || !player || !enabled) return;
            horizontalVelocity = 0;
            // ResetAt changes the body before an interpolated Transform catches up.
            transform.position = new Vector3(ClampToLevel(playerBody.position.x + lookAhead), cameraHeight, -10);
            UpdateScenery();
        }

        private void LateUpdate()
        {
            if (Time.deltaTime <= 0) return;
            float destination = ClampToLevel(playerBody.position.x + lookAhead);
            float x = Mathf.SmoothDamp(transform.position.x, destination, ref horizontalVelocity, followTime);
            transform.position = new Vector3(ClampToLevel(x), cameraHeight, -10);
            UpdateScenery();
        }

        private void UpdateScenery()
        {
            float x = transform.position.x;
            float second = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(wisteriaTransition.x, wisteriaTransition.y, x));
            float third = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(moonlitTransition.x, moonlitTransition.y, x));
            // Each incoming painting overlays an opaque previous painting: no dark dip at transitions.
            Place(entrance, 12, 1, second < 1 && third < 1);
            Place(wisteria, 46, second, second > 0 && third < 1);
            Place(moonlit, moonlitCenter, third, third > 0);
        }

        private void Place(SpriteRenderer layer, float sectionCenter, float opacity, bool visible)
        {
            Vector2 size = layer.sprite.bounds.size;
            float width = view.orthographicSize * view.aspect * 2;
            float height = cameraHeight + view.orthographicSize - groundHeight;
            float scale = Mathf.Max((width + 4) / size.x, (height + .2f) / size.y);
            float allowance = Mathf.Max(0, (size.x * scale - width) * .5f - .05f);
            float offset = Mathf.Clamp((sectionCenter - transform.position.x) * backgroundMotion, -allowance, allowance);
            layer.transform.position = new Vector3(transform.position.x + offset, groundHeight, 5);
            layer.transform.localScale = Vector3.one * scale;
            layer.color = new Color(1, 1, 1, opacity);
            layer.enabled = visible;
        }
    }
}
