using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowGraph : MonoBehaviour
{
    [SerializeField]
    private Sprite dotSprite;
    [SerializeField]
    private RectTransform graphContainer;
    [SerializeField]
    private List<Color> colors;
    [SerializeField]
    private float dotSize = 5f;
    [SerializeField]
    private Vector2 delta = Vector2.zero;

    private RectTransform window;
    private List<GameObject> dots = new List<GameObject>();
    private HashSet<Color> usedColors = new HashSet<Color>();

    private void Start()
    {
        window = GetComponent<RectTransform>();
        colors.Add(Color.green);
        colors.Add(Color.red);
        colors.Add(Color.blue);
        colors.Add(Color.yellow);
        colors.Add(Color.cyan);
        colors.Add(Color.magenta);
        colors.Add(Color.white);
    }

    internal void SetDataBoundary(Vector2 east, Vector2 north)
    {
        delta = new Vector2(-east.x + dotSize, -north.x + dotSize);
        ResizeDisplay(new Vector2(east.y - east.x + 2f * dotSize, north.y - north.x + 2f * dotSize));
    }

    private void CreateDot(Vector2 anchoredPosition, Color color, RectTransform parent = null)
    {
        GameObject dot = new GameObject("dot", typeof(Image));
        if (parent == null) dot.transform.parent = graphContainer;
        else dot.transform.parent = parent;
        var image = dot.GetComponent<Image>();
        image.sprite = dotSprite;
        image.color = color;
        RectTransform rectTransform = dot.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(dotSize, dotSize);
        rectTransform.anchoredPosition = anchoredPosition + delta;
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.zero;

        dots.Add(dot);
    }

    public void TestShipPredictionValues()
    {

    }

    public void DisplayShipMesurementData(List<PathPrediction.ShipMeasurementData> shipData)
    {
        GameObject parentObject = new GameObject();
        parentObject.AddComponent<RectTransform>();
        parentObject.transform.parent = window;
        var parent = parentObject.GetComponent<RectTransform>();
        parent.anchorMin = Vector2.zero;
        parent.anchorMax = Vector2.zero;
        parent.sizeDelta = graphContainer.sizeDelta;
        parent.anchoredPosition = graphContainer.anchoredPosition;
        Color color = colors[usedColors.Count];
        usedColors.Add(color);
        foreach (var data in shipData)
        {
            CreateDot(new Vector2(data.EUN.x, data.EUN.z), color, parent);
        }
    }

    private void DisplayNEPositions(List<Vector3> positions, Color color)
    {
        foreach (var pos in positions)
        {
            CreateDot(new Vector2(pos.z, pos.x), color);
        }
    }

    public void ResizeDisplay(Vector2 size, float edge = 0.05f)
    {
        float edgeSize = ((size.x + size.y) / 2f) * edge;
        window.sizeDelta = new Vector2(size.x + 2f * edgeSize, size.y + 2f * edgeSize);
        graphContainer.sizeDelta = size;
        graphContainer.anchoredPosition = new Vector2(size.x / 2f + edgeSize, size.y / 2f + edgeSize);
    }

    [ContextMenu("Clear Dots")]
    public void ClearDots()
    {
        foreach (var d in dots)
        {
            Destroy(d);
        }
        dots.Clear();
    }
}
