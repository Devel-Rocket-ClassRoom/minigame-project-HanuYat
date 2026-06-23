using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 리더보드 패널. 열릴 때(OnEnable) 전체 랭킹을 로드해 스크롤뷰에 표시.
// 내 기록은 리스트 내에서 하이라이트되고, 패널 하단에 내 등수/기록을 고정 표기.
public class LeaderboardUI : MonoBehaviour
{
    [Header("리스트 (ScrollView Content)")]
    [SerializeField]
    private Transform listParent;

    // 엔트리 프리팹: 루트에 Image(배경, 하이라이트용) + 자식 TMP 3개(순위/닉/시간) 순서.
    [SerializeField]
    private GameObject entryPrefab;

    [SerializeField]
    private TextMeshProUGUI statusText;

    [Header("내 기록 (하단 고정)")]
    [SerializeField]
    private TextMeshProUGUI myRankText;

    [SerializeField]
    private TextMeshProUGUI myNicknameText;

    [SerializeField]
    private TextMeshProUGUI myTimeText;

    [Header("하이라이트")]
    [SerializeField]
    private Color highlightColor = Color.lightGoldenRod;

    [SerializeField]
    private Color normalColor = Color.lightCyan;

    [SerializeField]
    private Button closeButton;

    // 닫을 때 비활성화할 루트(캔버스). 비우면 이 GameObject.
    [SerializeField]
    private GameObject panelRoot;

    private bool isRefreshing;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);
    }

    private void OnEnable()
    {
        RefreshAsync().Forget();
    }

    public void ClosePanel()
    {
        (panelRoot != null ? panelRoot : gameObject).SetActive(false);
    }

    private async UniTaskVoid RefreshAsync()
    {
        // 재진입 가드 — 여닫기 연타로 동시 로드되어 리스트가 중복 채워지는 것 방지.
        if (isRefreshing)
            return;
        isRefreshing = true;

        try
        {
            if (statusText != null)
                statusText.text = "불러오는 중...\nLoading...";

            if (LeaderboardManager.Instance == null)
            {
                if (statusText != null)
                    statusText.text = "리더보드 연결 안 됨\nLeaderboard unavailable";
                return;
            }

            List<LeaderboardEntry> all = await LeaderboardManager.Instance.LoadLeaderboardAsync();

            // 로드 도중 패널이 닫혔으면 갱신 중단.
            if (!isActiveAndEnabled)
                return;

            string myUid =
                AuthManager.Instance != null ? AuthManager.Instance.UserId : string.Empty;

            DisplayList(all, myUid);
            UpdateMyRecord(all, myUid);

            if (statusText != null)
                statusText.text =
                    all.Count > 0
                        ? $"총 {all.Count}명\n{all.Count} players"
                        : "아직 기록이 없습니다\nNo records yet";
        }
        finally
        {
            isRefreshing = false;
        }
    }

    private void DisplayList(List<LeaderboardEntry> entries, string myUid)
    {
        if (listParent == null || entryPrefab == null)
            return;

        foreach (Transform child in listParent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < entries.Count; i++)
        {
            LeaderboardEntry entry = entries[i];
            GameObject item = Instantiate(entryPrefab, listParent);

            LeaderboardEntryItem row = item.GetComponent<LeaderboardEntryItem>();
            if (row == null)
                continue;

            bool isMe = !string.IsNullOrEmpty(myUid) && entry.userId == myUid;
            row.Setup(i + 1, entry, isMe, highlightColor, normalColor);
        }
    }

    private void UpdateMyRecord(List<LeaderboardEntry> entries, string myUid)
    {
        int myRank = -1;
        LeaderboardEntry mine = null;

        if (!string.IsNullOrEmpty(myUid))
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].userId == myUid)
                {
                    myRank = i + 1;
                    mine = entries[i];
                    break;
                }
            }
        }

        if (mine != null)
        {
            if (myRankText != null)
                myRankText.text = $"{myRank}";
            if (myNicknameText != null)
                myNicknameText.text = mine.nickname;
            if (myTimeText != null)
                myTimeText.text = TimeUtil.ToClearTimeString(mine.timeMs);
        }
        else
        {
            if (myRankText != null)
                myRankText.text = "-";
            if (myNicknameText != null)
                myNicknameText.text = NicknameStore.Get();
            if (myTimeText != null)
                myTimeText.text = "기록 없음\nNo record";
        }
    }
}
