using HMUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    class DownloadQueueViewController : VRUIViewController, TableView.IDataSource
    {
        public BeatSaverMasterViewController _parentMasterViewController;

        public List<Song> _queuedSongs = new List<Song>();

        TextMeshProUGUI _titleText;

        TableView _queuedSongsTableView;
        SongListTableCell _songListTableCellInstance;

        protected override void DidActivate(bool firstActivation, ActivationType activationType)
        {
            if (firstActivation)
            {

                _songListTableCellInstance = Resources.FindObjectsOfTypeAll<SongListTableCell>().First(x => (x.name == "SongListTableCell"));

                if (_titleText == null)
                {
                    _titleText = BeatSaberUI.CreateText(rectTransform, "DOWNLOAD QUEUE", new Vector2(0f,-6f));
                    _titleText.alignment = TextAlignmentOptions.Top;
                    _titleText.fontSize = 8;
                }

                if(_queuedSongsTableView == null)
                {
                    _queuedSongsTableView = new GameObject().AddComponent<TableView>();

                    _queuedSongsTableView.transform.SetParent(rectTransform, false);

                    _queuedSongsTableView.dataSource = this;

                    (_queuedSongsTableView.transform as RectTransform).anchorMin = new Vector2(0.3f, 0.5f);
                    (_queuedSongsTableView.transform as RectTransform).anchorMax = new Vector2(0.7f, 0.5f);
                    (_queuedSongsTableView.transform as RectTransform).sizeDelta = new Vector2(0f, 60f);
                    (_queuedSongsTableView.transform as RectTransform).anchoredPosition = new Vector3(0f, -3f);

                    _queuedSongsTableView.didSelectRowEvent += _queuedSongsTableView_DidSelectRowEvent;
                }
                else
                {
                    _queuedSongsTableView.ReloadData();
                }
            }
        }

        private void _queuedSongsTableView_DidSelectRowEvent(TableView arg1, int arg2)
        {

        }

        protected override void DidDeactivate(DeactivationType deactivationType)
        {

        }

        public void EnqueueSong(Song song)
        {
            _queuedSongs.Add(song);
            _queuedSongsTableView.ReloadData();

            StartCoroutine(DownloadSongFromQueue(song));
        }

        IEnumerator DownloadSongFromQueue(Song song)
        {
            yield return _parentMasterViewController.DownloadSongCoroutine(song);

            _queuedSongs.Remove(song);

            _queuedSongsTableView.ReloadData();
        }

        public float RowHeight()
        {
            return 10f;
        }

        public int NumberOfRows()
        {
            return _queuedSongs.Count;
        }

        public TableCell CellForRow(int row)
        {
            SongListTableCell _tableCell = Instantiate(_songListTableCellInstance);

            _tableCell.songName = string.Format("{0}\n<size=80%>{1}</size>", HTML5Decode.HtmlDecode(_queuedSongs[row].songName), HTML5Decode.HtmlDecode(_queuedSongs[row].songSubName));
            _tableCell.author = HTML5Decode.HtmlDecode(_queuedSongs[row].authorName);
            StartCoroutine(_parentMasterViewController.LoadSprite("https://beatsaver.com/img/" + _queuedSongs[row].id + "." + _queuedSongs[row].img, _tableCell));

            return _tableCell;
        }
    }
}
