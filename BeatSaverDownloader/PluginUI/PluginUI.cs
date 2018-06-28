using HMUI;
using SimpleJSON;
using SongLoaderPlugin;
using SongLoaderPlugin.OverrideClasses;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class PluginUI : MonoBehaviour
    {
        static PluginUI _instance;
        private VotingUI _votingUI;
        private SongListUITweaks _tweaks;

        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverMasterViewController _beatSaverViewController;
        
        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;

        private LevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;

        private SongDetailViewController _songDetailViewController;

        private Button _deleteButton;
        private Button _playButton;
        private Button _favButton;
        private Prompt _confirmDeleteState;
        
        public static LevelCollectionsForGameplayModes _levelCollections;
        public static List<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode> _levelCollectionsForGameModes;

        public static string playerId;

        private bool isDeleting;

        private bool _deleting
        {
            get { return isDeleting; }
            set
            {
                isDeleting = value;
                if (value)
                {
                    _playButton.interactable = false;
                }
                else
                {
                    _playButton.interactable = true;
                }
            }
        }

        public static void OnLoad()
        {
            if (_instance != null)
            {
                return;
            }
            new GameObject("BeatSaver Plugin").AddComponent<PluginUI>();
        }

        public void Awake()
        {
            _instance = this;
            _votingUI = gameObject.AddComponent<VotingUI>();
            _tweaks = gameObject.AddComponent<SongListUITweaks>();
        }

        public void Start()
        {
            playerId = ReflectionUtil.GetPrivateField<string>(PersistentSingleton<PlatformLeaderboardsModel>.instance, "_playerId");
            
            StartCoroutine(_votingUI.WaitForResults());
            _votingUI.continuePressed += _votingUI_continuePressed;
            StartCoroutine(WaitForSongListUI());

            _levelCollections = Resources.FindObjectsOfTypeAll<LevelCollectionsForGameplayModes>().FirstOrDefault();
            _levelCollectionsForGameModes = ReflectionUtil.GetPrivateField<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode[]>(_levelCollections, "_collections").ToList();

            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<LevelSelectionFlowCoordinator>().First();

                ReflectionUtil.GetPrivateField<SongListViewController>(_levelSelectionFlowCoordinator, "_songListViewController").didSelectSongEvent += PluginUI_didSelectSongEvent;

                CreateBeatSaverButton();
            }
            catch (Exception e)
            {
                log.Exception("EXCEPTION ON AWAKE(TRY CREATE BUTTON): " + e);
            }
        }
        
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                PluginConfig.LoadOrCreateConfig();
            }
        }

        private IEnumerator WaitForSongListUI()
        {
            log.Log("Waiting for song list...");

            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<SongListViewController>().Count() > 0; });

            _tweaks.SongListUIFound();

            log.Log("Found song list!");
        }

        private void _votingUI_continuePressed(ResultsViewController sender)
        {
            PluginUI_didSelectSongEvent(null, sender.difficultyLevel.level);
        }
        
        private void PluginUI_didSelectSongEvent(SongListViewController sender, ILevel selectedLevel)
        {
            if (_deleting)
            {
                _confirmDeleteState = Prompt.No;
            }

            if (_songDetailViewController == null)
            {
                _songDetailViewController = ReflectionUtil.GetPrivateField<SongDetailViewController>(_levelSelectionFlowCoordinator, "_songDetailViewController");
            }

            if(_deleteButton == null)
            {
                _deleteButton = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "PlayButton");

                BeatSaberUI.SetButtonText(ref _deleteButton, "Delete");

                (_deleteButton.transform as RectTransform).anchoredPosition = new Vector2(27f, 6f);
                (_deleteButton.transform as RectTransform).sizeDelta = new Vector2(18f, 10f);

                _deleteButton.onClick.RemoveAllListeners();
                _deleteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(DeleteSong(selectedLevel.levelId));
                });
                if (selectedLevel.levelId.Length <= 32)
                {
                    _deleteButton.interactable = false;
                }
            }
            else
            {
                if(selectedLevel.levelId.Length > 32)
                {
                    _deleteButton.onClick.RemoveAllListeners();
                    _deleteButton.onClick.AddListener(delegate ()
                    {
                        StartCoroutine(DeleteSong(selectedLevel.levelId));
                    });

                    _deleteButton.interactable = true;
                }
                else
                {
                    _deleteButton.interactable = false;
                }
            }

            if(_playButton == null)
            {
                _playButton = _songDetailViewController.GetComponentInChildren<Button>();
                (_playButton.transform as RectTransform).sizeDelta = new Vector2(30f, 10f);
                (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
            }

            if (_favButton == null)
            {
                _favButton = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

                RectTransform iconTransform = _favButton.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Icon");
                iconTransform.gameObject.SetActive(true);
                Destroy(iconTransform.parent.GetComponent<HorizontalLayoutGroup>());
                iconTransform.sizeDelta = new Vector2(8f, 8f);

                Destroy(_favButton.GetComponentsInChildren<RectTransform>(true).First(x => x.name == "Text").gameObject);

                BeatSaberUI.SetButtonText(ref _favButton, "");
                BeatSaberUI.SetButtonIcon(ref _favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel.levelId) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));
                (_favButton.transform as RectTransform).anchoredPosition = new Vector2(-24f, 6f);
                (_favButton.transform as RectTransform).sizeDelta = new Vector2(10f, 10f);

                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    if (PluginConfig.favoriteSongs.Contains(selectedLevel.levelId))
                    {

                        PluginConfig.favoriteSongs.Remove(selectedLevel.levelId);
                    }
                    else
                    {
                        PluginConfig.favoriteSongs.Add(selectedLevel.levelId);
                    }
                    BeatSaberUI.SetButtonIcon(ref _favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel.levelId) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));
                    PluginConfig.SaveConfig();
                });
            }
            else
            {
                BeatSaberUI.SetButtonIcon(ref _favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel.levelId) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));

                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    if (PluginConfig.favoriteSongs.Contains(selectedLevel.levelId))
                    {

                        PluginConfig.favoriteSongs.Remove(selectedLevel.levelId);
                    }
                    else
                    {
                        PluginConfig.favoriteSongs.Add(selectedLevel.levelId);
                    }
                    BeatSaberUI.SetButtonIcon(ref _favButton, Base64ToSprite(PluginConfig.favoriteSongs.Contains(selectedLevel.levelId) ? Base64Sprites.RemoveFromFavorites : Base64Sprites.AddToFavorites));
                    PluginConfig.SaveConfig();
                });
            }

        }
        

        IEnumerator DeleteSong(string levelId)
        {
            if (levelId.Length > 32)
            {

                LevelStaticData[] _levelsForGamemode = _levelCollections.GetLevels(_songDetailViewController.gameplayMode);

                string nextLevelId = _levelsForGamemode[_levelsForGamemode.ToList().FindIndex(x => x.levelId == levelId)-1].levelId;
                
                bool zippedSong = false;
                _deleting = true;

                string _songPath = SongLoader.CustomSongInfos.First(x => x.levelId == levelId).path;

                if (!string.IsNullOrEmpty(_songPath) && _songPath.Contains("/.cache/"))
                {
                    zippedSong = true;
                }

                if (string.IsNullOrEmpty(_songPath))
                {
                    log.Error("Song path is null or empty!");
                    _playButton.interactable = true;
                    yield break;
                }
                if (!Directory.Exists(_songPath))
                {
                    log.Error("Song folder does not exists!");
                    _playButton.interactable = true;
                    yield break;
                }

                yield return PromptDeleteFolder(_songPath);

                if (_confirmDeleteState == Prompt.Yes)
                {
                    if (zippedSong)
                    {
                        log.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath, true);

                        string songHash = Directory.GetParent(_songPath).Name;

                        if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                        {
                            log.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                            Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                        }

                        string docPath = Application.dataPath;
                        docPath = docPath.Substring(0, docPath.Length - 5);
                        docPath = docPath.Substring(0, docPath.LastIndexOf("/"));
                        string customSongsPath = docPath + "/CustomSongs/";

                        string hash = "";

                        foreach (string file in Directory.GetFiles(customSongsPath, "*.zip"))
                        {
                            if (CreateMD5FromFile(file, out hash))
                            {
                                if (hash == songHash)
                                {
                                    File.Delete(file);
                                    break;
                                }
                            }
                        }

                    }
                    else
                    {
                        log.Log("Deleting \"" + _songPath.Substring(_songPath.LastIndexOf('/')) + "\"...");
                        Directory.Delete(_songPath, true);
                        if (Directory.GetFileSystemEntries(_songPath.Substring(0, _songPath.LastIndexOf('/'))).Length == 0)
                        {
                            log.Log("Deleting empty folder \"" + _songPath.Substring(0, _songPath.LastIndexOf('/')) + "\"...");
                            Directory.Delete(_songPath.Substring(0, _songPath.LastIndexOf('/')), false);
                        }
                    }

                    var prevSoloLevels = _levelCollections.GetLevels(GameplayMode.SoloStandard).ToList();
                    var prevOneSaberLevels = _levelCollections.GetLevels(GameplayMode.SoloOneSaber).ToList();
                
                    prevSoloLevels.RemoveAll(x => x.levelId == levelId);
                    prevOneSaberLevels.RemoveAll(x => x.levelId == levelId);

                    var prevCollections =
                    ReflectionUtil.GetPrivateField<LevelCollectionsForGameplayModes.LevelCollectionForGameplayMode[]>(
                        _levelCollections, "_collections").ToList();
                    var newSoloLevelsData = ScriptableObject.CreateInstance<CustomLevelCollectionStaticData>();
                    newSoloLevelsData.Init(prevSoloLevels.ToArray());
                    var newOneSaberLevelsData = ScriptableObject.CreateInstance<CustomLevelCollectionStaticData>();
                    newOneSaberLevelsData.Init(prevOneSaberLevels.ToArray());

                    var newSoloCollection = new CustomLevelCollection(GameplayMode.SoloStandard, newSoloLevelsData);
                    var newOneSaberCollection = new CustomLevelCollection(GameplayMode.SoloOneSaber, newOneSaberLevelsData);
                    var newNoArrowCollection = new CustomLevelCollection(GameplayMode.SoloNoArrows, newSoloLevelsData);
                    var newPartyCollection = new CustomLevelCollection(GameplayMode.PartyStandard, newSoloLevelsData);
                    prevCollections[0] = newSoloCollection;
                    prevCollections[1] = newOneSaberCollection;
                    prevCollections[2] = newNoArrowCollection;
                    prevCollections[3] = newPartyCollection;

                    ReflectionUtil.SetPrivateField(_levelCollections, "_collections", prevCollections.ToArray());

                    SongListViewController songListViewController = ReflectionUtil.GetPrivateField<SongListViewController>(_levelSelectionFlowCoordinator, "_songListViewController");

                    ReflectionUtil.SetPrivateField(songListViewController.GetComponentInChildren<SongListTableView>(), "_levels", _levelCollections.GetLevels(_songDetailViewController.gameplayMode));
                    ReflectionUtil.SetPrivateField(songListViewController, "_levels", _levelCollections.GetLevels(_songDetailViewController.gameplayMode));

                    TableView _songListTableView = songListViewController.GetComponentInChildren<TableView>(); 
                    _songListTableView.ReloadData();

                    int row = songListViewController.GetComponentInChildren<SongListTableView>().RowNumberForLevelID(nextLevelId);
                    _songListTableView.SelectRow(row, true);
                    _songListTableView.ScrollToRow(row, true);

                }
                _confirmDeleteState = Prompt.NotSelected;

                _deleting = false;
            }
            else
            {
                yield return null;
            }

        }

        IEnumerator PromptDeleteFolder(string dirName)
        {
            TextMeshProUGUI _deleteText = BeatSaberUI.CreateText(_songDetailViewController.rectTransform, String.Format("Delete folder \"{0}\"?", dirName.Substring(dirName.LastIndexOf('/')).Trim('/')), new Vector2(18f, -64f));

            Button _confirmDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            BeatSaberUI.SetButtonText(ref _confirmDelete, "Yes");
            (_confirmDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_confirmDelete.transform as RectTransform).anchoredPosition = new Vector2(-13f, 6f);
            _confirmDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.Yes; });

            Button _discardDelete = BeatSaberUI.CreateUIButton(_songDetailViewController.rectTransform, "ApplyButton");

            BeatSaberUI.SetButtonText(ref _discardDelete, "No");
            (_discardDelete.transform as RectTransform).sizeDelta = new Vector2(15f, 10f);
            (_discardDelete.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);
            _discardDelete.onClick.AddListener(delegate () { _confirmDeleteState = Prompt.No; });


            (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, -10f);

            yield return new WaitUntil(delegate () { return (_confirmDeleteState == Prompt.Yes || _confirmDeleteState == Prompt.No); });

            (_playButton.transform as RectTransform).anchoredPosition = new Vector2(2f, 6f);

            Destroy(_deleteText.gameObject);
            Destroy(_confirmDelete.gameObject);
            Destroy(_discardDelete.gameObject);

        }        

        private void CreateBeatSaverButton()
        {
            Button _beatSaverButton = BeatSaberUI.CreateUIButton(_mainMenuRectTransform, "QuitButton");

            try
            {
                (_beatSaverButton.transform as RectTransform).anchoredPosition = new Vector2(30f, 7f);
                (_beatSaverButton.transform as RectTransform).sizeDelta = new Vector2(28f, 10f);

                BeatSaberUI.SetButtonText(ref _beatSaverButton, "BeatSaver");

                _beatSaverButton.onClick.AddListener(delegate () {

                    try
                    {
                        if (_beatSaverViewController == null)
                        {
                            _beatSaverViewController = BeatSaberUI.CreateViewController<BeatSaverMasterViewController>();
                        }
                        _mainMenuViewController.PresentModalViewController(_beatSaverViewController, null, false);

                    }
                    catch (Exception e)
                    {
                        log.Exception("EXCETPION IN BUTTON: " + e.Message);
                    }

                });

            }
            catch (Exception e)
            {
                log.Exception("EXCEPTION: " + e.Message);
            }

        }

        public static bool CreateMD5FromFile(string path, out string hash)
        {
            hash = "";
            if (!File.Exists(path)) return false;
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(path))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);

                    StringBuilder sb = new StringBuilder();
                    foreach (byte hashByte in hashBytes)
                    {
                        sb.Append(hashByte.ToString("X2"));
                    }

                    hash = sb.ToString();
                    return true;
                }
            }
        }

        public static Sprite Base64ToSprite(string base64)
        {
            Texture2D tex = Base64ToTexture2D(base64);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), (Vector2.one / 2f));
        }

        public static Texture2D Base64ToTexture2D(string encodedData)
        {
            byte[] imageData = Convert.FromBase64String(encodedData);

            int width, height;
            GetImageSize(imageData, out width, out height);

            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.filterMode = FilterMode.Trilinear;
            texture.LoadImage(imageData);
            return texture;
        }

        private static void GetImageSize(byte[] imageData, out int width, out int height)
        {
            width = ReadInt(imageData, 3 + 15);
            height = ReadInt(imageData, 3 + 15 + 2 + 2);
        }

        private static int ReadInt(byte[] imageData, int offset)
        {
            return (imageData[offset] << 8) | imageData[offset + 1];
        }
    }
}
