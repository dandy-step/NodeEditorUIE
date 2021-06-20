using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "characterData", menuName = "Custom Editors/Character Data")]
public class CharacterData : ScriptableObject
{
    [HideInInspector]public string prefabPath;
    public string characterName = "";
    public string characterLongName = "";
    public string characterDescription = "";
    public Color editorColor = Color.gray;
    public float editorAdvance = 0f;
}
