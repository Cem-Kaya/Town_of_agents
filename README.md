# Town_of_agents

## Code Metrics

### Lines of Code

- Total lines: 10,518
- Non-blank lines: 8,366
- Code lines (non-blank, non-comment): 7,234
- Comment lines: 1,132
- C# files analyzed: 81
- Generated with `python tools/loc_metrics.py --output metrics/loc_metrics.json --markdown`

<details>
<summary>Per-file LOC breakdown</summary>

| File | Total Lines | Non-blank Lines | Code Lines | Comment Lines |
| --- | --- | --- | --- | --- |
| Assets/Scripts/ActionResponse.cs | 54 | 44 | 29 | 15 |
| Assets/Scripts/BGMPlayer.cs | 19 | 17 | 17 | 0 |
| Assets/Scripts/ChatDemo.cs | 37 | 32 | 29 | 3 |
| Assets/Scripts/ChatHistoryItem.cs | 63 | 52 | 37 | 15 |
| Assets/Scripts/ChatMessageItem.cs | 21 | 19 | 18 | 1 |
| Assets/Scripts/ChatResponse.cs | 26 | 21 | 21 | 0 |
| Assets/Scripts/ChatUI.cs | 301 | 248 | 220 | 28 |
| Assets/Scripts/ChatUIController.cs | 356 | 292 | 271 | 21 |
| Assets/Scripts/ChickenAgent2D.cs | 216 | 179 | 155 | 24 |
| Assets/Scripts/ChickenSoccerManager2D.cs | 193 | 154 | 148 | 6 |
| Assets/Scripts/ChickenWanderArea.cs | 142 | 110 | 99 | 11 |
| Assets/Scripts/ConversationManager.cs | 169 | 142 | 124 | 18 |
| Assets/Scripts/Debug/InventoryDebugger.cs | 13 | 12 | 12 | 0 |
| Assets/Scripts/Debug/NPCPathGizmo.cs | 39 | 33 | 33 | 0 |
| Assets/Scripts/Debug/NavGridViewer.cs | 88 | 73 | 66 | 7 |
| Assets/Scripts/Door2D.cs | 125 | 98 | 94 | 4 |
| Assets/Scripts/FootstepOnCellChange.cs | 68 | 57 | 57 | 0 |
| Assets/Scripts/GoalTrigger2D.cs | 29 | 23 | 21 | 2 |
| Assets/Scripts/InteractableItem.cs | 117 | 100 | 97 | 3 |
| Assets/Scripts/Inventory.cs | 63 | 53 | 49 | 4 |
| Assets/Scripts/InventoryManager.cs | 75 | 66 | 65 | 1 |
| Assets/Scripts/InventoryUI.cs | 186 | 159 | 147 | 12 |
| Assets/Scripts/Item.cs | 63 | 50 | 48 | 2 |
| Assets/Scripts/ItemBreak.cs | 71 | 59 | 50 | 9 |
| Assets/Scripts/ItemSlot.cs | 123 | 104 | 99 | 5 |
| Assets/Scripts/KeyPickup.cs | 32 | 27 | 26 | 1 |
| Assets/Scripts/KickableChicken2D.cs | 112 | 88 | 81 | 7 |
| Assets/Scripts/LLMTools.cs | 188 | 169 | 163 | 6 |
| Assets/Scripts/LLMUtils.cs | 79 | 67 | 66 | 1 |
| Assets/Scripts/MessageBubble.cs | 85 | 70 | 59 | 11 |
| Assets/Scripts/MpcLlmController.cs | 207 | 172 | 158 | 14 |
| Assets/Scripts/NPCInteractable.cs | 440 | 356 | 337 | 19 |
| Assets/Scripts/NPCShortcut.cs | 58 | 47 | 42 | 5 |
| Assets/Scripts/NPCTalkAudio.cs | 68 | 54 | 52 | 2 |
| Assets/Scripts/NPCWalker.cs | 313 | 262 | 251 | 11 |
| Assets/Scripts/Navgrid.cs | 134 | 115 | 107 | 8 |
| Assets/Scripts/NotePickup.cs | 31 | 26 | 25 | 1 |
| Assets/Scripts/Player.cs | 183 | 151 | 145 | 6 |
| Assets/Scripts/PlayerAutoNavigator.cs | 262 | 229 | 219 | 10 |
| Assets/Scripts/PlayerFacing2D.cs | 74 | 62 | 60 | 2 |
| Assets/Scripts/PromptInfo.cs | 35 | 30 | 30 | 0 |
| Assets/Scripts/QuickAccess.cs | 61 | 51 | 45 | 6 |
| Assets/Scripts/ShortcutToNPC.cs | 33 | 27 | 27 | 0 |
| Assets/Scripts/ShortcutToggleUI.cs | 44 | 35 | 33 | 2 |
| Assets/Scripts/SmartCamera2D.cs | 139 | 113 | 92 | 21 |
| Assets/Scripts/UnityMainThreadDispatcher.cs | 121 | 98 | 68 | 30 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01.cs | 128 | 91 | 63 | 28 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01_UGUI.cs | 135 | 95 | 59 | 36 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark02.cs | 97 | 71 | 65 | 6 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark03.cs | 92 | 74 | 73 | 1 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark04.cs | 85 | 62 | 41 | 21 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs | 292 | 223 | 198 | 25 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ChatController.cs | 51 | 36 | 31 | 5 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/DropdownSample.cs | 19 | 15 | 15 | 0 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/EnvMapAnimator.cs | 35 | 27 | 23 | 4 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ObjectSpin.cs | 67 | 54 | 49 | 5 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ShaderPropAnimator.cs | 51 | 38 | 33 | 5 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/SimpleScript.cs | 58 | 39 | 24 | 15 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/SkewTextExample.cs | 158 | 113 | 95 | 18 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_DigitValidator.cs | 27 | 24 | 19 | 5 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_ExampleScript_01.cs | 64 | 46 | 37 | 9 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_FrameRateCounter.cs | 134 | 102 | 91 | 11 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_PhoneNumberValidator.cs | 105 | 100 | 93 | 7 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventCheck.cs | 73 | 57 | 56 | 1 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventHandler.cs | 263 | 208 | 162 | 46 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextInfoDebugTool.cs | 652 | 506 | 395 | 111 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_A.cs | 157 | 117 | 94 | 23 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs | 547 | 413 | 237 | 176 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_UiFrameRateCounter.cs | 125 | 97 | 96 | 1 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMPro_InstructionOverlay.cs | 84 | 61 | 55 | 6 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TeleType.cs | 83 | 55 | 40 | 15 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextConsoleSimulator.cs | 121 | 92 | 79 | 13 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshProFloatingText.cs | 223 | 167 | 135 | 32 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshSpawner.cs | 79 | 59 | 46 | 13 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexColorCycler.cs | 84 | 61 | 48 | 13 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexJitter.cs | 175 | 131 | 105 | 26 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeA.cs | 161 | 122 | 99 | 23 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeB.cs | 185 | 141 | 112 | 29 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexZoom.cs | 192 | 144 | 112 | 32 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/WarpTextExample.cs | 144 | 102 | 87 | 15 |
| Assets/vids/SceneVideoController.cs | 216 | 177 | 175 | 2 |

