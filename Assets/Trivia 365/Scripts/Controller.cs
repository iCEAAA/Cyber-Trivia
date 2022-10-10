#if (UNITY_ANDROID || UNITY_IOS)
#define SUPPORTED_PLATFORM
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using QuizApp.Attributes;

#if USE_DOTWEEN
using DG.Tweening;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Controller : MonoBehaviour
{
    internal static Controller instance;

    public Text playerName;

    [QAHeader("Canvases")]

    public GameObject HomeCanvas;
    public GameObject CategoriesCanvas;
    public GameObject GameCanvas;
    public GameObject PauseCanvas;
    public GameObject GameOverCanvas;

    [QAHeader("Home Canvas Objects")]

    public Text TopbarTitle;
    public GameObject HighscorePage;
    public GameObject SettingsPage;
    public GameObject BackButton;
    public GameObject ProfileButton;
    public GameObject SettingsButton;

    [QAHeader("Settings Page Objects")]

    public Button SoundButton;
    public Button VibrationButton;
    public Button CacheButton;
    public Text CacheText;

    public GameObject AdExperienceButton;

    [QAHeader("Category Canvas Objects")]

    public GameObject CategoryGroup;
    public GameObject ErrorPanel;
    public GameObject LoadingAnimation;

    [HideInInspector]
    public Animation CategoryLoading;
    [HideInInspector]
    public CategoriesParent CategoriesParentScript;

    [QAHeader("Game Canvas Objects")]

    public Text CategoryName;
    public Text TimerText;
    public Text ScoreText;
    public Text LivesText;
    public Text MainQuestionDisplay;
    public Text AltQuestionDisplay;
    public RawImage ImageDisplay;
    public GameObject AnswerBarsParent;
    public GameObject ShortAnswerBarsParent;
    public GameObject ToFAnswersParent;
    public Text ToFAnswersHeading;
    public GameObject PostQuestionButtons;
    public GameObject ExplanationButton;
    public GameObject ExplanationPanel;
    public GameObject ExtraLifeButton;
    public Text ExplanationDisplay;
    public Animation ImageDownloadAnimation;
    public GameObject QuitGamePanel;

    [HideInInspector]
    public AspectRatioFitter ImageAspectRatioFitter;
    [HideInInspector]
    public CanvasGroup AnswerBarsCanvasGroup;
    [HideInInspector]
    public CanvasGroup ShortAnswerBarsCanvasGroup;
    [HideInInspector]
    public GameObject[] ToFAnswerHolder;
    [HideInInspector]
    public CanvasGroup ToFAnswerHoldersCanvasGroup;
    [HideInInspector]
    public Text[] AnswerDisplay;
    [HideInInspector]
    public Image[] AnswerParent;
    [HideInInspector]
    public Text[] ShortAnswerDisplay;
    [HideInInspector]
    public Image[] ShortAnswerParent;
    [HideInInspector]
    public Animation LivesAnimation;
    [HideInInspector]
    public Animation ScoreAnimation;

    [QAHeader("GameOver Canvas Objects")]

    public Image CategoryImage;
    public Text CategoryNameText;
    public Text GameScoreText;
    public Text HighscoreText;

    [QAHeader("Game Variables")]

    [Space(5)]

    public bool SkipCategoriesDisplay = false;

    [QASeparator]

    public bool EnablePausing = true;

    [QASeparator]

    [Range(5, 60)]
    public int DownloadTimeout = 15;

    [QASeparator]

    public bool EnableTimer = true;
    [Range(5, 100)]
    public int UniversalTimerAmount = 15;
    [Range(1, 100)]
    public int PointsPerSecond = 10;

    [Space(5)]

    [Range(1, 1000)]
    [Help("Points Per Question is used to calculate the score ONLY if you disable the timer. Otherwise, Points Per Second is used.")]
    public int PointsPerQuestion = 100;

    [QASeparator]

    public bool EnableLives = true;
    [Range(1, 100)]
    public int UniversalLivesAmount = 3;

    [QASeparator]

    public bool ShowPostQuestionButtons;
    [Range(1, 30)]
    public int PostQuestionDelay = 3;

    [QASeparator("Colors")]

    public Color DefaultColor = Color.white;
    public Color CorrectColor = Color.green;
    public Color WrongColor = Color.red;

    [QASeparator("Sounds")]

    public AudioClip CorrectSound;
    public AudioClip WrongSound;
    public AudioClip TickingSound;

    [HideInInspector]
    public List<QuestionFormat> QuestionList = new List<QuestionFormat>();
    [HideInInspector]
    public List<AnswerFormat> AnswersList = new List<AnswerFormat>();

    [HideInInspector]
    public AudioSource AudioPlayer;

    private const string SoundPref = "SoundState";
#if (SUPPORTED_PLATFORM || UNITY_EDITOR)
    private int VibrationState;
    private bool VibrationEnabled;
    private static string VibrationPref = "VibrationState";
#endif

    private int SoundState, CurrentQuestion, QuestionsLimit, TimeLeft, LivesLeft, CurrentScore;
    private bool SoundEnabled, isMainMenu, isSettings, isProfile, isCategories, ResetAnswerColors, isHybrid, Answered, isGameOver, isInGame, Paused, Ticking, ToFQuestion, WaitingForFile, WaitingForImage;

    private CategoryFormat CurrentCategory;

    private List<string> ImageLinks = new List<string>();
    private List<string> FaultyLinks = new List<string>();

    private AspectRatios AspectRatio;

#if UNITY_EDITOR
    private bool DebugAutoAnswer = false;
#endif

#if USE_ADMOB
    private bool RewardedShown;
    private int TotalGameOvers;
#endif

#if UNITY_EDITOR
    [ContextMenu("Fetch Constants")]
    public void FetchConstants()
    {
        LoadingAnimation.SetActive(false);
        CategoryLoading = LoadingAnimation.GetComponent<Animation>();

        AnswerBarsCanvasGroup = AnswerBarsParent.GetComponent<CanvasGroup>();
        ShortAnswerBarsCanvasGroup = ShortAnswerBarsParent.GetComponent<CanvasGroup>();
        ToFAnswerHoldersCanvasGroup = ToFAnswersParent.GetComponent<CanvasGroup>();

        ToFAnswerHolder = new GameObject[2];
        ToFAnswerHolder[0] = ToFAnswersParent.transform.GetChild(1).gameObject;
        ToFAnswerHolder[1] = ToFAnswersParent.transform.GetChild(2).gameObject;

        int childCount = AnswerBarsParent.transform.childCount;

        AnswerDisplay = new Text[childCount];
        AnswerParent = new Image[childCount];

        for (int x = 0; x < childCount; x++)
        {
            AnswerParent[x] = AnswerBarsParent.transform.GetChild(x).gameObject.GetComponent<Image>();
            AnswerDisplay[x] = AnswerParent[x].transform.GetChild(0).gameObject.GetComponent<Text>();
        }

        childCount = ShortAnswerBarsParent.transform.childCount;

        ShortAnswerDisplay = new Text[childCount];
        ShortAnswerParent = new Image[childCount];

        for (int x = 0; x < childCount; x++)
        {
            ShortAnswerParent[x] = ShortAnswerBarsParent.transform.GetChild(x).gameObject.GetComponent<Image>();
            ShortAnswerDisplay[x] = ShortAnswerParent[x].transform.GetChild(0).gameObject.GetComponent<Text>();
        }

        AudioPlayer = GetComponent<AudioSource>();

        CategoriesParentScript = CategoryGroup.GetComponentInChildren<CategoriesParent>(true);

        LivesAnimation = LivesText.GetComponent<Animation>();
        ScoreAnimation = ScoreText.GetComponent<Animation>();

        ImageAspectRatioFitter = ImageDisplay.GetComponent<AspectRatioFitter>();
    }

    [ContextMenu("Open Cache Location", false, 9999999)]
    public void OpenCacheLocation()
    {
        Application.OpenURL(Application.temporaryCachePath);
    }

    [ContextMenu("Generate Debug Data", false, 9999999)]
    public void GenerateDebugData()
    {
        string path = EditorUtility.SaveFilePanel("Save Location", "", string.Concat("Controller-Debug ", System.DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss tt")), "json");

        if (string.IsNullOrEmpty(path))
            return;

        File.WriteAllText(path, JsonUtility.ToJson(this, true));
    }
#endif

    // Use this for initialization
    private void Awake()
    {
        if (instance == null)
            instance = this;

        ToggleSound(true);
        ToggleVibration(true);

        GetAspectRatio();
    }

    private void GetAspectRatio()
    {
        float aspect = Camera.main.aspect;

        if (aspect >= 0.749)
            AspectRatio = AspectRatios._3x4;
        else if (aspect >= 0.666)
            AspectRatio = AspectRatios._2x3;
        else if (aspect >= 0.624)
            AspectRatio = AspectRatios._10x16;
        else
            AspectRatio = AspectRatios.Default;
    }

    private void Start()
    {
#if USE_ADMOB
        AdmobManager.instance.CheckConsent();
        TotalGameOvers = 0;
#endif
        ShowHome();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isMainMenu)
            {
#if UNITY_EDITOR
                EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
            else if (isSettings || isCategories || isProfile)
                ShowHome();
            else if (isInGame)
                PauseGame();
        }
    }

    private void PlaySound(AudioClip Clip)
    {
        if (!SoundEnabled)
            return;

        StopAudioPlayer();

        AudioPlayer.PlayOneShot(Clip);
    }

    private void StopAudioPlayer()
    {
        if (AudioPlayer.isPlaying)
            AudioPlayer.Stop();
    }

    public void ShowHome()
    {
        HomeCanvas.SetActive(true);
        CategoriesCanvas.SetActive(false);
        GameCanvas.SetActive(false);
        PauseCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);

        HighscorePage.SetActive(false);
        SettingsPage.SetActive(false);

        BackButton.SetActive(false);
        ProfileButton.SetActive(true);
        SettingsButton.SetActive(true);

        TopbarTitle.text = "Home";

        isGameOver = isMainMenu = true;
        isSettings = isProfile = isCategories = isInGame = Paused = false;
    }

    public void ShowSettings()
    {
        SettingsPage.SetActive(true);

        BackButton.SetActive(true);
        ProfileButton.SetActive(false);
        SettingsButton.SetActive(false);

#if USE_ADMOB
        AdExperienceButton.SetActive(true);
#else
        AdExperienceButton.SetActive(false);
#endif

        TopbarTitle.text = "Settings";

        isSettings = true;
        isMainMenu = false;
    }

    public IEnumerator DownloadRankJson()
    {
        string url = "http://1.117.161.63/info2.php";

        using (UnityWebRequest uwr = UnityWebRequest.Get(url))
        {
            uwr.timeout = 15;
            yield return uwr.SendWebRequest();

            if (uwr.isHttpError || uwr.isNetworkError)
            {
                Debug.Log("Http Error or Network Error");
            }
            else
            {
                Debug.Log("GET Success");
                GameObject.Find("ScoreBoard").GetComponent<ScoreBoard>().SendMessage("getData", uwr.downloadHandler.text);
            }
        }
    }

    public void ShowHighscores()
    {
        // obtain score json
        StartCoroutine(DownloadRankJson());

        HighscorePage.SetActive(true);

        BackButton.SetActive(true);
        ProfileButton.SetActive(false);
        SettingsButton.SetActive(false);

        TopbarTitle.text = "Highscores";

        isProfile = true;
        isMainMenu = false;
    }

    public void ToggleSound(bool Init = false)
    {
        SoundState = PlayerPrefs.GetInt(SoundPref, 1);

        if (Init)
        {
            if (SoundState == 0)
            {
                SoundEnabled = false;
                SoundButton.image.color = Color.red;
            }
            else if (SoundState == 1)
            {
                SoundEnabled = true;
                SoundButton.image.color = Color.green;
            }
            else
            {
                PlayerPrefs.SetInt(SoundPref, 1);
                SoundEnabled = true;
                SoundButton.image.color = Color.green;
            }
        }
        else
        {
            if (SoundState == 0)
            {
                SoundEnabled = true;
                PlayerPrefs.SetInt(SoundPref, 1);
                SoundButton.image.color = Color.green;
            }
            else if (SoundState == 1)
            {
                SoundEnabled = false;
                PlayerPrefs.SetInt(SoundPref, 0);
                SoundButton.image.color = Color.red;
            }
        }
    }

    public void ToggleVibration(bool Init = false)
    {
#if (SUPPORTED_PLATFORM || UNITY_EDITOR)
        VibrationState = PlayerPrefs.GetInt(VibrationPref, 1);

        if (Init)

        {
            if (VibrationState == 0)
            {
                VibrationEnabled = false;
                VibrationButton.image.color = Color.red;
            }
            else if (VibrationState == 1)
            {
                VibrationEnabled = true;
                VibrationButton.image.color = Color.green;
            }
            else
            {
                VibrationEnabled = true;
                PlayerPrefs.SetInt(VibrationPref, 1);
                VibrationButton.image.color = Color.green;
            }
        }
        else
        {

            if (VibrationState == 0)
            {
                VibrationEnabled = true;
                PlayerPrefs.SetInt(VibrationPref, 1);
                VibrationButton.image.color = Color.green;
            }
            else if (VibrationState == 1)
            {
                VibrationEnabled = false;
                PlayerPrefs.SetInt(VibrationPref, 0);
                VibrationButton.image.color = Color.red;
            }
        }
#else
        VibrationButton.gameObject.SetActive(false);
#endif
    }

    public void DeleteCache()
    {
        DirectoryInfo dir = new DirectoryInfo(Application.temporaryCachePath);

        foreach (FileInfo file in dir.GetFiles())
            file.Delete();

        StartCoroutine(DeletionComplete());
    }

    private IEnumerator DeletionComplete()
    {
        string OriginalText = CacheText.text;

        CacheButton.interactable = false;
        CacheText.text = "Done";

        yield return new WaitForSeconds(3f);

        CacheButton.interactable = true;
        CacheText.text = OriginalText;
    }

    public void ShowCategories()
    {
        HomeCanvas.SetActive(false);
        CategoriesCanvas.SetActive(true);

        ErrorPanel.SetActive(false);

        if (LoadingAnimation.activeInHierarchy)
        {
            CategoryLoading.Stop();
            LoadingAnimation.SetActive(false);
        }

        isCategories = true;
        isMainMenu = false;

        if (SkipCategoriesDisplay)
        {
            LoadCategory(CategoriesParentScript.GetSingleCategory().Category);
            CategoryGroup.SetActive(false);
        }
        else
            CategoryGroup.SetActive(true);
    }

    internal void LoadCategory(CategoryFormat format)
    {
        CurrentCategory = format;

        isHybrid = false;

        if (CurrentCategory.Mode == DownloadMode.Online)
            StartCoroutine(DownloadJSON());
        else if (CurrentCategory.Mode == DownloadMode.Offline)
            ParseJSON(CurrentCategory.OfflineFile.text);
        else
            StartCoroutine(DownloadJSON(true));
    }

    private void ParseJSON(string Content)
    {
        if (isMainMenu)
            return;

        QuestionList.Clear();

        QuestionsContainer container = JsonUtility.FromJson<QuestionsContainer>(Content);

        QuestionList = container.Questions;

        container = new QuestionsContainer();

        PrepareGame();
    }

    private IEnumerator DownloadJSON(bool Hybrid = false)
    {
        isHybrid = Hybrid;

        LoadingAnimation.SetActive(true);

        CategoryLoading.Play();

        CategoryGroup.SetActive(false);

        using (UnityWebRequest uwr = UnityWebRequest.Get(CurrentCategory.OnlinePath))
        {
            uwr.timeout = DownloadTimeout;

            yield return uwr.SendWebRequest();

            CategoryLoading.Stop();
            LoadingAnimation.SetActive(false);

            if (uwr.isHttpError || uwr.isNetworkError)
            {
                if (Hybrid)
                {
                    ParseJSON(CurrentCategory.OfflineFile.text);
                    CurrentCategory.Mode = DownloadMode.Offline;
                }
                else
                    ErrorPanel.SetActive(true);
            }
            else
            {
                if (Hybrid)
                    CurrentCategory.Mode = DownloadMode.Online;

                ParseJSON(uwr.downloadHandler.text);
            }

        }
    }

    private void PrepareGame()
    {
#if USE_ADMOB
        AdmobManager.instance.RequestInterstitial();

        if (EnableLives && ShowPostQuestionButtons)
        {
            AdmobManager.instance.RequestRewardBasedVideo();
            RewardedShown = false;
        }
#endif

        isCategories = false;

        CurrentQuestion = 0;

        ShuffleQuestions();

        QuestionsLimit = GetQuestionLimit();

        UpdateScore(true);

        UpdateTimer(true);

        UpdateLives(true);

        isGameOver = Paused = false;

        CategoryName.text = CurrentCategory.CategoryName;

        if (CurrentCategory.Mode == DownloadMode.Online)
        {
            ImageLinks.Clear();
            FaultyLinks.Clear();

            for (int a = 0; a < QuestionList.Count; a++)
                if (QuestionList[a].Image.Length > 1)
                    ImageLinks.Add(QuestionList[a].Image);

            StartCoroutine(CacheImages());
        }

#if USE_DOTWEEN
        ResetScale(true);
#endif

        CategoriesCanvas.SetActive(false);
        GameOverCanvas.SetActive(false);
        PauseCanvas.SetActive(false);
        GameCanvas.SetActive(true);

        QuitGamePanel.SetActive(false);
        PostQuestionButtons.SetActive(false);
        ExplanationPanel.SetActive(false);

        isInGame = true;

        StartCoroutine(LoadQuestion());
    }

#if USE_DOTWEEN
    private void ResetScale(bool All = false)
    {
        if (All)
        {
            for (int x = 0; x < ToFAnswerHolder.Length; x++)
                ToFAnswerHolder[x].transform.localScale = Vector3.zero;
            for (int x = 0; x < AnswerParent.Length; x++)
                AnswerParent[x].transform.localScale = Vector3.zero;
            for (int x = 0; x < ShortAnswerParent.Length; x++)
                ShortAnswerParent[x].transform.localScale = Vector3.zero;

            return;
        }

        if (ToFQuestion)
            for (int x = 0; x < ToFAnswerHolder.Length; x++)
                ToFAnswerHolder[x].transform.localScale = Vector3.zero;
        else
        {
            if (AspectRatio == AspectRatios.Default)
            {
                for (int x = 0; x < AnswerParent.Length; x++)
                    AnswerParent[x].transform.localScale = Vector3.zero;
            }
            else
            {
                for (int x = 0; x < ShortAnswerParent.Length; x++)
                    ShortAnswerParent[x].transform.localScale = Vector3.zero;
            }
        }
    }
#endif

    private int GetTimerAmount()
    {
        if (CurrentCategory.CustomTimerAmount)
            return CurrentCategory.TimerAmount;
        else
            return UniversalTimerAmount;
    }

    private void UpdateTimer(bool init = false)
    {
        if (EnableTimer)
        {
            if (init)
            {
                Ticking = false;
                TimerText.gameObject.SetActive(true);
                TimeLeft = GetTimerAmount();
            }
            else
                TimeLeft--;

            TimerText.text = string.Concat(TimeLeft.ToString(), "'");
        }
        else if (!EnableTimer && init)
            TimerText.gameObject.SetActive(false);
    }

    private int GetLivesAmount()
    {
        if (CurrentCategory.CustomLivesAmount)
            return CurrentCategory.LivesCount;
        else
            return UniversalLivesAmount;
    }

    private void UpdateLives(bool init = false)
    {
        if (EnableLives)
        {
            if (init)
            {
                LivesText.gameObject.SetActive(true);
                LivesLeft = GetLivesAmount();
            }
            else
            {
                LivesAnimation.Play();
            }

            LivesText.text = string.Concat("Lives: ", LivesLeft.ToString());
        }
        else if (!EnableLives && init)
            LivesText.gameObject.SetActive(false);
    }

    private void UpdateScore(bool init = false)
    {
        UpdateScore(0, init);
    }

    private void UpdateScore(int increase, bool init = false)
    {
        if (init)
            CurrentScore = 0;
        else
        {
            CurrentScore += increase;
            ScoreAnimation.Play();
        }

        ScoreText.text = string.Concat("Score: ", CurrentScore);
    }

    private int GetQuestionLimit()
    {
        if (CurrentCategory.LimitQuestions)
        {
            if (QuestionList.Count > CurrentCategory.QuestionLimit)
                return CurrentCategory.QuestionLimit - 1;
            else
                return QuestionList.Count - 1;
        }
        else
            return QuestionList.Count - 1;
    }

    //Shuffles the question list
    void ShuffleQuestions()
    {
        if (!CurrentCategory.ShuffleQuestions)
            return;

        for (int index = 0; index < QuestionList.Count; index++)
        {
            QuestionFormat tempNumber = QuestionList[index];

            int randomIndex = Random.Range(index, QuestionList.Count);

            QuestionList[index] = QuestionList[randomIndex];

            QuestionList[randomIndex] = tempNumber;
        }
    }

    //Shuffles the answer list
    void ShuffleAnswers()
    {
        if (!CurrentCategory.ShuffleAnswers)
            return;

        for (int index = 0; index < AnswersList.Count; index++)
        {
            AnswerFormat tempNumber = AnswersList[index];

            int randomIndex = Random.Range(index, AnswersList.Count);

            AnswersList[index] = AnswersList[randomIndex];

            AnswersList[randomIndex] = tempNumber;
        }
    }

    private IEnumerator CacheImages()
    {
        if (ImageLinks.Count <= 0 || isGameOver)
            yield break;

        string EncodedString = CalculateMD5Hash(ImageLinks[0]);

        string filePath = Path.Combine(Application.temporaryCachePath, EncodedString);

        //Check if the image already exists
        if (!File.Exists(filePath))
        {
            //Initialize a new bool and set it to false. We will use this to retry a download in case it fails on the first try
            int retried = 0;

        //We will use this if we need to retry a download in case it fails on the first try
        TryAgain:

            //Start a new download from the provided URL
            using (UnityWebRequest CacheRequest = UnityWebRequestTexture.GetTexture(ImageLinks[0]))
            {
                //Reset the timeout amount to the default
                CacheRequest.timeout = DownloadTimeout;

                yield return CacheRequest.SendWebRequest();

                //Check if the downloaded successfully
                if (!CacheRequest.isNetworkError && !CacheRequest.isHttpError)
                {
                    Texture2D testTexture = DownloadHandlerTexture.GetContent(CacheRequest);

                    if (testTexture.width == 8 && testTexture.height == 8)
                    {
#if DEBUG
                        Debug.LogWarning("Unsupported image was downloaded. Deleting file...");
#endif
                        if (File.Exists(filePath))
                            File.Delete(filePath);

                        if (retried < 2)
                        {
                            yield return new WaitForSeconds(1);
                            retried++;
                            goto TryAgain;
                        }
                        else
                        {
                            FaultyLinks.Add(ImageLinks[0]);
                            ImageLinks.RemoveAt(0);
                            StartCoroutine(CacheImages());
                        }

                    }
                    else
                    {
                        File.WriteAllBytes(filePath, CacheRequest.downloadHandler.data);
                        ImageLinks.RemoveAt(0);
                        StartCoroutine(CacheImages());
                    }

                    Destroy(testTexture);
                }
                else
                {
                    //If the download failed, retry again in x seconds if we haven't retried it before
                    if (retried < 2)
                    {
                        yield return new WaitForSeconds(1);
                        retried++;
                        goto TryAgain;
                    }
                    else
                    {
                        FaultyLinks.Add(ImageLinks[0]);
                        ImageLinks.RemoveAt(0);
                        StartCoroutine(CacheImages());
                    }
                }
            }
        }
        else
        {
            ImageLinks.RemoveAt(0);
            StartCoroutine(CacheImages());
        }
    }

    private string CalculateMD5Hash(string input)
    {
        UTF8Encoding ue = new UTF8Encoding();
        byte[] bytes = ue.GetBytes(input);

        MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
        byte[] hashBytes = md5.ComputeHash(bytes);

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < hashBytes.Length; i++)
        {
            sb.Append(hashBytes[i].ToString("x2"));
        }

        return sb.ToString();
    }

    private IEnumerator LoadQuestion()
    {
        MainQuestionDisplay.gameObject.SetActive(false);
        AltQuestionDisplay.gameObject.SetActive(false);
        ImageDisplay.gameObject.SetActive(false);

        ToFAnswersParent.SetActive(false);
        AnswerBarsParent.SetActive(false);
        ShortAnswerBarsParent.SetActive(false);

        if (CurrentCategory.Mode == DownloadMode.Online)
        {
            if (QuestionList[CurrentQuestion].Image.Length > 1)
            {
                string EncodedString = CalculateMD5Hash(QuestionList[CurrentQuestion].Image);
                string filePath = Path.Combine(Application.temporaryCachePath, EncodedString);

            TryAgain:

                WaitingForImage = false;

                //Check if the image already exists
                if (File.Exists(filePath))
                {
                    //Load the image from the phone storage
                    using (UnityWebRequest ImageRequest = UnityWebRequestTexture.GetTexture("file:///" + filePath))
                    {

                        //Wait for the image to load successfully
                        yield return ImageRequest.SendWebRequest();

                        if (ImageRequest.isHttpError || ImageRequest.isNetworkError)
                        {
#if DEBUG
                            Debug.Log("Image Request error - " + ImageRequest.error);
#endif
                            Continue();
                            yield break;
                        }

                        Texture2D imgTex = DownloadHandlerTexture.GetContent(ImageRequest);

                        //Load the sprite to the image display
                        ImageDisplay.texture = imgTex;

                        ImageAspectRatioFitter.aspectRatio = (float)imgTex.width / (float)imgTex.height;
                    }
                }
                else
                {
                    int timer = 0;

                    ImageDownloadAnimation.gameObject.SetActive(true);
                    ImageDownloadAnimation.Play();

                    WaitingForImage = true;

                    while (!File.Exists(filePath))
                    {
                        yield return new WaitForSeconds(1f);


                        timer++;

                        for (int count = 0; count < FaultyLinks.Count; count++)
                        {
                            if (QuestionList[CurrentQuestion].Image.Equals(FaultyLinks[count]))
                            {
                                ImageDownloadAnimation.Stop();
                                ImageDownloadAnimation.gameObject.SetActive(false);
                                Continue();
                                yield break;
                            }
                        }

                        if (timer >= DownloadTimeout)
                        {
                            ImageDownloadAnimation.Stop();
                            ImageDownloadAnimation.gameObject.SetActive(false);
                            Continue();
                            yield break;
                        }
                    }

                    ImageDownloadAnimation.Stop();
                    ImageDownloadAnimation.gameObject.SetActive(false);

                    goto TryAgain;
                }

                AltQuestionDisplay.text = QuestionList[CurrentQuestion].Question;
                AltQuestionDisplay.gameObject.SetActive(true);
                ImageDisplay.gameObject.SetActive(true);
            }
            else
            {
                MainQuestionDisplay.text = QuestionList[CurrentQuestion].Question;
                MainQuestionDisplay.gameObject.SetActive(true);
            }
        }
        else
            SetupQuestion();

        if (QuestionList[CurrentQuestion].isToF)
        {
            ToFAnswersHeading.gameObject.SetActive(false);

            ToFAnswerHolder[0].SetActive(true);
            ToFAnswerHolder[1].SetActive(true);

            ToFAnswersParent.SetActive(true);

            ToFAnswerHoldersCanvasGroup.blocksRaycasts = true;

            ToFQuestion = true;
        }
        else
        {
            AnswersList.Clear();

            for (int x = 0; x < QuestionList[CurrentQuestion].Answers.Length; x++)
                AnswersList.Add(QuestionList[CurrentQuestion].Answers[x]);

            ShuffleAnswers();

            if (AspectRatio == AspectRatios.Default)
            {

                for (int count = 0; count < AnswerDisplay.Length; count++)
                {
                    if (count >= AnswersList.Count)
                    {
                        AnswerParent[count].gameObject.SetActive(false);
                        continue;
                    }

                    AnswerParent[count].color = DefaultColor;
                    AnswerParent[count].gameObject.SetActive(true);
                    AnswerDisplay[count].text = AnswersList[count].Text;
                }

                AnswerBarsParent.SetActive(true);

                AnswerBarsCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                for (int count = 0; count < ShortAnswerDisplay.Length; count++)
                {
                    if (count >= AnswersList.Count)
                    {
                        ShortAnswerParent[count].gameObject.SetActive(false);
                        continue;
                    }

                    ShortAnswerParent[count].color = DefaultColor;
                    ShortAnswerParent[count].gameObject.SetActive(true);
                    ShortAnswerDisplay[count].text = AnswersList[count].Text;
                }

                ShortAnswerBarsParent.SetActive(true);

                ShortAnswerBarsCanvasGroup.blocksRaycasts = true;
            }

            ToFQuestion = false;
        }

        Answered = false;

#if USE_DOTWEEN
        StartCoroutine(AnimateButtons());
#else
        StartCoroutine(StartTimer());
#endif

#if UNITY_EDITOR
        if (DebugAutoAnswer)
            Invoke("AutoAnswer", 0.5f);
#endif
    }

    private void SetupQuestion()
    {
        if (QuestionList[CurrentQuestion].Image.Length > 1)
        {
            Texture2D imgTex = Resources.Load<Texture2D>("Images/" + QuestionList[CurrentQuestion].Image);

            ImageDisplay.texture = imgTex;

            ImageAspectRatioFitter.aspectRatio = (float)imgTex.width / (float)imgTex.height;

            AltQuestionDisplay.text = QuestionList[CurrentQuestion].Question;
            AltQuestionDisplay.gameObject.SetActive(true);
            ImageDisplay.gameObject.SetActive(true);
        }
        else
        {
            MainQuestionDisplay.text = QuestionList[CurrentQuestion].Question;
            MainQuestionDisplay.gameObject.SetActive(true);
        }
    }

