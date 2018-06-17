using VRUI;

namespace BeatSaverDownloader.PluginUI
{
    internal class BeatSaverSongDetailViewController : VRUIViewController
    {
        BeatSaverMasterViewController _parentMasterViewController;

        protected override void LeftAndRightScreenViewControllers(out VRUIViewController leftScreenViewController, out VRUIViewController rightScreenViewController)
        {
            if(_parentMasterViewController == null)
            {
                _parentMasterViewController = GetComponentInParent<BeatSaverMasterViewController>();
            }
            if (_parentMasterViewController._downloadQueueViewController == null)
            {
                _parentMasterViewController._downloadQueueViewController = BeatSaberUI.CreateViewController<DownloadQueueViewController>();
                _parentMasterViewController._downloadQueueViewController._parentMasterViewController = _parentMasterViewController;
            }
            leftScreenViewController = _parentMasterViewController._downloadQueueViewController;
            rightScreenViewController = null;
        }

    }
}