</details>

### Function Metrics

- Total functions/methods: 397
- Average LOC per function: 14.49 (median 8)
- Average cyclomatic complexity (CCN): 3.68 (median 2)
- Maximum CCN: 33 in `TMPro.Examples::CameraController::GetPlayerInput()` (`Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs:126`)
- Maximum LOC: 116 in `TMPro.Examples::TMP_TextSelector_B::LateUpdate()` (`Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs:81`)
- Comment density across all C# files: 10.76%
- Generated with `python tools/function_metrics.py --output metrics/function_metrics.json --markdown --top 5`

<details>
<summary>Top 5 functions by cyclomatic complexity</summary>

| Function | File | LOC | CCN | Parameters |
| --- | --- | --- | --- | --- |
| TMPro.Examples::CameraController::GetPlayerInput() | Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs:126 | 113 | 33 | 0 |
| TMPro::TMP_PhoneNumberValidator::Validate( ref string text , ref int pos , char ch) | Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_PhoneNumberValidator.cs:15 | 83 | 32 | 3 |
| TMPro.Examples::TMP_TextSelector_B::LateUpdate() | Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs:81 | 116 | 26 | 0 |
| NavGrid::Erode( int cells) | Assets/Scripts/Navgrid.cs:90 | 28 | 18 | 1 |
| TMPro.Examples::TMP_TextSelector_A::LateUpdate() | Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_A.cs:30 | 63 | 17 | 0 |

