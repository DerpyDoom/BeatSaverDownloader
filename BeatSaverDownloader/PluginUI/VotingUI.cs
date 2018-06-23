using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace BeatSaverDownloader.PluginUI
{
    class VotingUI : MonoBehaviour
    {

        private Logger log = new Logger("BeatSaverDownloader");

        public event Action<ResultsViewController> continuePressed;

        Button upvoteButton;
        Button downvoteButton;
        TextMeshProUGUI ratingText;

        Song votingSong;

        public IEnumerator WaitForResults()
        {
            log.Log("Waiting for results view controller");
            yield return new WaitUntil(delegate () { return Resources.FindObjectsOfTypeAll<ResultsViewController>().Count() > 0; });

            log.Log("Found results view controller!");

            ResultsViewController results = Resources.FindObjectsOfTypeAll<ResultsViewController>().First();

            results.continueButtonPressedEvent += continuePressed;

            log.Log($"Player ID: {PluginUI.playerId}");
            log.Log($"Level ID: {results.difficultyLevel.level.levelId}");

            if (results.difficultyLevel.level.levelId.Length > 32)
            {
                ratingText = BeatSaberUI.CreateText(results.rectTransform, "LOADING...", new Vector2(51.5f, -40f));
                ratingText.rectTransform.sizeDelta = new Vector2(100f, 10f);
                ratingText.alignment = TextAlignmentOptions.Center;
                ratingText.fontSize = 7f;

                upvoteButton = BeatSaberUI.CreateUIButton(results.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref upvoteButton, "+");
                BeatSaberUI.SetButtonTextSize(ref upvoteButton, 7f);
                (upvoteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 45f);
                upvoteButton.interactable = false;


                upvoteButton.onClick.RemoveAllListeners();
                upvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(true));
                });

                downvoteButton = BeatSaberUI.CreateUIButton(results.rectTransform, "ApplyButton");
                BeatSaberUI.SetButtonText(ref downvoteButton, "-");
                BeatSaberUI.SetButtonTextSize(ref downvoteButton, 7f);
                (downvoteButton.transform as RectTransform).anchoredPosition = new Vector2(40f, 26f);
                downvoteButton.interactable = false;

                downvoteButton.onClick.RemoveAllListeners();
                downvoteButton.onClick.AddListener(delegate ()
                {
                    StartCoroutine(VoteForSong(false));
                });

                UnityWebRequest www = UnityWebRequest.Get(String.Format("https://beatsaver.com/api.php?mode=hashinfo&hash={0}", results.difficultyLevel.level.levelId.Substring(0, 32)));
                www.timeout = 10;
                yield return www.SendWebRequest();

                if (www.isNetworkError || www.isHttpError)
                {
                    log.Error(www.error);
                    TextMeshProUGUI _errorText = BeatSaberUI.CreateText(results.rectTransform, www.error, new Vector2(40f, -30f));
                    _errorText.alignment = TextAlignmentOptions.Center;
                    Destroy(_errorText.gameObject, 2f);
                }
                else
                {
                    try
                    {
                        JSONNode node = JSON.Parse(www.downloadHandler.text);

                        votingSong = new Song(node[0]);

                        ratingText.text = votingSong.upvotes;

                        upvoteButton.interactable = true;
                        downvoteButton.interactable = true;

                    }
                    catch (Exception e)
                    {
                        log.Exception("EXCEPTION(GET SONG RATING): " + e);
                    }
                }

            }
        }

        IEnumerator VoteForSong(bool upvote)
        {
            log.Log($"Voting...");

            upvoteButton.interactable = false;
            downvoteButton.interactable = false;

            UnityWebRequest voteWWW = UnityWebRequest.Get($"https://beatsaver.com/vote.php?t=1&id={votingSong.id}&type={(upvote ? 1 : 0)}&name={PluginUI.playerId}");
            voteWWW.timeout = 10;
            yield return voteWWW.SendWebRequest();

            if (voteWWW.isNetworkError || voteWWW.isHttpError)
            {
                log.Error(voteWWW.error);
                ratingText.text = voteWWW.error;
            }
            else
            {
                upvoteButton.interactable = true;
                downvoteButton.interactable = true;

                int parsedInt;

                if (int.TryParse(voteWWW.downloadHandler.text, out parsedInt))
                {
                    switch (parsedInt)
                    {
                        case 0:
                            {
                                ratingText.text = (int.Parse(votingSong.upvotes) + (upvote ? 1 : -1)).ToString();
                            }; break;
                        case 2: break;
                        case 1:
                            {
                                ratingText.text = "Error";
                            }; break;
                        default:
                            {
                                ratingText.text = "Fatal error";
                            }; break;
                    }
                }
                else
                {
                    ratingText.text = voteWWW.downloadHandler.text;
                }
            }

        }

    }
}
