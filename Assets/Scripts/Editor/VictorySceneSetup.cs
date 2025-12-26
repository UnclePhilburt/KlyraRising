using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class VictorySceneSetup : MonoBehaviour
{
    [MenuItem("Klyra/Setup Victory Scene")]
    public static void SetupVictoryScene()
    {
        // Create root object
        GameObject root = new GameObject("VictoryCrawl");
        VictorySceneCrawl crawl = root.AddComponent<VictorySceneCrawl>();

        // Create Canvas
        GameObject canvasObj = new GameObject("Canvas");
        canvasObj.transform.SetParent(root.transform);
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        canvasObj.AddComponent<GraphicRaycaster>();

        // Create black background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create crawl container (this moves upward)
        GameObject container = new GameObject("CrawlContainer");
        container.transform.SetParent(canvasObj.transform, false);
        RectTransform containerRect = container.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.15f, 0f);
        containerRect.anchorMax = new Vector2(0.85f, 0f);
        containerRect.pivot = new Vector2(0.5f, 1f);
        containerRect.anchoredPosition = new Vector2(0, 0);
        containerRect.sizeDelta = new Vector2(0, 15000); // 16 chunks x 800 + buffer

        // Skip perspective rotation for now - it hides chunks
        // container.transform.localRotation = Quaternion.Euler(55f, 0f, 0f);

        // Split text into multiple objects to avoid TMP 65k vertex limit
        string[] textChunks = VictorySceneSetup.GetCrawlTextChunks();
        float yOffset = 0f;
        TextMeshProUGUI firstText = null;

        // Use fixed spacing since ForceMeshUpdate doesn't work reliably in Editor
        float chunkHeight = 800f; // Fixed height per small chunk (16 chunks total)

        for (int i = 0; i < textChunks.Length; i++)
        {
            GameObject textObj = new GameObject($"CrawlText_{i}");
            textObj.transform.SetParent(container.transform, false);
            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = textChunks[i];
            text.fontSize = 42;
            text.color = new Color(1f, 0.8f, 0.2f); // Star Wars yellow
            text.alignment = TextAlignmentOptions.Top;
            text.fontStyle = FontStyles.Bold;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableWordWrapping = true;

            RectTransform textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(0.5f, 1f);
            textRect.anchoredPosition = new Vector2(0, -yOffset);
            textRect.sizeDelta = new Vector2(-40, 1000); // Allow overflow

            yOffset += chunkHeight;

            if (i == 0) firstText = text;

            Debug.Log($"[VictoryScene] Created CrawlText_{i} at Y offset {-yOffset}");
        }

        // Create audio source for music
        AudioSource audio = root.AddComponent<AudioSource>();
        audio.playOnAwake = true;  // Auto-play when scene loads
        audio.loop = true;
        audio.volume = 0.5f;
        audio.spatialBlend = 0f;  // 2D sound

        // Also add AudioListener since this scene needs one
        GameObject camObj = new GameObject("AudioCamera");
        camObj.transform.SetParent(root.transform);
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.depth = -100;  // Render behind UI
        camObj.AddComponent<AudioListener>();

        // Wire up the VictorySceneCrawl component
        SerializedObject so = new SerializedObject(crawl);
        so.FindProperty("_crawlContainer").objectReferenceValue = containerRect;
        so.FindProperty("_crawlText").objectReferenceValue = firstText;
        so.FindProperty("_musicSource").objectReferenceValue = audio;
        so.ApplyModifiedProperties();

        // Select it
        Selection.activeGameObject = root;

        Debug.Log("[VictoryScene] Setup complete!");
        Debug.Log("1. Edit the CrawlText to customize your story");
        Debug.Log("2. Drag music into the AudioSource");
        Debug.Log("3. Adjust timing in VictorySceneCrawl component");
    }

    static string[] GetCrawlTextChunks()
    {
        // Shorter, punchier lore - keeps attention
        return new string[]
        {
            // Chunk 1: Title & Hook
            @"KLYRA RISING



Before time had a name,
there was only the Void.

From it came the Architects,
who built countless worlds.

And from the spaces between,
the Corruption emerged.",

            // Chunk 2: Wanderers
            @"The Architects created
the Wanderers to fight back.

Immortal warriors who
absorb the essence
of those they defeat.

You are a Wanderer.",

            // Chunk 3: Klyra
            @"

THE FALL OF KLYRA


This realm once knew peace.
Samurai clans ruled with honor.

Then the Corruption came.
The clans fell. Temples burned.

The people cried out.

The Architects sent you.",

            // Chunk 4: Victory
            @"

FIRST VICTORY


You have struck down
the Corruption's champion.

But this was only the beginning.

Greater enemies await.
Other worlds cry out.",

            // Chunk 5: Closing
            @"You are the sword
that cuts through shadow.

You are the last hope
of a thousand worlds.

Your journey has just begun.



RISE"
        };
    }

    static string GetDefaultCrawlText()
    {
        return @"KLYRA RISING



Before time had a name,
before the first star burned bright,
there was only the Void.

Endless. Silent. Hungry.

From this primordial darkness
came the Architects —
beings of pure creation
who shaped the multiverse
with thought alone.

They built countless worlds.
Countless realities.
Countless lives.

But creation breeds destruction.

From the spaces between universes,
the Corruption emerged —
a force of unmaking
that devours all it touches.

Worlds fell. Stars died.
Entire realities collapsed
into nothing.

The Architects, in their
final act of desperation,
created the Wanderers.



IMMORTAL WARRIORS


Bound to the space between worlds,
the Wanderers cannot truly die.
Their souls are anchored
to the fabric of existence itself.

When a Wanderer falls in battle,
they rise again —
stronger, wiser, unbroken.

Death is not an end.
It is a teacher.

And the Wanderers have
learned many lessons.



THE SOUL EATERS


The Wanderers possess
a terrible gift.

When they defeat an enemy,
they absorb their essence —
their skills, their strength,
their very form.

A Wanderer who slays a samurai
becomes a samurai.

A Wanderer who defeats a knight
can wear that knight's armor,
wield their blade,
know their memories.

They are living weapons,
forever adapting,
forever evolving.



YOU ARE A WANDERER


You have walked through
a thousand worlds.

You have been a soldier
in wars between galaxies.

You have been a king
who ruled with wisdom.

You have been a peasant
who rose to legend.

Every life you have lived
has made you stronger.

Every death you have suffered
has made you wiser.

You remember fragments —
faces, battles, loves, losses.

But always, the mission remains.

Destroy the Corruption.
Save the multiverse.
No matter the cost.



THE FALL OF KLYRA


Klyra was once a realm
of honor and beauty.

Samurai clans ruled with justice.
Temples touched the clouds.
Peace endured for generations.

Then the Corruption came.

It whispered to the ambitious.
It promised power to the weak.
It turned brother against brother.

The great clans fell.
The temples burned.
Darkness spread across the land.

The people of Klyra
cried out for salvation.

And the Architects answered.

They sent you.



THE FIRST VICTORY


You have struck down
the Corruption's champion.

The shadow that gripped
this land has weakened.

But do not be deceived.

This was merely a foothold.
A test. A beginning.

The true darkness lies ahead —
in the heart of Klyra,
where the source of
the Corruption festers.

Greater enemies await.
Darker truths will be revealed.
And the fate of this world
hangs by a thread.



THE PATH AHEAD


Beyond Klyra,
other worlds cry out.

Kingdoms of sorcery and steel
where dragons darken the skies.

Frozen wastelands
where ancient evils sleep.

Neon-lit cities of the future
where humanity fights for survival.

Desert empires built on
the bones of fallen gods.

Each world has fallen
to the Corruption.

Each world needs a Wanderer.

Each world needs you.



THIS IS YOUR PURPOSE


You will never rest.
You will never stop.
You will never truly die.

You are the sword
that cuts through shadow.

You are the light
in the dying dark.

You are the last hope
of a thousand worlds.

You are a Wanderer.

And your journey
has only just begun.





RISE




";
    }
}