</details>

## Unity Assets

- Sprites: 116
- Textures: 116
- Prefabs: 84
- Scenes: 45
- Materials: 27
- Audio clips: 11
- ScriptableObjects (.asset detected): 2,842
- Generated with `python tools/asset_inventory.py --output metrics/asset_counts.json --markdown`

<details>
<summary>Asset type counts</summary>

| Asset Type | Count |
| --- | --- |
| animations | 0 |
| asmdef | 0 |
| audio | 11 |
| controllers | 0 |
| materials | 27 |
| models | 0 |
| prefabs | 84 |
| scenes | 45 |
| scriptable_objects | 2842 |
| scripts | 81 |
| shaders | 14 |
| sprites | 116 |
| textures | 116 |

</details>

### Type Declarations

- Classes: 93
- Structs: 1
- Interfaces: 0
- Records: 0
- Total type declarations: 94
- Generated with `python tools/class_count.py --output metrics/class_counts.json --markdown`

<details>
<summary>Per-file type declarations</summary>

| File | Declarations |
| --- | --- |
| Assets/Scripts/ActionResponse.cs | ActionResponse |
| Assets/Scripts/BGMPlayer.cs | BGMPlayer |
| Assets/Scripts/ChatDemo.cs | ChatDemo |
| Assets/Scripts/ChatHistoryItem.cs | ChatHistoryItem |
| Assets/Scripts/ChatMessageItem.cs | ChatMessageItem |
| Assets/Scripts/ChatResponse.cs | ChatResponse |
| Assets/Scripts/ChatUI.cs | ChatUI |
| Assets/Scripts/ChatUIController.cs | ChatUIController |
| Assets/Scripts/ChickenAgent2D.cs | ChickenAgent2D |
| Assets/Scripts/ChickenSoccerManager2D.cs | ChickenSoccerManager2D |
| Assets/Scripts/ChickenWanderArea.cs | ChickenWanderArea |
| Assets/Scripts/ConversationManager.cs | ConversationManager |
| Assets/Scripts/Debug/InventoryDebugger.cs | InventoryDebugger |
| Assets/Scripts/Debug/NPCPathGizmo.cs | NPCPathGizmo |
| Assets/Scripts/Debug/NavGridViewer.cs | NavGridViewer |
| Assets/Scripts/Door2D.cs | Door2D |
| Assets/Scripts/FootstepOnCellChange.cs | FootstepOnCellChange |
| Assets/Scripts/GoalTrigger2D.cs | GoalTrigger2D |
| Assets/Scripts/InteractableItem.cs | InteractableItem |
| Assets/Scripts/Inventory.cs | Inventory, NoteData |
| Assets/Scripts/InventoryManager.cs | InventoryManager |
| Assets/Scripts/InventoryUI.cs | InventoryUI, KeyVisual |
| Assets/Scripts/Item.cs | Item |
| Assets/Scripts/ItemBreak.cs | ItemBreak |
| Assets/Scripts/ItemSlot.cs | ItemSlot |
| Assets/Scripts/KeyPickup.cs | KeyPickup |
| Assets/Scripts/KickableChicken2D.cs | KickableChicken2D |
| Assets/Scripts/LLMTools.cs | LLMTools |
| Assets/Scripts/LLMUtils.cs | LLMUtils |
| Assets/Scripts/MessageBubble.cs | MessageBubble |
| Assets/Scripts/MpcLlmController.cs | MpcLlmController |
| Assets/Scripts/NPCInteractable.cs | NPCInteractable |
| Assets/Scripts/NPCShortcut.cs | NPCShortcut, Row |
| Assets/Scripts/NPCTalkAudio.cs | NPCTalkAudio |
| Assets/Scripts/NPCWalker.cs | NPCWalkerGrid, PriorityQueue<T> |
| Assets/Scripts/Navgrid.cs | NavGrid |
| Assets/Scripts/NotePickup.cs | NotePickup |
| Assets/Scripts/Player.cs | Player |
| Assets/Scripts/PlayerAutoNavigator.cs | PlayerAutoNavigator, PriorityQueue<T> |
| Assets/Scripts/PlayerFacing2D.cs | PlayerFacing2D |
| Assets/Scripts/PromptInfo.cs | PromptInfo |
| Assets/Scripts/QuickAccess.cs | QuickAccess, ActionDef |
| Assets/Scripts/ShortcutToNPC.cs | ShortcutToNPC, Binding |
| Assets/Scripts/ShortcutToggleUI.cs | ShortcutToggleUI |
| Assets/Scripts/SmartCamera2D.cs | SmartCamera2D |
| Assets/Scripts/UnityMainThreadDispatcher.cs | UnityMainThreadDispatcher |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01.cs | Benchmark01 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark01_UGUI.cs | Benchmark01_UGUI |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark02.cs | Benchmark02 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark03.cs | Benchmark03 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/Benchmark04.cs | Benchmark04 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/CameraController.cs | CameraController |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ChatController.cs | ChatController |
| Assets/TextMesh Pro/Examples & Extras/Scripts/DropdownSample.cs | DropdownSample |
| Assets/TextMesh Pro/Examples & Extras/Scripts/EnvMapAnimator.cs | EnvMapAnimator |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ObjectSpin.cs | ObjectSpin |
| Assets/TextMesh Pro/Examples & Extras/Scripts/ShaderPropAnimator.cs | ShaderPropAnimator |
| Assets/TextMesh Pro/Examples & Extras/Scripts/SimpleScript.cs | SimpleScript |
| Assets/TextMesh Pro/Examples & Extras/Scripts/SkewTextExample.cs | SkewTextExample |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_DigitValidator.cs | TMP_DigitValidator |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_ExampleScript_01.cs | TMP_ExampleScript_01 |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_FrameRateCounter.cs | TMP_FrameRateCounter |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_PhoneNumberValidator.cs | TMP_PhoneNumberValidator |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventCheck.cs | TMP_TextEventCheck |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextEventHandler.cs | TMP_TextEventHandler, CharacterSelectionEvent, SpriteSelectionEvent, WordSelectionEvent, LineSelectionEvent, LinkSelectionEvent |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextInfoDebugTool.cs | TMP_TextInfoDebugTool |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_A.cs | TMP_TextSelector_A |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_TextSelector_B.cs | TMP_TextSelector_B |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMP_UiFrameRateCounter.cs | TMP_UiFrameRateCounter |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TMPro_InstructionOverlay.cs | TMPro_InstructionOverlay |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TeleType.cs | TeleType |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextConsoleSimulator.cs | TextConsoleSimulator |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshProFloatingText.cs | TextMeshProFloatingText |
| Assets/TextMesh Pro/Examples & Extras/Scripts/TextMeshSpawner.cs | TextMeshSpawner |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexColorCycler.cs | VertexColorCycler |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexJitter.cs | VertexJitter, VertexAnim |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeA.cs | VertexShakeA |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexShakeB.cs | VertexShakeB |
| Assets/TextMesh Pro/Examples & Extras/Scripts/VertexZoom.cs | VertexZoom |
| Assets/TextMesh Pro/Examples & Extras/Scripts/WarpTextExample.cs | WarpTextExample |
| Assets/vids/SceneVideoController.cs | SceneVideoController |

</details>
