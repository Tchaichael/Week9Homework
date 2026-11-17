using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// World-space particles are left behind rather than rotating with the cat.
public class CatStarTrail : MonoBehaviour
{
    public Material starMaterial;
    [Header("星星拖尾设置（运行时也可调节）")]
    [InspectorName("星星大小范围（最小 / 最大）"), Tooltip("世界单位大小，X 是最小值，Y 是最大值。数值越大，星星越明显。")]
    public Vector2 starSize = new Vector2(2f, 3.5f);
    [InspectorName("拖尾最大长度"), Tooltip("世界单位。高速时会自动缩短星星寿命，避免超过此长度。"), Min(.1f)]
    public float maximumTrailLength = 5f;
    [InspectorName("每秒生成星星数量"), Tooltip("数值越大，拖尾越密集。设为 0 可停止生成。"), Min(0f)]
    public float starsPerSecond = 28f;
    [InspectorName("向后掉落速度"), Tooltip("沿小猫飞行方向的反方向移动，单位：世界单位/秒。"), Min(0f)]
    public float backwardSpeed = 2f;
    [InspectorName("向下掉落速度"), Tooltip("额外向世界 Y 轴下方掉落的速度，设为 0 则不向下掉落。"), Min(0f)]
    public float downwardSpeed = .9f;
    [FormerlySerializedAs("particleLifetime")]
    [InspectorName("淡出时间（秒）"), Tooltip("星星从出现到缩小、淡出消失的时间。拖尾长度限制可能让高速时的实际时间更短。"), Min(.01f)]
    public float fadeOutTime = .22f;
    ParticleSystem stars;
    Rigidbody body;
    Collider bodyCollider;
    Vector3 previousPosition;
    float emissionRemainder;

    void OnEnable()
    {
        body = GetComponent<Rigidbody>();
        bodyCollider = GetComponent<Collider>();
        previousPosition = transform.position;
        emissionRemainder = 0f;
    }

    public void ResetAfterTeleport()
    {
        previousPosition = transform.position;
        emissionRemainder = 0f;
    }

    void CreateParticles()
    {
        if (stars || !starMaterial) return;
        var go = new GameObject(name + " - Star Sparkles");
        SceneManager.MoveGameObjectToScene(go, gameObject.scene);
        stars = go.AddComponent<ParticleSystem>();
        stars.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = stars.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 4096;
        main.startSpeed = 0;
        main.gravityModifier = 0;
        var emission = stars.emission;
        emission.enabled = false;
        var shape = stars.shape;
        shape.enabled = false;
        var size = stars.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(
            new Keyframe(0,.55f), new Keyframe(.15f,1), new Keyframe(1,0)));
        var color = stars.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(Color.white,0), new GradientColorKey(Color.white,1) },
            new[] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,.1f), new GradientAlphaKey(0,1) });
        color.color = gradient;
        var rotation = stars.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = .7f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = starMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        stars.Play();
    }

    void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector3 delta = position - previousPosition;
        previousPosition = position;
        // No sparkle while aiming, standing still, or teleporting/resetting.
        if (!body || body.isKinematic || body.linearVelocity.sqrMagnitude < 1f ||
            delta.sqrMagnitude > 2500f || Time.deltaTime <= 0f)
        {
            emissionRemainder = 0;
            return;
        }
        CreateParticles();
        if (!stars) return;
        Vector3 velocity = body.linearVelocity;
        velocity.z = 0;
        float speed = velocity.magnitude;
        if (speed < 1f) return;
        Vector3 direction = velocity / speed;
        Vector3 sideways = new Vector3(-direction.y,direction.x,0);
        float radius = bodyCollider ? Mathf.Max(bodyCollider.bounds.extents.x,bodyCollider.bounds.extents.y) : 2f;
        emissionRemainder += Time.deltaTime * Mathf.Max(0f, starsPerSecond);
        int count = Mathf.Min(256, Mathf.FloorToInt(emissionRemainder));
        emissionRemainder -= count;
        Vector3 fallVelocity = -direction * backwardSpeed + Vector3.down * downwardSpeed;
        float separationSpeed = (velocity - fallVelocity).magnitude;
        float lifetime = Mathf.Min(fadeOutTime, maximumTrailLength / Mathf.Max(.01f, separationSpeed));
        for (int i=0; i<count; i++)
        {
            var p = new ParticleSystem.EmitParams();
            p.position = position - direction * radius * .75f + sideways * Random.Range(-.6f,.6f);
            p.position += new Vector3(0,0,-1);
            p.velocity = fallVelocity;
            p.startLifetime = Mathf.Max(.001f,lifetime);
            p.startSize = Random.Range(Mathf.Max(.01f, Mathf.Min(starSize.x,starSize.y)),
                Mathf.Max(.01f, Mathf.Max(starSize.x,starSize.y)));
            p.rotation = Random.Range(-25f,25f);
            p.startColor = Random.value < .5f ? new Color(1,.97f,.83f,1) : new Color(1,.76f,.25f,1);
            stars.Emit(p,1);
        }
    }

    public void CopySettingsTo(CatStarTrail other)
    {
        other.starMaterial = starMaterial;
        other.maximumTrailLength = maximumTrailLength;
        other.fadeOutTime = fadeOutTime;
        other.starsPerSecond = starsPerSecond;
        other.starSize = starSize;
        other.backwardSpeed = backwardSpeed;
        other.downwardSpeed = downwardSpeed;
    }

    void OnDisable()
    {
        if (stars)
        {
            if (Application.isPlaying) Destroy(stars.gameObject);
            else DestroyImmediate(stars.gameObject);
        }
        stars = null;
    }
}



