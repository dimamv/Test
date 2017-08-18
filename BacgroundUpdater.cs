using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using Squirrel;

namespace GitParser
{
    public enum ResultUpdate
    {
        updateNo=0,
        updateYes,
        updateDownLoad,
        updateApply,
        updateError
    }

    public class BacgroundUpdater
    {
        public BackgroundWorker bgwUpdate { get; private set; }
        string PathUpdate;
        public string MessageError { get; private set; }
        Label labelStaus;
        Brush ColorUpdateYes;
        Brush ColorUpdateLoad;
        Brush ColorUpdateApply;
        Brush ColorUpdateNo;
        Brush ColorUpdateError;
        public ResultUpdate UpdateResult { get; private set; }
        int secondsPeriodCheckUpdate;
        int st=7;

        public BacgroundUpdater(string PathUpdate, Label labelStaus,
            int secondsPeriodCheckUpdate)
        {
            this.PathUpdate = PathUpdate;
            this.labelStaus = labelStaus;
            this.secondsPeriodCheckUpdate = secondsPeriodCheckUpdate;
            bgwUpdate = new BackgroundWorker();
            bgwUpdate.WorkerReportsProgress = true;
            bgwUpdate.WorkerSupportsCancellation = true;
            bgwUpdate.DoWork += BgwUpdate_DoWork;
            bgwUpdate.RunWorkerCompleted += BgwUpdate_RunWorkerCompleted;
            bgwUpdate.ProgressChanged += BgwUpdate_ProgressChanged;
            ColorUpdateYes = new SolidColorBrush(Colors.LightBlue);
            ColorUpdateLoad = new SolidColorBrush(Colors.LightCyan);
            ColorUpdateApply = new SolidColorBrush(Colors.LightGreen);
            ColorUpdateNo = new SolidColorBrush(Colors.LightGray);
            ColorUpdateError = new SolidColorBrush(Colors.LightPink);
            UpdateResult = ResultUpdate.updateNo;
        }

        private void BgwUpdate_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch(e.ProgressPercentage)
            {
                case (int)ResultUpdate.updateYes:
                    labelStaus.Content = "Y";
                    labelStaus.Background = ColorUpdateYes;
                    break;
                case (int)ResultUpdate.updateDownLoad:
                    labelStaus.Content = "D";
                    labelStaus.Background = ColorUpdateLoad;
                    break;
                case (int)ResultUpdate.updateNo:
                    labelStaus.Content = "?";
                    labelStaus.Background = ColorUpdateNo;
                    break;
            }
        }
        
        private void BgwUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                UpdateResult = ResultUpdate.updateError;
                MessageError = e.Error.Message;
                labelStaus.Content = "!";
                labelStaus.Background = ColorUpdateError;
            }
            else
            {
                if(!e.Cancelled && (ResultUpdate)e.Result== ResultUpdate.updateApply)
                {
                    UpdateResult = ResultUpdate.updateApply;
                    MessageError = String.Empty;
                    labelStaus.Content = "A";
                    labelStaus.Background = ColorUpdateApply;
                }
            }
        }

        private void BgwUpdate_DoWork(object sender, DoWorkEventArgs e)
        {
            var flExit = false;
            e.Result = ResultUpdate.updateNo;
            var countCikle = 0;
            while(true)
            {
                if(countCikle >= secondsPeriodCheckUpdate)
                {
                    countCikle = 0;
                    using(var mgr = new UpdateManager(PathUpdate))
                    {
                        var updateInfo = mgr.CheckForUpdate().Result;
                        if(updateInfo.CurrentlyInstalledVersion.Version < updateInfo.FutureReleaseEntry.Version)
                        {
                            bgwUpdate.ReportProgress((int)ResultUpdate.updateYes);
                            mgr.DownloadReleases(updateInfo.ReleasesToApply).Wait();
                            bgwUpdate.ReportProgress((int)ResultUpdate.updateDownLoad);
                            mgr.ApplyReleases(updateInfo).Wait();
                            e.Result = ResultUpdate.updateApply;
                            flExit = true;
                        }
                        else
                        {
                            bgwUpdate.ReportProgress((int)ResultUpdate.updateNo);
                        }
                    }
                }
                if(flExit)
                    break;
                if(bgwUpdate.CancellationPending)
                    break;
                Thread.Sleep(1000);
                countCikle++;
            }
        }
    }
}
