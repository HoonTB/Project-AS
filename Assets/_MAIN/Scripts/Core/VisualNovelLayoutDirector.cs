using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using System.Collections;

public class VisualNovelLayoutDirector : MonoBehaviour
{
    [Header("UI 연결")]
    public Transform characterPanel; // Horizontal Layout Group이 달린 부모
    public GameObject slotPrefab;    // 슬롯 프리팹 (빈 부모 + 이미지 자식)

    [Header("연출 설정")]
    public float charWidth = 350f;      // 캐릭터 하나가 차지할 최종 너비
    public float enterDuration = 0.6f;  // 등장(위로 올라옴 + 투명도) 시간
    public float slideDuration = 0.5f;  // 옆으로 밀려나는 시간
    public float startOffsetY = -100f;  // 시작 Y 위치 (화면 아래쪽 오프셋)

    // 파일명으로 캐릭터 등장시키기
    public void AddCharacter(string fileName)
    {
        // 1. 리소스 로드
        Sprite loadedSprite = Resources.Load<Sprite>("Images/Characters/" + fileName);

        if (loadedSprite != null)
        {
            StartCoroutine(SpawnRoutine(loadedSprite));
        }
        else
        {
            Debug.LogError($"이미지 로드 실패: Resources/Images/Characters/{fileName} 파일을 확인하세요.");
        }
    }

    private IEnumerator SpawnRoutine(Sprite sprite)
    {
        // 2. 슬롯 생성 (Panel의 자식으로)
        GameObject newSlot = Instantiate(slotPrefab, characterPanel);

        // 컴포넌트 찾아오기
        LayoutElement layoutElement = newSlot.GetComponent<LayoutElement>();
        Image charImage = newSlot.transform.GetChild(0).GetComponent<Image>(); // 자식에 있는 이미지

        // 3. 초기 세팅
        charImage.sprite = sprite;
        charImage.SetNativeSize(); // 이미지 원본 비율 맞춤

        // [중요] 공간을 0으로 만들어둠 -> 기존 캐릭터들이 아직 움직이지 않음
        layoutElement.preferredWidth = 0;
        layoutElement.minWidth = 0;

        // [중요] 이미지는 화면 아래(startOffsetY)에 배치하고 투명하게 설정
        // 슬롯(부모)은 Layout에 묶여도, 이미지(자식)는 자유롭게 움직일 수 있음
        charImage.rectTransform.anchoredPosition = new Vector2(0, startOffsetY);
        charImage.color = new Color(1, 1, 1, 0); // Alpha 0 (투명)

        // UI 갱신 대기 (필수)
        yield return new WaitForEndOfFrame();

        // 4. 애니메이션 실행

        // A. 공간 벌리기 (기존 캐릭터들이 스르륵 밀려남)
        Tween.Custom(layoutElement, layoutElement.preferredWidth, charWidth, slideDuration,
                    (target, x) => target.preferredWidth = x,
                    ease: Ease.OutQuart);

        // B. 이미지 등장 (아래에서 위로 올라오며 선명해짐)
        Tween.UIAnchoredPositionY(charImage.rectTransform, 0, enterDuration, ease: Ease.OutBack);
        Tween.Alpha(charImage, 1, enterDuration);
    }

    // (참고) 캐릭터 퇴장 기능
    public void RemoveCharacter(int index)
    {
        if (index < characterPanel.childCount)
        {
            Transform targetSlot = characterPanel.GetChild(index);
            LayoutElement le = targetSlot.GetComponent<LayoutElement>();
            Image img = targetSlot.GetChild(0).GetComponent<Image>();

            // 1. 이미지 사라지기 (Fade Out)
            Tween.Alpha(img, 0, slideDuration);

            // 2. 공간 닫기 & 종료 후 삭제
            Tween.Custom(le, le.preferredWidth, 0, slideDuration,
                        (target, x) => target.preferredWidth = x,
                        ease: Ease.OutQuart)
                        .OnComplete(targetSlot.gameObject, go => Object.Destroy(go));
        }
    }
}