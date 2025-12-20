#if TEST_Manager
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct SliderConfig
{
    public TextMeshProUGUI TMP;
    public Slider slider;
    public int value;
    public int minValue;
    public int maxValue;
    public string format;

    public SliderConfig(int _value, int _min, int _max, string _format)
    {
        TMP = null;
        slider = null;
        value = _value;
        minValue = _min;
        maxValue = _max;
        format = _format;
    }
}

public class TestManager : MonoBehaviour
{
    public static TestManager Instance { private set; get; }

    [Header("Game Test")]
    [SerializeField][Min(0)] private int testCount = 0;
    [SerializeField][Min(0)] private int maxScore = 0;
    private int totalScore = 0;
    [SerializeField][Min(0)] private int averageScore = 0;

    private float totalPlay = 0f;
    [SerializeField][Min(0f)] private float averagePlay = 0f;

    public bool IsAuto { private set; get; } = false;
    [SerializeField][Min(0f)] private float autoReplay = 0f;
    private Coroutine autoRoutine;

    [Header("Sound Test")]
    [SerializeField] private bool bgmPause = false;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new SliderConfig(1, 1, 10, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI maxScoreNum;
    [SerializeField] private TextMeshProUGUI averageScoreNum;
    [SerializeField] private TextMeshProUGUI averagePlayNum;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (testUI == null)
            testUI = GameObject.Find("TestUI");

        if (gameSpeed.TMP == null)
            gameSpeed.TMP = GameObject.Find("TestUI/GameSpeed/TestText")?.GetComponent<TextMeshProUGUI>();
        if (gameSpeed.slider == null)
            gameSpeed.slider = GameObject.Find("TestUI/GameSpeed/TestSlider")?.GetComponent<Slider>();

        if (testCountNum == null)
            testCountNum = GameObject.Find("TestUI/TestCount/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (maxScoreNum == null)
            maxScoreNum = GameObject.Find("TestUI/MaxScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageScoreNum == null)
            averageScoreNum = GameObject.Find("TestUI/AverageScore/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averagePlayNum == null)
            averagePlayNum = GameObject.Find("TestUI/AveragePlay/TestNum")?.GetComponent<TextMeshProUGUI>();
    }
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        testUI.SetActive(false);
    }

    private void Start()
    {
        SoundManager.Instance?.ToggleBGM();

        AutoPlay();
        UpdateTestUI();
    }

    private void Update()
    {
        #region 게임 매니저
        if (Input.GetKeyDown(KeyCode.P)) GameManager.Instance?.Pause(!GameManager.Instance.IsPaused);
        if (Input.GetKeyDown(KeyCode.G)) GameManager.Instance?.GameOver();
        if (Input.GetKeyDown(KeyCode.R)) GameManager.Instance?.Replay();
        if (Input.GetKeyDown(KeyCode.Q)) GameManager.Instance?.Quit();
        #endregion

        #region 사운드 매니저
        if (Input.GetKeyDown(KeyCode.B))
        {
            bgmPause = !bgmPause;
            SoundManager.Instance?.PauseSound(bgmPause);
        }
        if (Input.GetKeyDown(KeyCode.M)) SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N)) SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 매니저
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = (i == 10) ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            if (Input.GetKeyDown(key))
            {
                break;
            }
        }
        #endregion

        #region UI 매니저
        if (Input.GetKeyDown(KeyCode.Z)) UIManager.Instance?.OpenSetting(!UIManager.Instance.GetOnSetting());
        if (Input.GetKeyDown(KeyCode.X)) UIManager.Instance?.OpenConfirm(!UIManager.Instance.GetOnConfirm());
        if (Input.GetKeyDown(KeyCode.C)) UIManager.Instance?.OpenResult(!UIManager.Instance.GetOnResult());
        #endregion

        #region 테스트 매니저
        if (Input.GetKeyDown(KeyCode.BackQuote)) OnClickTest();
        if (Input.GetKeyDown(KeyCode.O)) AutoPlay(!IsAuto);
        if (IsAuto)
        {
            if (GameManager.Instance.IsGameOver)
            {
                if (autoRoutine == null)
                    autoRoutine = StartCoroutine(AutoReplay());
            }
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
            ChangeGameSpeed(gameSpeed.value == gameSpeed.maxValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.maxValue);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            ChangeGameSpeed(gameSpeed.value == gameSpeed.minValue ? GameManager.Instance.GetMaxSpeed() : gameSpeed.minValue);
        #endregion
    }

    #region 테스트
    public void AutoPlay(bool _on = true)
    {
        IsAuto = _on;

        GameManager.Instance?.SetSpeed(_on ? GameManager.Instance.GetMaxSpeed() : 1f);
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);
        if (GameManager.Instance.IsGameOver)
        {
            int score = GameManager.Instance.GetScore();
            totalScore += score;
            maxScore = Mathf.Max(score, maxScore);
            averageScore = totalScore / ++testCount;

            float playTime = UIManager.Instance.GetPlayTime();
            totalPlay += playTime;
            averagePlay = totalPlay / testCount;

            GameManager.Instance?.Replay();

            UpdateTestUI();
        }
        autoRoutine = null;
    }
    #endregion

    #region 테스트 UI
    private void OnEnable()
    {
        InitSlider(gameSpeed, ChangeGameSpeed);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);
    }

    private void InitSlider(SliderConfig _config, UnityEngine.Events.UnityAction<float> _action)
    {
        if (_config.slider == null) return;

        _config.slider.minValue = _config.minValue;
        _config.slider.maxValue = _config.maxValue;
        _config.slider.wholeNumbers = true;

        float v = _config.value;
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        _config.slider.value = v;

        _action.Invoke(_config.slider.value);
        _config.slider.onValueChanged.AddListener(_action);
    }

    private int ChangeSlider(float _value, SliderConfig _config)
    {
        int v = Mathf.RoundToInt(_value);
        if (v < _config.minValue) v = _config.minValue;
        else if (v > _config.maxValue) v = _config.maxValue;
        return v;
    }

    private void ApplySlider(ref SliderConfig _config, float _value, System.Action<int> _afterAction = null)
    {
        _config.value = ChangeSlider(_value, _config);
        UpdateSliderUI(_config);
        _afterAction?.Invoke(_config.value);
    }

    private void UpdateSliderUI(SliderConfig _config)
    {
        if (string.IsNullOrEmpty(_config.format))
            _config.TMP.text = _config.value.ToString();
        else
            _config.TMP.text = string.Format(_config.format, _config.value);

        _config.slider.value = _config.value;
    }
    private void ChangeGameSpeed(float _value) => ApplySlider(ref gameSpeed, _value, _v => GameManager.Instance?.SetSpeed(_v, true));

    private void UpdateTestUI()
    {
        testCountNum.text = testCount.ToString();
        maxScoreNum.text = maxScore.ToString();
        averageScoreNum.text = averageScore.ToString();
        averagePlayNum.text = (averagePlay / 60).ToString("00") + ":" + (averagePlay % 60).ToString("00");

        UpdateSliderUI(gameSpeed);
    }

    public void OnClickTest()
    {
        testUI.SetActive(!testUI.activeSelf);
        UpdateTestUI();
    }
    public void OnClickReset()
    {
        testCount = 0;
        maxScore = 0;
        totalScore = 0;
        averageScore = 0;
        totalPlay = 0f;
        averagePlay = 0f;

        UpdateTestUI();
    }
    public void OnClickReplay() => GameManager.Instance?.Replay();
    #endregion
}
#endif
