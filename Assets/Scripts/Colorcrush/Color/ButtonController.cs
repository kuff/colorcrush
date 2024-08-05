using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ButtonController : MonoBehaviour
{
    private List<Sprite> emojiSprites;
    private Queue<Sprite> emojiQueue;
    private const int RandomSeed = 42; // Specify the random seed here
    private Image[] selectionGridImages;
    private Button[] selectionGridButtons;
    private ColorController colorController;
    private bool[] buttonToggledStates;
    private Vector3[] originalButtonScales;
    private const float ShrinkFactor = 0.9f;
    private const float ToggledAlpha = 0.5f;
    private const float DefaultAlpha = 1f;

    private void Awake()
    {
        LoadAndShuffleEmojis();
        InitializeEmojiQueue();
        InitializeSelectionGridImages();
        InitializeSelectionGridButtons();
        colorController = FindObjectOfType<ColorController>();
        if (colorController == null)
        {
            Debug.LogError("ColorController not found in the scene.");
        }
    }

    private void LoadAndShuffleEmojis()
    {
        // Load all emoji sprites from the Resources folder
        emojiSprites = Resources.LoadAll<Sprite>("Colorcrush/Emoji").ToList();

        // Create a new System.Random instance with the specified seed
        System.Random random = new System.Random(RandomSeed);

        // Shuffle the list using the Fisher-Yates algorithm
        int n = emojiSprites.Count;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            (emojiSprites[k], emojiSprites[n]) = (emojiSprites[n], emojiSprites[k]);
        }
        Debug.Log($"ButtonController: Loaded and shuffled {emojiSprites.Count} emojis");
    }

    private void InitializeEmojiQueue()
    {
        emojiQueue = new Queue<Sprite>(emojiSprites);
        Debug.Log($"ButtonController: Emoji queue initialized with {emojiQueue.Count} emojis");
    }

    private void InitializeSelectionGridImages()
    {
        GameObject selectionGrid = GameObject.FindGameObjectWithTag("SelectionGrid");
        if (selectionGrid != null)
        {
            selectionGridImages = selectionGrid.GetComponentsInChildren<Image>()
                .Where(image => image.transform != selectionGrid.transform)
                .OrderBy(image => image.name)
                .ToArray();
            Debug.Log($"ButtonController: Selection grid images initialized. Count: {selectionGridImages.Length}");
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SelectionGrid' found.");
        }
    }

    private void InitializeSelectionGridButtons()
    {
        GameObject selectionGrid = GameObject.FindGameObjectWithTag("SelectionGrid");
        if (selectionGrid != null)
        {
            selectionGridButtons = selectionGrid.GetComponentsInChildren<Button>()
                .OrderBy(button => button.name)
                .ToArray();
            
            buttonToggledStates = new bool[selectionGridButtons.Length];
            originalButtonScales = new Vector3[selectionGridButtons.Length];
            
            for (int i = 0; i < selectionGridButtons.Length; i++)
            {
                buttonToggledStates[i] = false;
                originalButtonScales[i] = selectionGridButtons[i].transform.localScale;
            }
            Debug.Log($"ButtonController: Selection grid buttons initialized. Count: {selectionGridButtons.Length}");
        }
        else
        {
            Debug.LogWarning("No GameObject with tag 'SelectionGrid' found.");
        }
    }

    public void OnButtonClicked(int index)
    {
        Debug.Log($"ButtonController: Button clicked. Index: {index}");
        if (index >= 0 && index < selectionGridButtons.Length)
        {
            buttonToggledStates[index] = !buttonToggledStates[index];
            
            // Toggle opacity in shader
            float alpha = buttonToggledStates[index] ? ToggledAlpha : DefaultAlpha;
            selectionGridImages[index].material.SetFloat("_Alpha", alpha);
            
            // Toggle scale
            Vector3 targetScale = buttonToggledStates[index] 
                ? originalButtonScales[index] * ShrinkFactor 
                : originalButtonScales[index];
            selectionGridButtons[index].transform.localScale = targetScale;
            
            Debug.Log($"ButtonController: Button {index} toggled. New state: {buttonToggledStates[index]}");
        }
        else
        {
            Debug.LogWarning("Invalid button index.");
        }
    }

    public void OnSubmitButtonClicked()
    {
        Debug.Log("ButtonController: Submit button clicked");
        int updatedButtonsCount = 0;
        string firstUpdatedObjectName = null;
        for (int i = 0; i < selectionGridButtons.Length; i++)
        {
            if (buttonToggledStates[i])
            {
                buttonToggledStates[i] = false;
                selectionGridImages[i].sprite = GetNextEmoji();
                
                if (firstUpdatedObjectName == null)
                {
                    firstUpdatedObjectName = selectionGridImages[i].name;
                }
                
                // Reset button appearance
                selectionGridImages[i].material.SetFloat("_Alpha", DefaultAlpha);
                selectionGridButtons[i].transform.localScale = originalButtonScales[i];
                
                // Update the material color
                if (colorController != null)
                {
                    Color newColor = colorController.GetNextColor();
                    selectionGridImages[i].material.SetColor("_TargetColor", newColor);
                }
                updatedButtonsCount++;
            }
        }
        Debug.Log($"ButtonController: Updated {updatedButtonsCount} buttons, including {firstUpdatedObjectName}");
    }

    public Sprite GetNextEmoji()
    {
        if (emojiQueue.Count == 0)
        {
            Debug.Log("ButtonController: Emoji queue empty, reinitializing");
            InitializeEmojiQueue(); // Reinitialize if queue is empty
        }

        Sprite nextEmoji = emojiQueue.Dequeue();
        emojiQueue.Enqueue(nextEmoji); // Add back to the end for wrapping
        Debug.Log($"ButtonController: Next emoji retrieved. Remaining in queue: {emojiQueue.Count}");
        return nextEmoji;
    }
}
