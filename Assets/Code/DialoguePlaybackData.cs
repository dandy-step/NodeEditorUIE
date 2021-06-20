using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using UnityEditor;

public class DialoguePlaybackData : MonoBehaviour
{
    public string dlgFilePath = "";
    public Scene editorScene;
    public GameObject dlgUIPrefab;
    public byte[] data = null;
    [SerializeField] List<GameObject> speakerPositionList;

    void LoadDlg() {
        Debug.Log("PLAY DIALOGUE IN GAMEMODE " + dlgFilePath);
    }

    void Start()
    {
        if ((speakerPositionList == null) || (speakerPositionList.Count == 0)) {
            speakerPositionList = new List<GameObject>();
            Debug.Log("No speaker positions, please set the variables");
        }

        //restore old scene when we leave
        if (dlgFilePath != "") {
            //read from playback file ourselves if we don't have the data array from editor
            if (data == null) {
                FileStream newFile = File.OpenRead(dlgFilePath);
                byte[] data = new byte[newFile.Length + 1];
                newFile.Read(data, 0, (int)newFile.Length);
            }

            //TextAsset test = Resources.Load<TextAsset>("/playback.dlg");
            DlgFormatParser parser = new DlgFormatParser();
            DlgFormatData parsedData = parser.ParseDlgFile(data);
            GameObject uiObject = GameObject.Instantiate(dlgUIPrefab);

            DialogueUIHookup uiHook = gameObject.AddComponent<DialogueUIHookup>();
            uiHook.speakerPositions = speakerPositionList;
            uiHook.dlgData = parsedData;
            uiHook.uiManager = uiObject.GetComponent<ConversationUIManager>();
        }
    }
}
