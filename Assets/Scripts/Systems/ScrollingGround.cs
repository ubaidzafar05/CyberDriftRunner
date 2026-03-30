using UnityEngine;

public sealed class ScrollingGround : MonoBehaviour
{
    [Header("Segments")]
    [SerializeField] private int segmentCount = 4;
    [SerializeField] private float segmentLength = 40f;
    [SerializeField] private float roadWidth = 12f;
    [SerializeField] private float roadY = 0f;

    [Header("Visuals")]
    [SerializeField] private Material roadMaterial;
    [SerializeField] private Material stripeMaterial;
    [SerializeField] private Color roadColor = new Color(0.06f, 0.06f, 0.12f);
    [SerializeField] private Color stripeColor = new Color(0.1f, 0.7f, 1f, 0.6f);

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
        Color edgeColor = new Color(1f, 0.15f, 0.7f, 0.8f);

        for (int e = 0; e < edgeX.Length; e++)
        {
            GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "EdgeLine";
            edge.transform.SetParent(parent, false);
            edge.transform.localPosition = new Vector3(edgeX[e], 0.01f, segmentLength * 0.5f);
            edge.transform.localScale = new Vector3(0.12f, 0.03f, segmentLength);

            Renderer r = edge.GetComponent<Renderer>();
            Material mat = r.material;
            mat.color = edgeColor;
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", edgeColor * 2f);
            }

            Collider c = edge.GetComponent<Collider>();
            if (c != null)
            {
                Object.Destroy(c);
            }
        }
    }
}
