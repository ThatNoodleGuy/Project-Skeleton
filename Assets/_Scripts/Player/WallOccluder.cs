using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fades out whichever wall(s) currently sit between the fixed isometric camera
/// and the player, so the player stays visible/controllable near room edges and
/// doorways instead of being hidden behind near-side geometry. Probes from three
/// points spanning the player's width (not just its center) so multiple walls
/// (corners, junctions, doorways) can be caught and faded simultaneously, and
/// walls stay faded for a short grace period after they stop registering a hit so
/// a wall seen nearly edge-on doesn't flicker in and out every frame.
///
/// Fading swaps the wall's renderer onto an instance of a dedicated, pre-authored
/// transparent material (fadeMaterialTemplate) rather than flipping the wall's own
/// opaque material to Transparent at runtime via shader keywords — that approach
/// was unreliable (URP's opaque/transparent code path and lighting response don't
/// always follow a runtime keyword toggle cleanly) and never got reliably close to
/// fully invisible. The tradeoff is a one-frame texture swap (grid pattern -> flat
/// grey) the instant a wall starts fading, but it's far less jarring than the wall
/// staying stubbornly solid, and the fade itself is now fully reliable.
/// </summary>
public class WallOccluder : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform target;
    [SerializeField] private Material fadeMaterialTemplate;
    [SerializeField] private float targetHeightOffset = 1f;
    [SerializeField] private float probeRadius = 0.6f;
    [SerializeField] private float sampleSpread = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float fadedAlpha = 0f;
    [SerializeField] private float fadeSpeed = 6f;
    [SerializeField] private float unfadeGracePeriod = 0.25f;

    private class FadeState
    {
        public Material instanceMaterial;
        public Material originalSharedMaterial;
        public float currentAlpha;
        public float lastHitTime;
    }

    private readonly Dictionary<Renderer, FadeState> fades = new Dictionary<Renderer, FadeState>();
    private readonly HashSet<Renderer> hitThisFrame = new HashSet<Renderer>();
    private readonly List<Renderer> toForget = new List<Renderer>();
    private readonly List<Vector3> samplePoints = new List<Vector3>(3);

    private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorProp = Shader.PropertyToID("_Color");

    void Start()
    {
        if (cameraTransform == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (camObj != null)
                cameraTransform = camObj.transform;
        }

        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                target = playerObj.transform;
        }

        if (fadeMaterialTemplate == null)
            Debug.LogError("WallOccluder: fadeMaterialTemplate not assigned — wall fading will do nothing.");
    }

    void LateUpdate()
    {
        if (cameraTransform == null || target == null || fadeMaterialTemplate == null)
            return;

        hitThisFrame.Clear();

        Vector3 targetPoint = target.position + Vector3.up * targetHeightOffset;
        Vector3 camRight = cameraTransform.right;

        samplePoints.Clear();
        samplePoints.Add(targetPoint);
        samplePoints.Add(targetPoint + camRight * sampleSpread);
        samplePoints.Add(targetPoint - camRight * sampleSpread);

        for (int p = 0; p < samplePoints.Count; p++)
        {
            Vector3 toTarget = samplePoints[p] - cameraTransform.position;
            float distance = toTarget.magnitude;
            if (distance < 0.01f)
                continue;

            RaycastHit[] hits = Physics.SphereCastAll(cameraTransform.position, probeRadius, toTarget / distance, distance);
            foreach (RaycastHit hit in hits)
            {
                if (!hit.collider.name.Contains("Wall"))
                    continue;

                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend == null)
                    continue;

                hitThisFrame.Add(rend);
                if (!fades.ContainsKey(rend))
                    BeginFade(rend);
                fades[rend].lastHitTime = Time.time;
            }
        }

        toForget.Clear();
        foreach (var kvp in fades)
        {
            Renderer rend = kvp.Key;
            FadeState state = kvp.Value;
            if (rend == null)
            {
                toForget.Add(rend);
                continue;
            }

            // Keep fading a wall down even on frames it wasn't re-hit, as long as
            // we're still inside its grace period — avoids flicker on near-edge-on walls.
            bool stillOccluding = hitThisFrame.Contains(rend) || (Time.time - state.lastHitTime) < unfadeGracePeriod;

            float desiredAlpha = stillOccluding ? fadedAlpha : 1f;
            state.currentAlpha = Mathf.MoveTowards(state.currentAlpha, desiredAlpha, fadeSpeed * Time.deltaTime);
            ApplyAlpha(state.instanceMaterial, state.currentAlpha);

            if (!stillOccluding && Mathf.Approximately(state.currentAlpha, 1f))
            {
                // Fully restored — hand the renderer its original opaque material back.
                rend.sharedMaterial = state.originalSharedMaterial;
                toForget.Add(rend);
            }
        }

        for (int i = 0; i < toForget.Count; i++)
            fades.Remove(toForget[i]);
    }

    private void BeginFade(Renderer rend)
    {
        FadeState state = new FadeState
        {
            originalSharedMaterial = rend.sharedMaterial,
            instanceMaterial = new Material(fadeMaterialTemplate),
            currentAlpha = 1f,
            lastHitTime = Time.time
        };

        rend.material = state.instanceMaterial;
        ApplyAlpha(state.instanceMaterial, state.currentAlpha);
        fades[rend] = state;

        Debug.Log($"[WallOccluder] Began fading '{rend.name}'. Instance material: {state.instanceMaterial.name}, " +
                   $"shader: {state.instanceMaterial.shader.name}, _Surface={state.instanceMaterial.GetFloat("_Surface")}, " +
                   $"_Cull={state.instanceMaterial.GetFloat("_Cull")}, renderQueue={state.instanceMaterial.renderQueue}.");
    }

    private static void ApplyAlpha(Material mat, float alpha)
    {
        if (mat.HasProperty(BaseColorProp))
        {
            Color c = mat.GetColor(BaseColorProp);
            c.a = alpha;
            mat.SetColor(BaseColorProp, c);
        }
        if (mat.HasProperty(ColorProp))
        {
            Color c = mat.GetColor(ColorProp);
            c.a = alpha;
            mat.SetColor(ColorProp, c);
        }
    }
}
