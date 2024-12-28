using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using TTSDK.UNBridgeLib.LitJson;
using TTSDK;
using StarkSDKSpace;
using UnityEngine.Analytics;

public class GameUI : MonoBehaviour
{
    public string clickid;
    private StarkAdManager starkAdManager;

    public Image fade;

    [Header("Game Over UI")]
    public GameObject gameOver;
    public Text gameOverScore;

    [Header("New Wave UI")]
    public RectTransform newWaveBanner;
    public Text newWaveTitle;
    public Text newWaveEnemiesCount;

    [Header("Health Bar & Score")]
    public Text score;
    public RectTransform healthBar;

    Player player;

    //to access each new wave
    Spawner spawner;

    public TextMeshProUGUI HighScore;

    private void Awake()
    {
        spawner = FindObjectOfType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }


    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<Player>();
        player.OnDeath += OnGameOver;
        if(Spawner.isContinue==true)
        {
            ScoreKeeper.score = PlayerPrefs.GetInt("Score");
        }
    }


    private void Update()
    {
        score.text = ScoreKeeper.score.ToString("D6");
        HighScore.text = PlayerPrefs.GetInt("HighScore").ToString();
        float healthPercent = 0;
        if (player != null)
        {
            healthPercent = player.health / player.startingHealth;
        }
        healthBar.localScale = new Vector3(healthPercent, 1, 1);
    }


    public void OnNewWave(int waveNumber) {
        string[] numbers = { "一", "二", "三", "四", "五" };
        newWaveTitle.text = "- 当前波数 " + numbers[waveNumber - 1] + " -";

        string enemyCount = ((spawner.waves[waveNumber - 1].infinite) ? "无限" : spawner.waves[waveNumber - 1].enemyCount + "");
        /*
        if (spawner.waves[waveNumber - 1].infinite)
        {
            enemyCount = "Infinite";
        } else
        {
            spawner.waves[waveNumber - 1].enemyCount;
        }
        */
        newWaveEnemiesCount.text = "敌人: " + enemyCount;

        StopCoroutine("AnimateBanner");
        StartCoroutine("AnimateBanner");
    }


    IEnumerator AnimateBanner() {
        float percent = 0;
        float speed = 2.5f;
        int dir = 1;
        float delayTime = 1f;
        

        while (percent >= 0) {
            percent += Time.deltaTime * (1 / speed) * dir;
            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-200, 40, percent);

            if (percent > 1) {
                dir = -1;
                yield return new WaitForSeconds(delayTime);
            }

            yield return null;
        }
    }


    void OnGameOver() {
        

        StartCoroutine(Fade(Color.clear, new Color(0, 0, 0, 0.95f), 1));
        score.gameObject.SetActive(false);
        healthBar.gameObject.SetActive(false);
        gameOverScore.text = score.text;
        gameOver.SetActive(true);
        ShowInterstitialAd("1lcaf5895d5l1293dc",
            () => {
                Debug.LogError("--插屏广告完成--");

            },
            (it, str) => {
                Debug.LogError("Error->" + str);
            });
    }

    IEnumerator Fade(Color from, Color to, float second) {
        float speed = 1 / second;
        float percent = 0;

        while (percent < 1) {
            percent += Time.deltaTime * speed;
            fade.color = Color.Lerp(from, to, percent);
            yield return null;
        }
    }


    public void PlayAgain() {

        SceneManager.LoadScene("Game");
    }

    public void Menu()
    {
        SceneManager.LoadScene("Menu");
    }
    public void Continue()
    {
        ShowVideoAd("192if3b93qo6991ed0",
            (bol) => {
                if (bol)
                {
                    Spawner.isContinue = true;
                    SceneManager.LoadScene("Game");
                    clickid = "";
                    getClickid();
                    apiSend("game_addiction", clickid);
                    apiSend("lt_roi", clickid);


                }
                else
                {
                    StarkSDKSpace.AndroidUIManager.ShowToast("观看完整视频才能获取奖励哦！");
                }
            },
            (it, str) => {
                Debug.LogError("Error->" + str);
                //AndroidUIManager.ShowToast("广告加载异常，请重新看广告！");
            });
        
    }


    public void getClickid()
    {
        var launchOpt = StarkSDK.API.GetLaunchOptionsSync();
        if (launchOpt.Query != null)
        {
            foreach (KeyValuePair<string, string> kv in launchOpt.Query)
                if (kv.Value != null)
                {
                    Debug.Log(kv.Key + "<-参数-> " + kv.Value);
                    if (kv.Key.ToString() == "clickid")
                    {
                        clickid = kv.Value.ToString();
                    }
                }
                else
                {
                    Debug.Log(kv.Key + "<-参数-> " + "null ");
                }
        }
    }

    public void apiSend(string eventname, string clickid)
    {
        TTRequest.InnerOptions options = new TTRequest.InnerOptions();
        options.Header["content-type"] = "application/json";
        options.Method = "POST";

        JsonData data1 = new JsonData();

        data1["event_type"] = eventname;
        data1["context"] = new JsonData();
        data1["context"]["ad"] = new JsonData();
        data1["context"]["ad"]["callback"] = clickid;

        Debug.Log("<-data1-> " + data1.ToJson());

        options.Data = data1.ToJson();

        TT.Request("https://analytics.oceanengine.com/api/v2/conversion", options,
           response => { Debug.Log(response); },
           response => { Debug.Log(response); });
    }


    /// <summary>
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="closeCallBack"></param>
    /// <param name="errorCallBack"></param>
    public void ShowVideoAd(string adId, System.Action<bool> closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            starkAdManager.ShowVideoAdWithId(adId, closeCallBack, errorCallBack);
        }
    }
    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="adId"></param>
    /// <param name="errorCallBack"></param>
    /// <param name="closeCallBack"></param>
    public void ShowInterstitialAd(string adId, System.Action closeCallBack, System.Action<int, string> errorCallBack)
    {
        starkAdManager = StarkSDK.API.GetStarkAdManager();
        if (starkAdManager != null)
        {
            var mInterstitialAd = starkAdManager.CreateInterstitialAd(adId, errorCallBack, closeCallBack);
            mInterstitialAd.Load();
            mInterstitialAd.Show();
        }
    }
}
