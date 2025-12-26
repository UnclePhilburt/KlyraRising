using UnityEngine;
using UnityEditor;
using TMPro;

public class FixVictoryScene : MonoBehaviour
{
    [MenuItem("Klyra/Fix Victory Scene Text")]
    public static void FixText()
    {
        // Find the crawl text in scene
        TextMeshProUGUI[] texts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        foreach (var text in texts)
        {
            if (text.gameObject.name == "CrawlText")
            {
                // Fix text settings
                text.overflowMode = TextOverflowModes.Overflow;
                text.enableWordWrapping = true;

                // Fix height
                RectTransform rect = text.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 20000);

                // Fix parent container height too
                RectTransform parent = text.transform.parent.GetComponent<RectTransform>();
                if (parent != null)
                {
                    parent.sizeDelta = new Vector2(parent.sizeDelta.x, 20000);
                }

                EditorUtility.SetDirty(text);
                EditorUtility.SetDirty(rect);
                if (parent != null) EditorUtility.SetDirty(parent);

                Debug.Log("[FixVictory] Fixed CrawlText - height set to 20000, overflow enabled");
                Debug.Log("[FixVictory] Don't forget to SAVE THE SCENE!");

                Selection.activeGameObject = text.gameObject;
                return;
            }
        }

        Debug.LogError("[FixVictory] Could not find CrawlText object in scene!");
    }
}
