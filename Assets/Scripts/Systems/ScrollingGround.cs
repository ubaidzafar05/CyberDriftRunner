using UnityEngine;

public sealed class ScrollingGround : MonoBehaviour
{
    private static readonly Color EdgeGlowColor = new Color(0.18f, 0.88f, 0.96f, 0.42f);
    private static readonly Color GuardRailColor = new Color(0.12f, 0.14f, 0.2f);
    private static readonly Color SupportGlowColor = new Color(1f, 0.32f, 0.74f, 0.34f);

    [Header("Segments")]
    [SerializeField] private int segmentCount = 4;
    [SerializeField] private float segmentLength = 40f;
    [SerializeField] private float roadWidth = 12f;
    [SerializeField] private float roadY = 0f;

    [Header("Visuals")]
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Material stripeMaterial;
    [SerializeField] private Color roadColor = new Color(0.08f, 0.09f, 0.14f);
    [SerializeField] private Color stripeColor = new Color(0.3f, 0.72f, 0.9f, 0.38f);

    private Transform[] _segments;
    private float _recycleZ;

    private void Start()
    {
        _segments = new Transform[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            _segments[i] = CreateSegment(i);
        }

        _recycleZ = -segmentLength;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.Player == null)
        {
            return;
        }

        float playerZ = GameManager.Instance.Player.transform.position.z;
        float behindThreshold = playerZ - segmentLength * 1.5f;

