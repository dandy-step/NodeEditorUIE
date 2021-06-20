using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConversationUIManager : MonoBehaviour {
    public Text dialogText;
    public Text speakerName;
    public RectTransform choicePanel;
    public GameObject choiceUIPrefab;

    public IEnumerator TextFadeIn() {
        yield return null;
    }

    public void SetDialogBoxVisible(bool visible) {
        dialogText.gameObject.SetActive(visible);
    }

    public void SetNameBoxVisible(bool visible) {
        speakerName.gameObject.SetActive(visible);
        speakerName.transform.parent.gameObject.SetActive(visible);
    }

    public void SetDialogText(string text) {
        dialogText.text = text;
    }

    public void UpdateLayout() {
        LayoutRebuilder.ForceRebuildLayoutImmediate(speakerName.GetComponent<RectTransform>());
    }

    public void SetSpeakerName(string name) {
        speakerName.text = name;
        //StartCoroutine("TextFadeIn", new object)
    }
}
