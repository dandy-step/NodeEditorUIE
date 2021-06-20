using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;

public class DialogueUIHookup : MonoBehaviour
{
    //simple script that takes the parsed data into a game-friendly format and plays the sequence, with basic game logic

    public DlgFormatData dlgData = null;
    public int currEntryIndex = 0;
    public DlgBaseEntry currEntryBase;
    public DlgBaseEntry lastEntryBase;
    public int lastEntryIndex = -1;
    public ConversationUIManager uiManager;
    public GameObject characterParentGO;
    public CharacterData[] characterData;
    public EventSystem events;
    private Stack<GameObject> spawnedChoiceItems;
    public List<GameObject> speakerPositions;
    bool idleInit = false;
    GameObject speakerIndicator;

    void Start()
    {
        if (dlgData == null) {
            Debug.Log("No dlg data set on UI Hookup");
        }

        if ((speakerPositions == null) || (speakerPositions.Count <= 0)) {
            Debug.Log("Please set up speaker positions!");
        }

        //load character AssetBundle
        Debug.Log(Application.streamingAssetsPath);
        AssetBundle charAssetBundle = AssetBundle.LoadFromFile("Assets/AssetBundles/characterbundle");
        characterParentGO = new GameObject("Characters");
        characterData = new CharacterData[dlgData.characterCount];
        int availablePositions = 1;
        for (int i = 0; i < dlgData.charPrefabPath.Count; i++) {
            GameObject character;
            if (dlgData.charPrefabPath[i].Length > 0) {
                character = (GameObject)Instantiate(charAssetBundle.LoadAsset(Path.GetFileNameWithoutExtension(dlgData.charPrefabPath[i])));
                characterData[i] = character.GetComponent<Character>().data;

                //move character to appropriate spot
                if (i == dlgData.mainSpeaker) {
                    character.transform.position = speakerPositions[0].transform.position;
                    character.transform.rotation = speakerPositions[0].transform.rotation;
                } else {
                    if (availablePositions < speakerPositions.Count) {
                        character.transform.position = speakerPositions[availablePositions].transform.position;
                        character.transform.rotation = speakerPositions[availablePositions].transform.rotation;
                        availablePositions++;
                    } else {
                        Debug.Log("Not enough speaker positions set up for the number of characters in the file!");
                    }
                }
            } else {
                character = (GameObject)Instantiate(new GameObject());
                character.name = "Narrator/Empty";
            }

            character.transform.parent = characterParentGO.transform;
        }

        events = FindObjectOfType<EventSystem>();
        spawnedChoiceItems = new Stack<GameObject>();
    }

    void Update()
    {
        if (!idleInit) {
            for (int i = 0; i < dlgData.characterCount; i++) {
                if (!string.IsNullOrEmpty(dlgData.charIdles[i])) {
                    Animation animation = characterParentGO.transform.GetChild(i).GetComponent<Animation>();
                    if (animation) {
                        animation.wrapMode = WrapMode.Loop;
                        animation.CrossFade(dlgData.charIdles[i], 0.55f, PlayMode.StopAll);
                        animation.GetClip(dlgData.charIdles[i]).wrapMode = WrapMode.Loop;
                    }
                }
            }

            idleInit = true;
        }

        if (currEntryIndex < dlgData.dialogueEntries.Count) {
            currEntryBase = dlgData.dialogueEntries[currEntryIndex];
            System.Type entryType = currEntryBase.GetType();

            if (entryType == typeof(DlgDialogueEntry)) {
                DlgDialogueEntry currEntry = (DlgDialogueEntry)currEntryBase;
                uiManager.SetDialogText(currEntry.text);
                uiManager.SetNameBoxVisible(true);
                if (currEntry.customSpeakerLabel.Length > 0) {
                    uiManager.SetSpeakerName(currEntry.customSpeakerLabel);
                } else if (!currEntry.hasNoCharacterModel) {
                    uiManager.SetSpeakerName(characterData[currEntry.speakerIndex].characterName);
                } else {
                    uiManager.SetNameBoxVisible(false);
                }

                if (Input.anyKeyDown) {
                    currEntryIndex++;
                    return;
                }

                if (lastEntryIndex != currEntryIndex) {
                    if ((currEntry.supportsAnimations) && (currEntry.animationClipName != "")) {
                        if (currEntry.animationClipName != "INVALID") {
                            Animation animation = characterParentGO.transform.GetChild(currEntry.speakerIndex).GetComponent<Animation>();
                            if (animation) {
                                animation.wrapMode = WrapMode.Loop;
                                animation.CrossFade(currEntry.animationClipName, 0.55f, PlayMode.StopAll);
                                animation.GetClip(currEntry.animationClipName).wrapMode = WrapMode.Loop;
                            }
                        }
                    }

                    if (speakerIndicator != null) {
                        Destroy(speakerIndicator);
                    }

                    if (currEntry.supportsAnimations) {
                        speakerIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        speakerIndicator.GetComponent<MeshRenderer>().material.color = Color.red;
                        speakerIndicator.transform.localScale *= 0.25f;
                        speakerIndicator.transform.position = new Vector3(characterParentGO.transform.GetChild(currEntry.speakerIndex).transform.position.x, characterParentGO.transform.GetChild(currEntry.speakerIndex).transform.GetComponentInChildren<SkinnedMeshRenderer>().bounds.max.y + .1f, characterParentGO.transform.GetChild(currEntry.speakerIndex).transform.position.z);
                    }
                }
            } else if (entryType == typeof(DlgMultipleChoiceEntry)) {
                //entry type is multiple choice
                DlgMultipleChoiceEntry currEntry = (DlgMultipleChoiceEntry)currEntryBase;
                uiManager.choicePanel.gameObject.SetActive(true);

                if (lastEntryIndex != currEntryIndex) {
                    uiManager.SetDialogText(currEntry.choiceQuestion);
                    uiManager.SetNameBoxVisible(false);

                    for (int i = 0; i < currEntry.choiceCount; i++) {
                        spawnedChoiceItems.Push(Instantiate(uiManager.choiceUIPrefab));
                        spawnedChoiceItems.Peek().transform.SetParent(uiManager.choicePanel.transform);
                        Text textMesh = spawnedChoiceItems.Peek().GetComponentInChildren<Text>();
                        textMesh.text = currEntry.choiceText[i];
                    }
                }

                if (events.currentSelectedGameObject) {
                    for (int i = 0; i < spawnedChoiceItems.Count; i++) {
                        Destroy(spawnedChoiceItems.Pop());
                    }

                    uiManager.choicePanel.gameObject.SetActive(false);
                    currEntryIndex++;
                    uiManager.SetDialogBoxVisible(true);
                    uiManager.SetNameBoxVisible(true);
                    return;
                }
            }

            lastEntryIndex = currEntryIndex;
            uiManager.UpdateLayout();
        }
    }
}
