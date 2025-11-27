using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using System.Collections;

public class VisualNovelLayoutDirector : MonoBehaviour
{
    // ========================= [Enums] =========================
    public enum EntranceType { Left, Right, BottomLeft, BottomRight, Center, Top }
    public enum ActionType { Jump, Shake, Nod, Punch }

    [Header("UI 연결")]
    public Transform characterPanel;
    public GameObject slotPrefab;

    [Header("설정")]
    public float charWidth = 350f;
    public float defaultDuration = 0.5f;
    public float moveDistance = 800f;

    // ========================= [1. 등장 (Entry)] =========================
    public void AddCharacter(string fileName, EntranceType type)
    {
        // 중복 방지
        if (FindSlot(fileName) != null)
        {
            Debug.LogWarning($"이미 존재하는 캐릭터입니다: {fileName}");
            return;
        }

        string path = "Images/Characters/" + fileName;
        Sprite loadedSprite = Resources.Load<Sprite>(path);

        if (loadedSprite != null)
        {
            StartCoroutine(SpawnRoutine(fileName, loadedSprite, type));
        }
        else
        {
            Debug.LogError($"이미지 로드 실패: {path}");
        }
    }

    private IEnumerator SpawnRoutine(string name, Sprite sprite, EntranceType type)
    {
        // 1. 슬롯 생성
        GameObject newSlot = Instantiate(slotPrefab, characterPanel);

        // 오브젝트 이름을 파일명(ID)으로 설정
        newSlot.name = name;

        LayoutElement layoutElement = newSlot.GetComponent<LayoutElement>();
        Image charImage = newSlot.transform.GetChild(0).GetComponent<Image>();

        // 2. 초기화
        charImage.sprite = sprite;
        charImage.SetNativeSize();
        layoutElement.preferredWidth = 0; layoutElement.minWidth = 0;

        // 3. 순서 재배치 (기존 로직 유지)
        int totalCount = characterPanel.childCount;
        switch (type)
        {
            case EntranceType.Left:
            case EntranceType.BottomLeft:
                newSlot.transform.SetSiblingIndex(0); break;
            case EntranceType.Right:
            case EntranceType.BottomRight:
                newSlot.transform.SetSiblingIndex(totalCount - 1); break;
            case EntranceType.Center:
            case EntranceType.Top:
                newSlot.transform.SetSiblingIndex((totalCount - 1) / 2); break;
        }

        // 4. 위치 잡기 및 애니메이션
        Vector2 startPos = GetDirectionVector(type);
        charImage.rectTransform.anchoredPosition = startPos;
        charImage.color = new Color(1, 1, 1, 0);

        yield return new WaitForEndOfFrame();

        Tween.Custom(layoutElement, 0f, charWidth, defaultDuration, (t, x) => t.preferredWidth = x, Ease.OutQuart);
        Tween.UIAnchoredPosition(charImage.rectTransform, Vector2.zero, defaultDuration, Ease.OutQuart);
        Tween.Alpha(charImage, 1f, defaultDuration);
    }

    // ========================= [2. 퇴장 (Exit)] =========================
    public void RemoveCharacter(string characterName, EntranceType exitTo)
    {
        Transform targetSlot = FindSlot(characterName);

        if (targetSlot != null)
        {
            StartCoroutine(ExitRoutine(targetSlot, exitTo));
        }
        else
        {
            Debug.LogWarning($"삭제 실패: '{characterName}' 캐릭터를 찾을 수 없습니다.");
        }
    }

    private IEnumerator ExitRoutine(Transform slotTransform, EntranceType exitTo)
    {
        // 중복 호출 방지를 위해 이름을 바꿔둠 (빠르게 연타했을 때 에러 방지)
        slotTransform.name = slotTransform.name + "_Removing";

        LayoutElement layoutElement = slotTransform.GetComponent<LayoutElement>();
        Image charImage = slotTransform.GetChild(0).GetComponent<Image>();
        Vector2 targetPos = GetDirectionVector(exitTo);

        // 이미지 날리기 & 투명화
        Tween.UIAnchoredPosition(charImage.rectTransform, targetPos, defaultDuration, Ease.OutQuart);
        Tween.Alpha(charImage, 0f, defaultDuration * 0.8f);

        // 공간 닫기
        yield return Tween.Custom(layoutElement, layoutElement.preferredWidth, 0f, defaultDuration,
            (t, x) => t.preferredWidth = x, Ease.OutQuart).ToYieldInstruction();

        Destroy(slotTransform.gameObject);
    }

    // ========================= [3. 액션 (Action)] =========================
    public void PlayAction(string characterName, ActionType action)
    {
        Transform targetSlot = FindSlot(characterName);

        if (targetSlot == null)
        {
            Debug.LogWarning($"액션 실패: '{characterName}' 캐릭터를 찾을 수 없습니다.");
            return;
        }

        RectTransform targetImageRect = targetSlot.GetChild(0).GetComponent<RectTransform>();

        // 기존 애니메이션 정지 및 초기화
        Tween.StopAll(targetImageRect);
        targetImageRect.anchoredPosition = Vector2.zero;

        switch (action)
        {
            case ActionType.Jump:
                // frequency: 2 (위로 갔다가 한두 번 띠용~ 하고 멈춤)
                Tween.PunchLocalPosition(targetImageRect, new Vector3(0, 100f, 0), 0.5f, frequency: 2);
                break;

            case ActionType.Shake:
                // 좌우 흔들기 (진동 횟수 10번)
                Tween.ShakeLocalPosition(targetImageRect, new Vector3(50f, 0, 0), 0.5f, frequency: 10);
                break;

            case ActionType.Nod:
                // (Sequence는 변경 없음)
                Sequence.Create()
                    .Chain(Tween.UIAnchoredPositionY(targetImageRect, -30f, 0.15f, Ease.OutQuad))
                    .Chain(Tween.UIAnchoredPositionY(targetImageRect, 0f, 0.15f, Ease.InQuad));
                break;

            case ActionType.Punch:
                // frequency: 1 (커졌다가 딱 한 번 출렁이고 복구됨)
                Tween.PunchScale(targetImageRect, new Vector3(0.2f, 0.2f, 0), 0.4f, frequency: 1);
                break;
        }
    }

    // [Helper] 이름으로 슬롯 찾기
    private Transform FindSlot(string name)
    {
        // CharacterPanel 바로 아래 자식들 중에서 이름을 검색
        return characterPanel.Find(name);
    }

    private Vector2 GetDirectionVector(EntranceType type)
    {
        switch (type)
        {
            case EntranceType.Left:
                return new Vector2(-moveDistance, 0);
            case EntranceType.Right:
                return new Vector2(moveDistance, 0);
            case EntranceType.Center:
            case EntranceType.BottomLeft:
            case EntranceType.BottomRight:
                return new Vector2(0, -moveDistance);
            case EntranceType.Top:
                return new Vector2(0, moveDistance);
            default:
                return Vector2.zero;
        }
    }
}