        for (int i = 0; i < _segments.Length; i++)
        {
            if (_segments[i].position.z < behindThreshold)
            {
                float maxZ = GetMaxSegmentZ();
                _segments[i].position = new Vector3(0f, roadY, maxZ + segmentLength);
            }
        }
    }

    private float GetMaxSegmentZ()
    {
        float max = float.MinValue;
        for (int i = 0; i < _segments.Length; i++)
        {
            if (_segments[i].position.z > max)
            {
                max = _segments[i].position.z;
            }
        }

        return max;
    }

    private Transform CreateSegment(int index)
    {
        GameObject segment = new GameObject($"RoadSegment_{index}");
        segment.transform.SetParent(transform, false);
        segment.transform.position = new Vector3(0f, roadY, index * segmentLength);

        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "RoadSurface";
        road.transform.SetParent(segment.transform, false);
        road.transform.localPosition = new Vector3(0f, -0.05f, segmentLength * 0.5f);
        road.transform.localScale = new Vector3(roadWidth, 0.1f, segmentLength);

        Renderer roadRenderer = road.GetComponent<Renderer>();
        if (roadMaterial != null)
        {
            roadRenderer.sharedMaterial = roadMaterial;
        }
        else
        {
            roadRenderer.material.color = roadColor;
        }

        Collider roadCollider = road.GetComponent<Collider>();
        if (roadCollider != null)
        {
            Object.Destroy(roadCollider);
        }

        CreateLaneStripes(segment.transform);
        CreateEdgeLines(segment.transform);
        CreateEnergySpine(segment.transform);
        CreateGuardRails(segment.transform);
        CreateSupportPosts(segment.transform);
        return segment.transform;
    }

    private void CreateLaneStripes(Transform parent)
    {
        float[] lanePositions = { -3f, 3f };
        for (int lane = 0; lane < lanePositions.Length; lane++)
        {
            int stripeCount = Mathf.FloorToInt(segmentLength / 4f);
            for (int s = 0; s < stripeCount; s++)
            {
                GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stripe.name = "LaneStripe";
                stripe.transform.SetParent(parent, false);
                stripe.transform.localPosition = new Vector3(lanePositions[lane], 0.01f, (s * 4f) + 1f);
                stripe.transform.localScale = new Vector3(0.15f, 0.02f, 1.8f);

                Renderer r = stripe.GetComponent<Renderer>();
                if (stripeMaterial != null)
                {
                    r.sharedMaterial = stripeMaterial;
                }
                else
                {
                    r.material.color = stripeColor;
                }

                Collider c = stripe.GetComponent<Collider>();
                if (c != null)
                {
                    Object.Destroy(c);
                }
            }
        }
    }

    private void CreateEdgeLines(Transform parent)
    {
        float halfWidth = roadWidth * 0.5f;
        float[] edgeX = { -halfWidth, halfWidth };

        for (int e = 0; e < edgeX.Length; e++)
        {
            CreateStrip(parent, "EdgeLine", new Vector3(edgeX[e], 0.01f, segmentLength * 0.5f), new Vector3(0.12f, 0.03f, segmentLength), EdgeGlowColor, EdgeGlowColor * 0.35f);
            CreateStrip(parent, "EdgePlate", new Vector3(edgeX[e] * 0.98f, 0.12f, segmentLength * 0.5f), new Vector3(0.3f, 0.18f, segmentLength), GuardRailColor, Color.black);
        }
    }

    private void CreateEnergySpine(Transform parent)
    {
        CreateStrip(parent, "CenterSpine", new Vector3(0f, -0.015f, segmentLength * 0.5f), new Vector3(1.2f, 0.04f, segmentLength), new Color(0.06f, 0.08f, 0.12f), Color.black);

        int nodeCount = Mathf.FloorToInt(segmentLength / 5f);
        for (int i = 0; i < nodeCount; i++)
        {
            float z = (i * 5f) + 2.5f;
            CreateStrip(parent, "EnergyNode", new Vector3(0f, 0.015f, z), new Vector3(0.8f, 0.035f, 1.1f), SupportGlowColor, SupportGlowColor * 0.4f);
        }
    }

    private void CreateGuardRails(Transform parent)
    {
        float halfWidth = roadWidth * 0.5f;
        float[] edgeX = { -(halfWidth + 0.45f), halfWidth + 0.45f };
        for (int i = 0; i < edgeX.Length; i++)
        {
            CreateStrip(parent, "GuardRailBase", new Vector3(edgeX[i], 0.4f, segmentLength * 0.5f), new Vector3(0.18f, 0.16f, segmentLength), GuardRailColor, Color.black);
            CreateStrip(parent, "GuardRailGlow", new Vector3(edgeX[i] + Mathf.Sign(edgeX[i]) * -0.06f, 0.62f, segmentLength * 0.5f), new Vector3(0.06f, 0.08f, segmentLength), EdgeGlowColor, EdgeGlowColor * 0.42f);
        }
    }

    private void CreateSupportPosts(Transform parent)
    {
        float halfWidth = roadWidth * 0.5f;
        int postCount = Mathf.FloorToInt(segmentLength / 8f);
        for (int i = 0; i <= postCount; i++)
        {
            float z = Mathf.Min(segmentLength - 1f, (i * 8f) + 2f);
            CreateStrip(parent, "SupportLeft", new Vector3(-(halfWidth + 0.72f), 1.35f, z), new Vector3(0.16f, 2.4f, 0.16f), GuardRailColor, Color.black);
            CreateStrip(parent, "SupportRight", new Vector3(halfWidth + 0.72f, 1.35f, z), new Vector3(0.16f, 2.4f, 0.16f), GuardRailColor, Color.black);
            CreateStrip(parent, "SupportCross", new Vector3(0f, 2.52f, z), new Vector3(roadWidth + 1.25f, 0.12f, 0.12f), new Color(0.08f, 0.1f, 0.16f), SupportGlowColor * 0.18f);
        }
    }

    private void CreateStrip(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, Color emission)
    {
        GameObject strip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        strip.name = name;
        strip.transform.SetParent(parent, false);
        strip.transform.localPosition = localPosition;
        strip.transform.localScale = localScale;

        Renderer renderer = strip.GetComponent<Renderer>();
        Material material = renderer.material;
        if (roadMaterial != null && name.Contains("Base"))
        {
            material.CopyPropertiesFromMaterial(roadMaterial);
        }
        else if (stripeMaterial != null && (name.Contains("Glow") || name.Contains("Node") || name.Contains("EdgeLine")))
        {
            material.CopyPropertiesFromMaterial(stripeMaterial);
        }

        material.color = color;
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        Collider collider = strip.GetComponent<Collider>();
        if (collider != null)
        {
            Object.Destroy(collider);
        }
    }
}
