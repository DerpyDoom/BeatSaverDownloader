using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class PluginUI : MonoBehaviour
    {
        static PluginUI _instance;

        private Logger log = new Logger("BeatSaverDownloader");

        public BeatSaverMasterViewController _beatSaverViewController;
        
        private RectTransform _mainMenuRectTransform;
        private MainMenuViewController _mainMenuViewController;

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
        }

        public void Start()
        {
            try
            {
                _mainMenuViewController = Resources.FindObjectsOfTypeAll<MainMenuViewController>().First();
                _mainMenuRectTransform = _mainMenuViewController.transform as RectTransform;

                CreateBeatSaverButton();
            }
            catch (Exception e)
            {
                log.Exception("EXCEPTION ON AWAKE(TRY CREATE BUTTON): " + e);
            }

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

    }
}
