using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class SongListUITweaks : MonoBehaviour
    {
        SearchKeyboardViewController _searchViewController;

        LevelSelectionFlowCoordinator _levelSelectionFlowCoordinator;
        SongListViewController _songListViewController;

        RectTransform _tableViewRectTransform;

        Button _sortByButton;
        Button _favButton;
        Button _allButton;
        Button _searchButton;

        public void SongListUIFound()
        {
            if (_levelSelectionFlowCoordinator == null)
            {
                _levelSelectionFlowCoordinator = Resources.FindObjectsOfTypeAll<LevelSelectionFlowCoordinator>().First();
            }

            if (_songListViewController == null)
            {
                _songListViewController = ReflectionUtil.GetPrivateField<SongListViewController>(_levelSelectionFlowCoordinator, "_songListViewController");
            }

            if (_tableViewRectTransform == null)
            {
                _tableViewRectTransform = _songListViewController.GetComponentsInChildren<RectTransform>().First(x => x.name == "TableViewContainer");

                _tableViewRectTransform.sizeDelta = new Vector2(0f , -20f);
                _tableViewRectTransform.anchoredPosition = new Vector2(0f, -2.5f);

                RectTransform _pageUp = _tableViewRectTransform.GetComponentsInChildren<RectTransform>().First(x => x.name == "PageUpButton");
                _pageUp.anchoredPosition = new Vector2(0f, -1f);

                RectTransform _pageDown = _tableViewRectTransform.GetComponentsInChildren<RectTransform>().First(x => x.name == "PageDownButton");
                _pageDown.anchoredPosition = new Vector2(0f, 1f);
            }

            if (_sortByButton == null)
            {
                _sortByButton = BeatSaberUI.CreateUIButton(_songListViewController.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref _sortByButton, "Sort by");
                BeatSaberUI.SetButtonTextSize(ref _sortByButton, 3f);
                (_sortByButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_sortByButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _sortByButton.onClick.RemoveAllListeners();
                _sortByButton.onClick.AddListener(delegate ()
                {
                    SelectTopButtons(TopButtonsState.SortBy);
                });

            }
            else
            {
                Logger.StaticLog(_sortByButton.name);

            }

            if (_favButton == null)
            {
                _favButton = BeatSaberUI.CreateUIButton(_songListViewController.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref _favButton, "Favorites");
                BeatSaberUI.SetButtonTextSize(ref _favButton, 3f);
                (_favButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_favButton.transform as RectTransform).anchoredPosition = new Vector2(0f, 73f);
                _favButton.onClick.RemoveAllListeners();
                _favButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(true);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _favButton.gameObject.SetActive(false);
            }

            if (_allButton == null)
            {
                _allButton = BeatSaberUI.CreateUIButton(_songListViewController.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref _allButton, "All");
                BeatSaberUI.SetButtonTextSize(ref _allButton, 3f);
                (_allButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_allButton.transform as RectTransform).anchoredPosition = new Vector2(-30f, 73f);
                _allButton.onClick.RemoveAllListeners();
                _allButton.onClick.AddListener(delegate ()
                {
                    ShowLevels(false);
                    SelectTopButtons(TopButtonsState.Select);
                });
                _allButton.gameObject.SetActive(false);

            }

            if (_searchButton == null)
            {
                _searchButton = BeatSaberUI.CreateUIButton(_songListViewController.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref _searchButton, "Search");
                BeatSaberUI.SetButtonTextSize(ref _searchButton, 3f);
                (_searchButton.transform as RectTransform).sizeDelta = new Vector2(30f, 6f);
                (_searchButton.transform as RectTransform).anchoredPosition = new Vector2(-30f, 73f);
                _searchButton.onClick.RemoveAllListeners();
                _searchButton.onClick.AddListener(delegate ()
                {
                    ShowSearchKeyboard();
                    SelectTopButtons(TopButtonsState.Search);

                });
            }
        }

        public void SelectTopButtons(TopButtonsState _newState)
        {
            switch (_newState)
            {
                case TopButtonsState.Select:
                    {
                        _sortByButton.gameObject.SetActive(true);
                        _searchButton.gameObject.SetActive(true);
                        
                        _favButton.gameObject.SetActive(false);
                        _allButton.gameObject.SetActive(false);
                    }; break;
                case TopButtonsState.SortBy:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        
                        _favButton.gameObject.SetActive(true);
                        _allButton.gameObject.SetActive(true);
                    }; break;
                case TopButtonsState.Search:
                    {
                        _sortByButton.gameObject.SetActive(false);
                        _searchButton.gameObject.SetActive(false);
                        
                        _favButton.gameObject.SetActive(false);
                        _allButton.gameObject.SetActive(false);

                    }; break;


            }

        }

        void ShowSearchKeyboard()
        {
            if(_searchViewController == null)
            {
                _searchViewController = BeatSaberUI.CreateViewController<SearchKeyboardViewController>();
                _searchViewController.searchButtonPressed += _searchViewController_searchButtonPressed;
            }
            
            _songListViewController.navigationController.PresentModalViewController(_searchViewController, null, false);
        }

        private void _searchViewController_searchButtonPressed(string searchFor)
        {
            Logger.StaticLog($"Searching for \"{searchFor}\"...");
            
            SelectTopButtons(TopButtonsState.Select);
            SearchForLevels(searchFor);
        }

        void ShowLevels(bool onlyFavorites)
        {
            GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_levelSelectionFlowCoordinator, "_gameplayMode");

            if (onlyFavorites)
            {
                SetSongListLevels(PluginUI._levelCollections.GetLevels(gameplayMode).Where(x => PluginConfig.favouriteSongs.Contains(x.levelId)).ToArray());
            }
            else
            {
                SetSongListLevels(PluginUI._levelCollections.GetLevels(gameplayMode));
            }
        }

        void SearchForLevels(string searchFor)
        {
            GameplayMode gameplayMode = ReflectionUtil.GetPrivateField<GameplayMode>(_levelSelectionFlowCoordinator, "_gameplayMode");

            SetSongListLevels(PluginUI._levelCollections.GetLevels(gameplayMode).Where(x => $"{x.songName} {x.songSubName} {x.authorName}".ToLower().Contains(searchFor)).ToArray());
        }

        void SetSongListLevels(ILevel[] levels)
        {
            SongListViewController songListViewController = ReflectionUtil.GetPrivateField<SongListViewController>(_levelSelectionFlowCoordinator, "_songListViewController");

            ReflectionUtil.SetPrivateField(songListViewController.GetComponentInChildren<SongListTableView>(), "_levels", levels);
            ReflectionUtil.SetPrivateField(songListViewController, "_levels", levels);

            TableView _songListTableView = songListViewController.GetComponentInChildren<TableView>();
            _songListTableView.ReloadData();

            //int row = songListViewController.GetComponentInChildren<SongListTableView>().RowNumberForLevelID(nextLevelId);
            //_songListTableView.SelectRow(row, true);
            //_songListTableView.ScrollToRow(row, true);
        }

    }
}
