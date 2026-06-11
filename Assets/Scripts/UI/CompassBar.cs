using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompassBar : MonoBehaviour
{
    public float BarRange => barRange;

    [SerializeField] private float barRange = 120f;
    [SerializeField] private Transform player;

    [Header("Rows")]
    [SerializeField] private Row cardinalDirectionRow;
    [SerializeField] private Row directionDegreeRow;
    [SerializeField] private RectTransform rowParent;
    [SerializeField] private Color northColor = Color.yellow;

    public RectTransform BarRectTransform => _rectTransform;
    private RectTransform _rectTransform;

    private readonly List<(RectTransform rect, float degree)> rows = new();

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (rowParent == null)
            rowParent = _rectTransform;

        SetupCompassRows();
    }

    private void Update()
    {
        UpdateCompassRows();
    }

    private void SetupCompassRows()
    {
        for (int i = 0; i < 24; i++)
        {
            int degree = 15 * (i + 1);

            bool isCardinal =
                degree == 90 ||
                degree == 180 ||
                degree == 270 ||
                degree == 360;

            Row prefab = isCardinal ? cardinalDirectionRow : directionDegreeRow;
            Row row = Instantiate(prefab, rowParent);

            if (degree == 360)
            {
                row.DirectionText.text = "N";
                row.LineImage.color = northColor;
                row.DirectionText.color = northColor;
            }
            else if (degree == 90)
            {
                row.DirectionText.text = "E";
            }
            else if (degree == 180)
            {
                row.DirectionText.text = "S";
            }
            else if (degree == 270)
            {
                row.DirectionText.text = "W";
            }
            else
            {
                row.DirectionText.text = degree.ToString();
            }

            row.gameObject.SetActive(true);

            float normalizedDegree = degree == 360 ? 0f : degree;
            rows.Add((row.GetComponent<RectTransform>(), normalizedDegree));
        }

        cardinalDirectionRow.gameObject.SetActive(false);
        directionDegreeRow.gameObject.SetActive(false);
    }

    private void UpdateCompassRows()
    {
        float playerYaw = player.eulerAngles.y;

        foreach (var row in rows)
        {
            float signedAngle = Mathf.DeltaAngle(playerYaw, row.degree);

            float mappedAngle = signedAngle / 180f;

            float xPosition =
                mappedAngle *
                (360f / barRange) *
                (_rectTransform.rect.width / 2f);

            row.rect.anchoredPosition = new Vector2(xPosition, row.rect.anchoredPosition.y);

            row.rect.gameObject.SetActive(Mathf.Abs(signedAngle) <= barRange / 2f);
        }
    }
}