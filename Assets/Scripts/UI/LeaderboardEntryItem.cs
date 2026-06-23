using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 리더보드 한 행. 프리팹 루트에 부착하고 필드를 직접 연결 — 자식 순서 의존 제거.
public class LeaderboardEntryItem : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI rankText;

    [SerializeField]
    private TextMeshProUGUI nicknameText;

    [SerializeField]
    private TextMeshProUGUI timeText;

    // 하이라이트용 행 배경.
    [SerializeField]
    private Image background;

    public void Setup(int rank, LeaderboardEntry entry, bool isMe, Color highlight, Color normal)
    {
        if (rankText != null)
            rankText.text = $"{rank}";
        if (nicknameText != null)
            nicknameText.text = entry.nickname;
        if (timeText != null)
            timeText.text = TimeUtil.ToClearTimeString(entry.timeMs);
        if (background != null)
            background.color = isMe ? highlight : normal;
    }
}
