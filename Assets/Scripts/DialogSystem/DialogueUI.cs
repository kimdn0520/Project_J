using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DialogSystem
{
    /// <summary>
    /// Handles the visual representation of the dialogue.
    /// Manages the text boxes, typewriter effects, and dynamic choice buttons.
    /// </summary>
    public class DialogueUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject dialoguePanel;

        [Header("Text Components")]
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private GameObject nextIndicator; // Flashing arrow indicating the text is fully printed

        [Header("Choice Configuration")]
        [SerializeField] private Button choiceButtonPrefab;
        [SerializeField] private Transform choiceButtonContainer;

        [Header("Typewriter Settings")]
        [SerializeField] private float charactersPerSecond = 30f;

        private List<Button> activeButtons = new List<Button>();
        private bool isTyping = false;
        private bool skipRequested = false;
        private Action<int> onChoiceSelected;
        private bool isInputCooldown = false;

        private void Awake()
        {
            HideDialogue();
            HideChoices();
            if (nextIndicator != null) nextIndicator.SetActive(false);
        }

        private void Update()
        {
            // If player clicks or presses advance keys while typing, skip the typewriter effect
            if (isTyping && CheckSkipOrAdvanceInput())
            {
                skipRequested = true;
            }
        }

        private bool CheckSkipOrAdvanceInput()
        {
            if (isInputCooldown) return false;

            #if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;

            if (keyboard != null && (keyboard.spaceKey.wasPressedThisFrame || keyboard.zKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                return true;
            }
            #else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            {
                return true;
            }
            #endif
            return false;
        }

        private async UniTaskVoid TriggerInputCooldown()
        {
            isInputCooldown = true;
            // Wait 1 frame to clear the input state across scene frame transition
            await UniTask.Yield(PlayerLoopTiming.Update);
            isInputCooldown = false;
        }

        public void ShowDialogue()
        {
            dialoguePanel.SetActive(true);
        }

        public void HideDialogue()
        {
            dialoguePanel.SetActive(false);
            if (nextIndicator != null) nextIndicator.SetActive(false);
        }

        public void ShowChoices()
        {
            if (choiceButtonContainer != null)
            {
                choiceButtonContainer.gameObject.SetActive(true);
            }
        }

        public void HideChoices()
        {
            if (choiceButtonContainer != null)
            {
                choiceButtonContainer.gameObject.SetActive(false);
            }
            ClearChoices();
        }

        private void ClearChoices()
        {
            foreach (var btn in activeButtons)
            {
                if (btn != null) Destroy(btn.gameObject);
            }
            activeButtons.Clear();
        }

        /// <summary>
        /// Displays the speaker name and text using a typewriter effect.
        /// </summary>
        public async UniTask DisplayDialogueNodeAsync(DialogueNode node, CancellationToken token)
        {
            ShowDialogue(); // Ensure panel is active before showing content
            skipRequested = false;
            if (nextIndicator != null) nextIndicator.SetActive(false);

            // Trigger cooldown to prevent advance keypress from immediately skipping this text
            TriggerInputCooldown().Forget();

            // Handle narration style vs character dialogue style
            if (string.IsNullOrEmpty(node.speaker))
            {
                if (speakerText != null && speakerText.gameObject.activeSelf)
                {
                    speakerText.gameObject.SetActive(false);
                }
            }
            else
            {
                if (speakerText != null)
                {
                    speakerText.gameObject.SetActive(true);
                    speakerText.text = node.speaker;
                }
            }

            // Start typing
            await RunTypewriterAsync(node.text, token);

            if (nextIndicator != null)
            {
                nextIndicator.SetActive(true);
            }
        }

        private async UniTask RunTypewriterAsync(string text, CancellationToken token)
        {
            isTyping = true;
            dialogueText.text = "";

            float delayBetweenChars = 1f / charactersPerSecond;
            int charIndex = 0;

            while (charIndex < text.Length)
            {
                if (skipRequested)
                {
                    dialogueText.text = text;
                    break;
                }

                // If rich text tag (e.g. <color=red>) is detected, type it out instantly
                if (text[charIndex] == '<')
                {
                    int tagCloseIndex = text.IndexOf('>', charIndex);
                    if (tagCloseIndex != -1)
                    {
                        charIndex = tagCloseIndex + 1;
                        dialogueText.text = text.Substring(0, charIndex);
                        continue;
                    }
                }

                dialogueText.text += text[charIndex];
                charIndex++;

                // Delay between letters
                await UniTask.Delay(TimeSpan.FromSeconds(delayBetweenChars), cancellationToken: token);
            }

            isTyping = false;
            skipRequested = false;
        }

        /// <summary>
        /// Populates choice buttons on the screen and triggers the callback when chosen.
        /// </summary>
        public void DisplayChoices(List<DialogueChoice> visibleChoices, Action<int> onSelect)
        {
            ClearChoices();
            ShowChoices();

            onChoiceSelected = onSelect;

            for (int i = 0; i < visibleChoices.Count; i++)
            {
                int index = i;
                DialogueChoice choice = visibleChoices[i];

                Button btn = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                btn.gameObject.SetActive(true);
                
                TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                if (btnText != null) btnText.text = choice.text;

                btn.onClick.AddListener(() =>
                {
                    HideChoices();
                    onChoiceSelected?.Invoke(index);
                });

                activeButtons.Add(btn);
            }
        }

        /// <summary>
        /// Waits for player to click or press Space/Z to advance dialogue.
        /// </summary>
        public async UniTask WaitForPlayerAdvanceAsync(CancellationToken token)
        {
            // Small frame delay to prevent double skipping from the keypress that triggered dialogue
            await UniTask.Yield(PlayerLoopTiming.Update, token);

            while (true)
            {
                if (CheckSkipOrAdvanceInput())
                {
                    if (!isTyping)
                    {
                        break;
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
    }
}
