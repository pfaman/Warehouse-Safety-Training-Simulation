using UnityEngine;
using UnityEngine.Video;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class ItemHoverGlow : MonoBehaviour
{
    [Header("Item Info")]
    public string itemName;
    public VideoClip hoverVideoClip;

    [Header("Glow")]
    public bool useHighlightMaterial = true;
    public Material highlightMaterial;
    public Color emissionColor = Color.yellow;
    public float emissionIntensity = 2f;

    [Header("Video (projector)")]
    public bool playProjectorVideoOnHover = true;
    public VideoPlayer projectorVideo;
    public AudioSource projectorAudioSource;

    private Renderer[] renderers;
    private List<Material[]> originalMaterials = new List<Material[]>();
    private List<Material[]> glowMaterials = new List<Material[]>();

    void Awake()
    {
        // Get all renderers in this object and its children
        renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            // Store original
            originalMaterials.Add(r.materials);

            // Prepare glow versions
            Material[] glowMats = new Material[r.materials.Length];
            for (int i = 0; i < r.materials.Length; i++)
            {
                Material m = new Material(r.materials[i]);
                if (m.HasProperty("_EmissionColor"))
                {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", emissionColor * emissionIntensity);
                }
                glowMats[i] = m;
            }
            glowMaterials.Add(glowMats);
        }

        // Setup video
        if (projectorVideo != null)
        {
            projectorVideo.audioOutputMode = VideoAudioOutputMode.AudioSource;
            if (projectorAudioSource != null)
            {
                projectorVideo.SetTargetAudioSource(0, projectorAudioSource);
                projectorAudioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        if (ChecklistManager.Instance != null && !string.IsNullOrWhiteSpace(itemName))
            ChecklistManager.Instance.RegisterItem(itemName);
    }

    void OnMouseEnter()
    {
       
        SetGlow(true);

        if (playProjectorVideoOnHover && projectorVideo != null)
        {
            if (hoverVideoClip != null)
                projectorVideo.clip = hoverVideoClip;

            if (!projectorVideo.isPlaying)
                projectorVideo.Play();

            if (projectorAudioSource != null)
            {
                projectorAudioSource.Stop();
                projectorAudioSource.Play();
            }
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
    }

    void SetGlow(bool on)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (on)
            {
                if (useHighlightMaterial && highlightMaterial != null)
                {
                    Material[] mats = new Material[renderers[i].materials.Length];
                    for (int m = 0; m < mats.Length; m++)
                        mats[m] = highlightMaterial;
                    renderers[i].materials = mats;
                }
                else
                {
                    renderers[i].materials = glowMaterials[i];
                }
            }
            else
            {
                renderers[i].materials = originalMaterials[i];
            }
        }
    }

    void OnDestroy()
    {
        foreach (var mats in glowMaterials)
        {
            foreach (var m in mats)
                if (m != null) Destroy(m);
        }
    }
}
