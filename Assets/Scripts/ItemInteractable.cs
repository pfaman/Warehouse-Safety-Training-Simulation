using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(Collider))]
public class ItemHoverGlow : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;  // Added item name
    public VideoClip hoverVideoClip; // Video that should play when hovered

    [Header("Glow")]
    public bool useHighlightMaterial = true;
    public Material highlightMaterial;
    public Color emissionColor = Color.yellow;
    public float emissionIntensity = 2f;

    [Header("Video (projector)")]
    public bool playProjectorVideoOnHover = true;
    public VideoPlayer projectorVideo;
    public AudioSource projectorAudioSource;

    Renderer rend;
    Material[] originalMaterials;
    Material[] glowMaterials;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend == null) rend = GetComponentInChildren<Renderer>();

        if (rend != null)
        {
            originalMaterials = rend.materials;
            glowMaterials = new Material[originalMaterials.Length];
            for (int i = 0; i < originalMaterials.Length; i++)
            {
                Material m = new Material(originalMaterials[i]);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                }
                glowMaterials[i] = m;
            }
        }

        if (projectorVideo != null)
        {
            projectorVideo.audioOutputMode = VideoAudioOutputMode.AudioSource;

            if (projectorAudioSource != null)
            {
                projectorVideo.SetTargetAudioSource(0, projectorAudioSource);
                projectorAudioSource.playOnAwake = false;
                projectorAudioSource.volume = 1f;
            }
        }
    }

    private void Start()
    {
        // safe auto-register; manager must be in scene before items Start() (place Managers high in hierarchy)
        if (ChecklistManager.Instance != null && !string.IsNullOrWhiteSpace(itemName))
            ChecklistManager.Instance.RegisterItem(itemName);
    }

    void OnMouseEnter()
    {
        SetGlow(true);

        if (playProjectorVideoOnHover && projectorVideo != null)
        {
            // Assign hover video dynamically
            if (hoverVideoClip != null)
                projectorVideo.clip = hoverVideoClip;

            if (!projectorVideo.isPlaying)
                projectorVideo.Play();

            if (projectorAudioSource != null)
            {
                projectorAudioSource.volume = 1f;
                projectorAudioSource.Stop(); // reset to start
                projectorAudioSource.Play();
            }

            Debug.Log("Hovering over: " + itemName);
        }
    }

    void OnMouseDown()
    {
        if (InspectionManager.Instance != null && !InspectionManager.Instance.IsInspecting)
        {
            InspectionManager.Instance.StartInspection(this);
        }
    }


    void OnMouseExit()
    {
        SetGlow(false);
        // Optionally pause the video
        // if (playProjectorVideoOnHover && projectorVideo != null) projectorVideo.Pause();
    }

    void SetGlow(bool on)
    {
        if (rend == null) return;
        if (on)
        {
            if (useHighlightMaterial && highlightMaterial != null)
            {
                Material[] mats = new Material[rend.materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = highlightMaterial;
                rend.materials = mats;
            }
            else if (glowMaterials != null)
            {
                rend.materials = glowMaterials;
            }
        }
        else
        {
            if (originalMaterials != null)
            {
                rend.materials = originalMaterials;
            }
        }
    }

    void OnDestroy()
    {
        if (glowMaterials != null)
        {
            foreach (var m in glowMaterials)
                if (m != null) Destroy(m);
        }
    }
}
