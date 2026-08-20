#if TEST_Manager
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct TestResult
{
    [Min(0)] public long value;
    [Min(0f)] public float playTime;

    public TestResult(long _value, float _playTime)
    {
        value = _value;
        playTime = _playTime;
    }
}

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
    [SerializeField] private List<TestResult> testResults = new();
    [SerializeField][Min(0f)] private float playTime = 0f;
    [Space]
    [SerializeField][Min(0f)] private float autoReplay = 0f;
    private Coroutine autoRoutine;

    public bool IsAuto { private set; get; } = false;

    [Header("Sound Test")]
    [SerializeField] private bool onPauseBgm = false;

    [Header("Test UI")]
    [SerializeField] private GameObject testUI;
    [Space]
    [SerializeField] private SliderConfig gameSpeed = new(1, 1, 20, "배속 × {0}");
    [Space]
    [SerializeField] private TextMeshProUGUI testCountNum;
    [SerializeField] private TextMeshProUGUI averagePlayNum;
    [SerializeField] private TextMeshProUGUI averageValueName;
    [SerializeField] private TextMeshProUGUI averageValueNum;
    [SerializeField] private TextMeshProUGUI value10Num;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (testUI == null)
            testUI = GameObject.Find("TestUI");

        if (gameSpeed.TMP == null)
            gameSpeed.TMP = GameObject.Find("TestUI/GameSpeed/TestName")?.GetComponent<TextMeshProUGUI>();
        if (gameSpeed.slider == null)
            gameSpeed.slider = GameObject.Find("TestUI/GameSpeed/TestSlider")?.GetComponent<Slider>();

        if (testCountNum == null)
            testCountNum = GameObject.Find("TestUI/TestCount/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averagePlayNum == null)
            averagePlayNum = GameObject.Find("TestUI/AveragePlay/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (averageValueName == null)
            averageValueName = GameObject.Find("TestUI/AverageValue/TestName")?.GetComponent<TextMeshProUGUI>();
        if (averageValueNum == null)
            averageValueNum = GameObject.Find("TestUI/AverageValue/TestNum")?.GetComponent<TextMeshProUGUI>();
        if (value10Num == null)
            value10Num = GameObject.Find("TestUI/Value10/TestNum")?.GetComponent<TextMeshProUGUI>();
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
    }

    private void Start()
    {
        SoundManager.Instance?.ToggleBGM();
        SoundManager.Instance?.ToggleSFX();

        SetAuto();
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
            onPauseBgm = !onPauseBgm;
            SoundManager.Instance?.PauseSound(onPauseBgm);
        }
        if (Input.GetKeyDown(KeyCode.M)) SoundManager.Instance?.ToggleBGM();
        if (Input.GetKeyDown(KeyCode.N)) SoundManager.Instance?.ToggleSFX();
        #endregion

        #region 엔티티 매니저
        for (int i = 1; i <= 10; i++)
        {
            KeyCode key = i == 10 ? KeyCode.Alpha0 : (KeyCode)((int)KeyCode.Alpha0 + i);
            int digit = i == 10 ? 0 : i;

            if (Input.GetKeyDown(key))
            {
                break;
            }
        }
        #endregion

        #region UI 매니저
        if (Input.GetKeyDown(KeyCode.Z)) UIManager.Instance?.OpenSetting(!UIManager.Instance.OnSetting);
        if (Input.GetKeyDown(KeyCode.X)) UIManager.Instance?.OpenConfirm(!UIManager.Instance.OnConfirm);
        if (Input.GetKeyDown(KeyCode.C)) UIManager.Instance?.OpenResult(!UIManager.Instance.OnResult);
        #endregion

        #region 테스트 매니저
        if (Input.GetKeyDown(KeyCode.BackQuote)) OnClickTest();
        if (Input.GetKeyDown(KeyCode.O)) SetAuto(!IsAuto);
        if (IsAuto)
        {
            if (GameManager.Instance.IsGameOver)
            {
                if (autoRoutine == null)
                    autoRoutine = StartCoroutine(AutoReplay());
            }
            else AutoPlay();
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
            ChangeGameSpeed(Mathf.Approximately(GameManager.Instance.Speed, gameSpeed.maxValue)
                ? GameManager.Instance.MaxSpeed
                : gameSpeed.maxValue);
        if (Input.GetKeyDown(KeyCode.DownArrow))
            ChangeGameSpeed(Mathf.Approximately(GameManager.Instance.Speed, gameSpeed.minValue)
                ? GameManager.Instance.MaxSpeed
                : gameSpeed.minValue);
        #endregion
    }

    #region 자동 플레이 + 테스트 모드
    public void SetAuto(bool _on = true)
    {
        IsAuto = _on;
    }

    private IEnumerator AutoReplay()
    {
        yield return new WaitForSecondsRealtime(autoReplay);

        if (GameManager.Instance.IsGameOver)
        {
            long value = GameManager.Instance.Score;

            testResults.Add(new TestResult(value, playTime));

            playTime = 0f;

            GameManager.Instance?.Replay();
            UpdateTestUI();
        }
        autoRoutine = null;
    }

    private void AutoPlay()
    {
        playTime += Time.deltaTime;
    }
    #endregion

    private void OnEnable()
    {
        gameSpeed.value = (int)GameManager.Instance?.Speed;
        InitSlider(gameSpeed, ChangeGameSpeed);
    }

    private void OnDisable()
    {
        gameSpeed.slider.onValueChanged.RemoveListener(ChangeGameSpeed);
    }

    #region 테스트 UI_기본
    private void InitSlider(SliderConfig _config, UnityEngine.Events.UnityAction<float> _action)
    {
        if (_config.slider == null) return;

        _config.slider.minValue = _config.minValue;
        _config.slider.maxValue = _config.maxValue;
        _config.slider.wholeNumbers = true;
        _config.slider.value = Mathf.Clamp(_config.value, _config.minValue, _config.maxValue);

        _action.Invoke(_config.slider.value);
        _config.slider.onValueChanged.AddListener(_action);
    }

    private void ApplySlider(ref SliderConfig _config, float _value, System.Action<int> _afterAction = null)
    {
        int value = ChangeSlider(_value, _config);
        if (_config.value == value)
        {
            UpdateSliderUI(_config);
            return;
        }

        _config.value = value;
        UpdateSliderUI(_config);
        _afterAction?.Invoke(_config.value);
    }

    private int ChangeSlider(float _value, SliderConfig _config)
        => Mathf.Clamp(Mathf.RoundToInt(_value), _config.minValue, _config.maxValue);

    private void UpdateSliderUI(SliderConfig _config)
    {
        _config.TMP.text = string.IsNullOrEmpty(_config.format)
            ? _config.value.ToString()
            : string.Format(_config.format, _config.value);
        _config.slider.SetValueWithoutNotify(_config.value);
    }

    private void UpdateTestUI()
    {
        int count = testResults.Count;

        List<long> values = new(count);
        long totalValue = 0;
        float totalPlay = 0f;
        double valueSqSum = 0d;

        for (int i = 0; i < count; i++)
        {
            TestResult r = testResults[i];

            values.Add(r.value);
            totalValue += r.value;
            totalPlay += r.playTime;
            valueSqSum += (double)r.value * r.value;
        }

        long topAvg = 0;
        long bottomAvg = 0;
        if (count > 0)
        {
            values.Sort();
            int group = Mathf.Max(Mathf.CeilToInt(count * 0.1f), 1);

            long sumBottom = 0;
            for (int i = 0; i < group; i++) sumBottom += values[i];

            long sumTop = 0;
            for (int i = count - group; i < count; i++) sumTop += values[i];

            bottomAvg = (long)System.Math.Round((double)sumBottom / group);
            topAvg = (long)System.Math.Round((double)sumTop / group);
        }

        long averageValue = count > 0 ? (long)System.Math.Round((double)totalValue / count) : 0;
        float averagePlay = count > 0 ? totalPlay / count : 0f;

        double cvValue = 0d;
        if (count > 1)
        {
            double meanValue = (double)totalValue / count;
            double varValue = valueSqSum / count - meanValue * meanValue;
            if (varValue < 0d) varValue = 0d;
            double stdValue = System.Math.Sqrt(varValue);
            cvValue = meanValue != 0d ? (stdValue / meanValue) * 100d : 0d;
        }

        int totalSeconds = Mathf.RoundToInt(averagePlay);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        testCountNum.text = count.ToString();
        averagePlayNum.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        averageValueName.text = "평균점수";
        averageValueNum.text = $"{averageValue:#,0} ({cvValue:0.#}%)";
        value10Num.text = $"{topAvg:#,0} / {bottomAvg:#,0}";

        UpdateSliderUI(gameSpeed);
    }
    #endregion

    #region 테스트 UI_추가
    private void ChangeGameSpeed(float _value)
        => ApplySlider(ref gameSpeed, _value, _v => GameManager.Instance?.SetSpeed(_v, true));
    #endregion

    #region 테스트 UI_클릭
    public void OnClickTest()
    {
        testUI.SetActive(!testUI.activeSelf);
        UpdateTestUI();
    }
    public void OnClickReset()
    {
        testResults.Clear();
        playTime = 0f;

        if (autoRoutine != null)
        {
            StopCoroutine(autoRoutine);
            autoRoutine = null;
        }

        UpdateTestUI();
    }
    public void OnClickReplay()
    {
        testUI.SetActive(false);
        OnClickReset();
        ChangeGameSpeed(gameSpeed.maxValue);
        GameManager.Instance?.Replay();
    }
    #endregion

    #region 프로퍼티
    #endregion
}
#endif
