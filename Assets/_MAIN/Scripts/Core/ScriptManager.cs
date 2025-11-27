using System.Collections.Generic;
using System.Collections;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScriptManager : MonoBehaviour
{
    [SerializeField]
    TextAsset scriptFile;
    [SerializeField]
    TextMeshProUGUI speakerText;
    [SerializeField]
    GameObject speakerSprite;
    [SerializeField]
    TextMeshProUGUI dialogueText;
    [SerializeField]
    private GameObject choiceButtonPrefab;
    [SerializeField]
    private Transform choiceButtonContainer;
    [SerializeField]
    private Image choiceBackground;
    [SerializeField]
    float charsPerSecond = 45f;

    public VisualNovelLayoutDirector director;
    private readonly float shakeAmount = 1.1f;
    private bool isChoiceAvailable = false;
    private Tween dialogueTween;
    private Script _currentScript;

    void Start()
    {
        speakerText.SetText(" ");
        speakerText.ForceMeshUpdate(true);
        dialogueText.SetText(" ");
        dialogueText.ForceMeshUpdate(true);

        StartCoroutine(TestAnim());

        _currentScript = ScriptParser.Parse(scriptFile.text);
        NextStep();
    }

    IEnumerator TestAnim()
    {
        director.AddCharacter("chino01", VisualNovelLayoutDirector.EntranceType.Center);
        yield return new WaitForSeconds(1f);
        director.AddCharacter("chino02", VisualNovelLayoutDirector.EntranceType.Left);
        yield return new WaitForSeconds(1f);
        director.AddCharacter("chino03", VisualNovelLayoutDirector.EntranceType.Right);
        yield return new WaitForSeconds(1f);
        director.RemoveCharacter("chino02", VisualNovelLayoutDirector.EntranceType.Left);
        yield return new WaitForSeconds(1f);
        director.RemoveCharacter("chino03", VisualNovelLayoutDirector.EntranceType.Right);
        yield return new WaitForSeconds(1f);
        director.PlayAction("chino01", VisualNovelLayoutDirector.ActionType.Jump);
        yield return new WaitForSeconds(1f);
        director.PlayAction("chino01", VisualNovelLayoutDirector.ActionType.Shake);
        yield return new WaitForSeconds(1f);
        director.PlayAction("chino01", VisualNovelLayoutDirector.ActionType.Nod);
        yield return new WaitForSeconds(1f);
        director.PlayAction("chino01", VisualNovelLayoutDirector.ActionType.Punch);
        yield return new WaitForSeconds(1f);
        director.AddCharacter("chino02", VisualNovelLayoutDirector.EntranceType.Left);
        yield return new WaitForSeconds(1f);
        director.AddCharacter("chino03", VisualNovelLayoutDirector.EntranceType.Center);
    }

    void Update()
    {
        DisplayEffects(dialogueText);
        if (!isChoiceAvailable && !IsPointerOverInteractiveUI() && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            if (dialogueTween.isAlive)
                dialogueTween.Complete();
            else
                NextStep();
        }

    }

    private void NextStep()
    {
        if (_currentScript.HasNextAction())
        {
            ScriptAction action = _currentScript.Continue();
            ExecuteAction(action);
            return;
        }

        Debug.Log("ScriptManager :: End of Script");
    }

    private void ExecuteAction(ScriptAction action)
    {
        if (action.Type == "scene")
        {
            string sceneName = action.GetParam("name");
            Debug.Log($"ScriptManager :: Change Scene: {sceneName}");
            NextStep();
            return;
        }
        if (action.Type == "bg")
        {
            string bgFile = action.GetParam("file");
            Debug.Log($"ScriptManager :: Change Background: {bgFile}");
            NextStep();
            return;
        }
        if (action.Type == "spk")
        {
            string speaker = action.GetParam("name");
            if (speaker == "")
                speakerSprite.SetActive(false);

            speakerText.SetText(speaker);
            speakerText.ForceMeshUpdate(true);
            NextStep();
            return;
        }
        if (action.Type == "msg")
        {
            string dialogue = action.GetParam("content");
            DisplayDialogue(dialogue);
            return;
        }
        if (action.Type == "goto")
        {
            string targetScene = action.GetParam("scene");
            _currentScript.JumpTo(targetScene);
            NextStep();
            return;
        }
        if (action.Type == "choices")
        {
            Debug.Log("ScriptManager :: Show Choices");
            isChoiceAvailable = true;

            // WTF.. is this shit
            Color tempColor = choiceBackground.color;
            tempColor.a = 0.8f;
            choiceBackground.color = tempColor;

            foreach (var choice in action.Choices)
            {
                string text = choice["content"];
                string target = choice["goto"];
                GameObject buttonObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                buttonObj.GetComponentInChildren<TextMeshProUGUI>().text = text;
                buttonObj
                    .GetComponent<Button>()
                    .onClick.AddListener(() =>
                    {
                        foreach (Transform child in choiceButtonContainer)
                            Destroy(child.gameObject);
                        isChoiceAvailable = false;

                        // shitty code
                        tempColor.a = 0f;
                        choiceBackground.color = tempColor;

                        _currentScript.JumpTo(target);
                        NextStep();
                    });
            }
            return;
        }
    }

    public void DebugReload()
    {
        speakerText.SetText(" ");
        speakerText.ForceMeshUpdate(true);
        dialogueText.SetText(" ");
        dialogueText.ForceMeshUpdate(true);

        _currentScript = ScriptParser.Parse(scriptFile.text);
    }

    private bool IsPointerOverInteractiveUI()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
            if (result.gameObject.GetComponent<Selectable>() != null)
                return true;

        return false;
    }

    private void DisplayDialogue(string text)
    {
        // Unity 내부 최적화로 인해 줄이 바뀔 시 LinkInfo 배열이 초기화되지 않음.
        // 따라서 수동으로 초기화를 수행.
        dialogueText.textInfo.linkInfo = new TMP_LinkInfo[0];
        dialogueText.SetText(text);
        dialogueText.ForceMeshUpdate(true);
        dialogueText.maxVisibleCharacters = 0;

        dialogueTween = Tween.Custom(
            startValue: 0f,
            endValue: dialogueText.textInfo.characterCount,
            duration: dialogueText.textInfo.characterCount / charsPerSecond,
            onValueChange: x => dialogueText.maxVisibleCharacters = Mathf.RoundToInt(x),
            ease: Ease.Linear
        );
    }

    private void DisplayEffects(TextMeshProUGUI text)
    {
        text.ForceMeshUpdate(true);

        TMP_TextInfo textInfo = text.textInfo;
        TMP_LinkInfo[] linkInfo = textInfo.linkInfo;

        Mesh mesh = text.mesh;
        Vector3[] vertices = mesh.vertices;

        foreach (var link in linkInfo)
        {
            string linkName = link.GetLinkID();
            int start = link.linkTextfirstCharacterIndex;
            int end = link.linkTextfirstCharacterIndex + link.linkTextLength;

            for (var i = start; i < end; i++)
            {
                TMP_CharacterInfo c = textInfo.characterInfo[i];
                int idx = c.vertexIndex;

                if (!c.isVisible)
                    continue; // 공백은 VertexIndex 0 Return -> Visible이 안 되므로

                if (linkName == "shake")
                {
                    Vector3 offset = new Vector3(
                        Random.Range(-shakeAmount, shakeAmount),
                        Random.Range(-shakeAmount, shakeAmount)
                    );
                    for (byte j = 0; j < 4; j++)
                        vertices[idx + j] += offset;
                }
            }
        }

        mesh.vertices = vertices;
        text.canvasRenderer.SetMesh(mesh);
    }
}
