using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IdenticalBarController : MonoBehaviour
{
    public GameObject itemPrefab;

    public int itemCount = 5; 
    public float spacing = 0f; 
    public float startX = 0f;   

    private List<GameObject> items = new List<GameObject>();

    void Start()
    {
        CreateItems();
    }

    void CreateItems()
    {
        if (itemPrefab == null)
        {
            Debug.LogError("No itemPrefab!");
            return;
        }

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        items.Clear();

        for (int i = 0; i < itemCount; i++)
        {
            GameObject item = Instantiate(itemPrefab, transform);
            RectTransform rt = item.GetComponent<RectTransform>();

            float x = startX + i * spacing;
            rt.anchoredPosition = new Vector2(x, 0);

            items.Add(item);
        }
    }
}
