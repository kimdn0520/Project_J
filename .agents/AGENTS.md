# Antigravity Workspace Rules & Project Status

## 📌 Project Context
This is a Unity 2D/3D Horror Tsukur game project.
We have designed and fully implemented a custom decoupled **Dialogue & Node Editor System** in `Assets/Scripts/DialogSystem/`.

---

## 📂 Active Dialogue System Architecture
All scripts reside in [Assets/Scripts/DialogSystem/](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/) folder.

1. **[IInteractable.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/IInteractable.cs)**: Polymorphic interface for all player interactions (NPCs, examining spots, drawers).
2. **[DialogueModel.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueModel.cs)**: Data models for dialogue nodes and branching choices.
3. **[DialogueDatabase.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueDatabase.cs)**: Cache database using Dictionary for $O(1)$ fast node retrieval.
4. **[DialogueManager.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueManager.cs)**: Singleton manager controlling progress loops (using UniTask) and condition evaluations.
5. **[DialogueUI.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueUI.cs)**: Typewriter effect (ignores Rich Text HTML tags), choice button spawning, and New Input System (Space/Z/Enter keys).
6. **[InteractionTrigger.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/InteractionTrigger.cs)**: Scene object interactable component. Supports conditional dialogue overrides.
7. **[AreaNarrativeTrigger.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/AreaNarrativeTrigger.cs)**: Zone-based (2D/3D Collider Trigger) narration activation.
8. **[DialogueEventDispatcher.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueEventDispatcher.cs)**: Observer-dispatcher to trigger visual/audio gameplay actions (camera shake, jump scare).
9. **[DialogueObfuscation.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueObfuscation.cs)**: Symmetric XOR encryption helper to prevent story leak datamining.
10. **Editor Tooling** (in [Editor/](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/Editor/) directory):
    * **[DialogueContainerSO.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/DialogueContainerSO.cs)**: ScriptableObject holding graph nodes and positions.
    * **[DialogueGraphView.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/Editor/DialogueGraphView.cs)**: Canvas implementation for visual graph editing.
    * **[DialogueGraphEditorWindow.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/Editor/DialogueGraphEditorWindow.cs)**: Custom window UI for node editor.
    * **[DialogueBinaryExporter.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/Editor/DialogueBinaryExporter.cs)**: Compiles all SOs in the project into an obfuscated binary file (`dialogues.bin`).
    * **[DialogueSampleGenerator.cs](file:///C:/Users/kimdn/My%20Project/Assets/Scripts/DialogSystem/Editor/DialogueSampleGenerator.cs)**: Automatically wires a mansion horror scenario SampleGraph asset for testing.

---

## ⚙️ Development Workflows & State
- **Editor Execution**: The `DialogueManager` loads and converts nodes directly from the assigned `editorDialogueGraph` ScriptableObject to ensure instant testing without compiling to binary.
- **Build Execution**: The `DialogueManager` reads from `binaryDialogueAsset` (`dialogues.bin`), decrypts the contents in memory, and caches nodes directly to Dictionary. (No raw JSON file writing is used).
- **Controls**: Advance input uses `Space`, `Z`, and `Enter`. Mouse click is disabled to maintain Tsukur genre feel.
- **Input Double Skip Protection**: An `isInputCooldown` flag blocks input for 1 frame upon node transitions to prevent a single spacebar press from skipping multiple dialogues.
- **UI Design**: The backing `choicePanel` has been removed; choice containers are toggled directly via `choiceButtonContainer.gameObject`.
- **Status**: The compiler tests show **zero errors**.