#if UNITY_EDITOR
    private void AutoAnswer()
    {
        if (QuestionList[CurrentQuestion].isToF)
        {
            ToFAnswer(QuestionList[CurrentQuestion].ToFAnswer);
        }
        else
        {
            for (int x = 0; x < AnswersList.Count; x++)
                if (AnswersList[x].Correct)
                {
                    MultipleChoiceAnswer(x);
                    return;
                }
        }
    }
#endif

#if USE_DOTWEEN
    private IEnumerator AnimateButtons()
    {
        if (ToFQuestion)
        {
            ToFAnswerHolder[0].transform.DOScale(Vector3.one, 0.2f);
            yield return new WaitForSeconds(0.1f);
            ToFAnswerHolder[1].transform.DOScale(Vector3.one, 0.2f);
        }
        else
        {
            if (AspectRatio == AspectRatios.Default)
            {
                for (int x = 0; x < AnswerParent.Length; x++)
                {
                    if (AnswerParent[x].gameObject.activeInHierarchy)
                    {
                        AnswerParent[x].transform.DOScale(Vector3.one, 0.15f);
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
            else
            {
                for (int x = 0; x < ShortAnswerParent.Length; x++)
                {
                    if (ShortAnswerParent[x].gameObject.activeInHierarchy)
                    {
                        ShortAnswerParent[x].transform.DOScale(Vector3.one, 0.15f);
                        yield return new WaitForSeconds(0.1f);
                    }
                }
            }
        }

        StartCoroutine(StartTimer());
    }
#endif

    private IEnumerator StartTimer()
    {
        if (!EnableTimer)
            yield break;

        while (TimeLeft > 0 && !Answered && !isGameOver && !Paused)
        {
            if (TimeLeft <= 5 && !Ticking && TickingSound != null)
            {
                PlaySound(TickingSound);
                Ticking = true;
            }

            yield return new WaitForSeconds(1);

            if (!Answered && !Paused)
                UpdateTimer();
        }

        if (!Answered && !isGameOver && !Paused)
        {
            if (ToFQuestion)
            {
                ToFAnswer(!QuestionList[CurrentQuestion].ToFAnswer);
            }
            else
            {
                HandleAnswer(false, 0, true);

                AnswerBarsCanvasGroup.blocksRaycasts = false;
            }
        }

        if (isGameOver || Paused)
        {
            if (Ticking)
            {
                StopAudioPlayer();
                Ticking = false;
            }
        }
    }

    public void ToFAnswer(bool isTrue)
    {
        int selected = 0;

        if (!isTrue)
            selected = 1;

        HandleAnswer(QuestionList[CurrentQuestion].ToFAnswer == isTrue, selected);

        ToFAnswerHoldersCanvasGroup.blocksRaycasts = false;
    }

    public void MultipleChoiceAnswer(int selected)
    {
        HandleAnswer(AnswersList[selected].Correct, selected);

        if (AspectRatio == AspectRatios.Default)
            AnswerBarsCanvasGroup.blocksRaycasts = false;
        else
            ShortAnswerBarsCanvasGroup.blocksRaycasts = false;
    }

    private void HandleAnswer(bool Correct = false, int selected = 0, bool NoResponse = false)
    {
        Answered = true;

        if (Ticking)
        {
            StopAudioPlayer();
            Ticking = false;
        }

        if (Correct)
        {
            PlaySound(CorrectSound);

            if (ToFQuestion)
            {
                ToFAnswersHeading.text = "You are correct";
                ToFAnswersHeading.gameObject.SetActive(true);

                ToggleToFButtons(selected, true);
            }
            else
            {
                if (AspectRatio == AspectRatios.Default)
                {
                    for (int x = 0; x < AnswersList.Count; x++)
                        if (AnswersList[x].Correct)
                            AnswerParent[x].color = CorrectColor;
                }
                else
                {
                    for (int x = 0; x < AnswersList.Count; x++)
                        if (AnswersList[x].Correct)
                            ShortAnswerParent[x].color = CorrectColor;
                }
            }

            if (EnableTimer)
                UpdateScore(PointsPerSecond * TimeLeft);
            else
                UpdateScore(PointsPerQuestion);
        }
        else
        {
            LivesLeft--;
            UpdateLives();

            PlaySound(WrongSound);

#if (SUPPORTED_PLATFORM || UNITY_EDITOR)
            if (VibrationEnabled)
                Handheld.Vibrate();
#endif

            if (ToFQuestion)
            {
                ToFAnswersHeading.text = "The Correct Answer is";
                ToFAnswersHeading.gameObject.SetActive(true);

                ToggleToFButtons(selected, false);
            }
            else
            {
                if (AspectRatio == AspectRatios.Default)
                {
                    if (!NoResponse)
                        AnswerParent[selected].color = WrongColor;

                    for (int x = 0; x < AnswersList.Count; x++)
                        if (AnswersList[x].Correct)
                            AnswerParent[x].color = CorrectColor;
                }
                else
                {
                    if (!NoResponse)
                        ShortAnswerParent[selected].color = WrongColor;

                    for (int x = 0; x < AnswersList.Count; x++)
                        if (AnswersList[x].Correct)
                            ShortAnswerParent[x].color = CorrectColor;
                }
            }
        }

        StartCoroutine(PostQuestion());
    }

    private void ToggleToFButtons(int current, bool show)
    {
        if (current == 0)
        {
            if (show)
            {
                ToFAnswerHolder[0].SetActive(true);
                ToFAnswerHolder[1].SetActive(false);
            }
            else
            {
                ToFAnswerHolder[0].SetActive(false);
                ToFAnswerHolder[1].SetActive(true);
            }
        }
        else
        {
            if (show)
            {
                ToFAnswerHolder[0].SetActive(false);
                ToFAnswerHolder[1].SetActive(true);
            }
            else
            {
                ToFAnswerHolder[0].SetActive(true);
                ToFAnswerHolder[1].SetActive(false);
            }
        }
    }

    private IEnumerator PostQuestion()
    {
        yield return new WaitForSeconds(PostQuestionDelay);

        if (ShowPostQuestionButtons)
        {
            Resources.UnloadUnusedAssets();

            ExtraLifeButton.SetActive(false);

#if USE_ADMOB
            if (EnableLives)
                if (LivesLeft <= 1 && !RewardedShown && AdmobManager.instance.CheckRewardedVideoStatus())
                    ExtraLifeButton.SetActive(true);
#endif

            if (QuestionList[CurrentQuestion].Explanation.Length > 0)
                ExplanationButton.SetActive(true);
            else
                ExplanationButton.SetActive(false);

            PostQuestionButtons.SetActive(true);
            ToFAnswersParent.SetActive(false);
            AnswerBarsParent.SetActive(false);
            ShortAnswerBarsParent.SetActive(false);
        }
        else
            Continue();
    }

    public void ToggleExplanationPanel(bool open)
    {
        if (!open)
        {
            ExplanationPanel.SetActive(false);
            return;
        }

        ExplanationDisplay.text = QuestionList[CurrentQuestion].Explanation;
        ExplanationPanel.SetActive(true);
    }

    public void ShowRewardedVideo()
    {
#if USE_ADMOB
        AdmobManager.instance.ShowRewardedAd();
#endif
    }

#if USE_ADMOB
    internal void IncreaseLives(int increase = 1)
    {
        ExtraLifeButton.SetActive(false);
        RewardedShown = true;

        LivesLeft += increase;
        UpdateLives();
    }
#endif

    public void Continue()
    {
        PostQuestionButtons.SetActive(false);

        if (EnableLives && LivesLeft <= 0)
        {
            GameOver();
            return;
        }

        if (CurrentQuestion < QuestionsLimit)
        {
            if (EnableTimer)
            {
                TimeLeft = GetTimerAmount();
                TimerText.text = string.Concat(TimeLeft.ToString(), "'");
            }

#if USE_DOTWEEN
            ResetScale();
#endif

            CurrentQuestion++;
            StartCoroutine(LoadQuestion());
        }
        else
            GameOver();
    }

    private void GameOver()
    {
        isGameOver = true;
        isInGame = false;
        GameOverCanvas.SetActive(true);

#if USE_ADMOB
        TotalGameOvers++;

        if (TotalGameOvers >= AdmobManager.instance.ShowInterstitialAdAfterXGameovers)
        {
            AdmobManager.instance.ShowInterstitialAd();
            TotalGameOvers = 0;
        }
#endif

        if (!ShowPostQuestionButtons)
            Resources.UnloadUnusedAssets();

        int CategoryHighscore = PlayerPrefs.GetInt(CurrentCategory.HighscorePref, 0);

        if (CurrentScore > CategoryHighscore)
        {
            CategoryHighscore = CurrentScore;
            PlayerPrefs.SetInt(CurrentCategory.HighscorePref, CurrentScore);
            PlayerPrefs.Save();
        }

        CategoryImage.sprite = CurrentCategory.CategoryImage;
        CategoryNameText.text = CurrentCategory.CategoryName;

        GameScoreText.text = CurrentScore.ToString();
        HighscoreText.text = string.Concat("Highscore: ", CategoryHighscore);
    }

    public void PauseGame()
    {
        if (WaitingForImage)
            return;

        if (!EnablePausing)
        {
            QuitGamePanel.SetActive(true);
            return;
        }

        Paused = true;
        PauseCanvas.SetActive(true);
    }

    public void ResumeGame()
    {
        if (!EnablePausing)
        {
            QuitGamePanel.SetActive(false);
            return;
        }

        Paused = false;
        PauseCanvas.SetActive(false);

        StartCoroutine(StartTimer());
    }

    public void RestartGame()
    {
        if (isHybrid && CurrentCategory.Mode == DownloadMode.Offline)
        {
            CurrentCategory.Mode = DownloadMode.Hybrid;
            LoadCategory(CurrentCategory);
            return;
        }

        PrepareGame();
    }

    public void uploadScore()
    {
        string guid = System.Guid.NewGuid().ToString("N");
        Debug.Log(guid);
        string player = playerName.text == "" ? "Anonymous" : playerName.text;
        Debug.Log(player);
        int finalScore = CurrentScore;
        Debug.Log(finalScore);
        string time = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        Debug.Log(time);

        // upload to db
        WWWForm form = new WWWForm();
        form.AddField("uuid", guid);
        form.AddField("player", player);
        form.AddField("score", finalScore);
        form.AddField("time", time);

        StartCoroutine(SendPost("http://1.117.161.63/upload2.php", form));

        ShowHome();
    }

    IEnumerator SendPost(string url, WWWForm wForm)
    {
        UnityWebRequest www = UnityWebRequest.Post(url, wForm);

        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            Debug.Log("Send Success!");
        }
    }
}