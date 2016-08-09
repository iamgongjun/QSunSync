﻿using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using Newtonsoft.Json;
using SunSync.Models;
using System.IO;
using System.Threading;
using Qiniu.Util;
using Qiniu.Storage;
using Qiniu.Storage.Model;
namespace SunSync
{
    /// <summary>
    /// Interaction logic for AccountSettingPage.xaml
    /// </summary>
    public partial class AccountSettingPage : Page
    {
        private MainWindow mainWindow;
        private string myAKSKLink = "https://portal.qiniu.com/user/key";
        public AccountSettingPage(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.loadAccountInfo();
        }

        private void BackToHome_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            this.mainWindow.GotoHomePage();
        }

        /// <summary>
        /// view my ak & sk button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ViewMyAKSK_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(this.myAKSKLink);
            }
            catch (Exception ex)
            {
                Log.Error("open ak & sk link failed, " + ex.Message);
            }
        }

        /// <summary>
        /// load ak & sk from local file
        /// </summary>
        private void loadAccountInfo()
        {
            Account acct = Account.TryLoadAccount();
            if (!string.IsNullOrEmpty(acct.AccessKey))
            {
                this.AccessKeyTextBox.Text = acct.AccessKey;
            }
            if (!string.IsNullOrEmpty(acct.SecretKey))
            {
                this.SecretKeyTextBox.Text = acct.SecretKey;
            }
            this.IsAbroadAccountCheckBox.IsChecked = acct.IsAbroad;
        }

        /// <summary>
        /// save account settings to local file and check the validity of the settings
        /// </summary>
        private void SaveAccountSetting(object accountObj)
        {
            Account account = (Account)accountObj;
            //write settings to local file
            string accData = JsonConvert.SerializeObject(account);
            string myDocPath = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string appDir = System.IO.Path.Combine(myDocPath, "qsunsync");
            try
            {
                if (!Directory.Exists(appDir))
                {
                    try
                    {
                        Directory.CreateDirectory(appDir);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(string.Format("create app dir {0} failed due to {1}", appDir, ex.Message));
                        Dispatcher.Invoke(new Action(delegate
                        {
                            this.SettingsErrorTextBlock.Text = "创建本地配置路径失败";
                        }));
                    }
                }
                string accPath = System.IO.Path.Combine(appDir, "account.json");
                using (StreamWriter sw = new StreamWriter(accPath, false, Encoding.UTF8))
                {
                    sw.Write(accData);
                }
            }
            catch (Exception ex)
            {
                Log.Error("save account info to file failed, " + ex.Message);
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "帐号设置写入文件失败";
                }));
            }

            //check ak & sk validity
            Mac mac = new Mac(account.AccessKey, account.SecretKey);
            BucketManager bucketManager = new BucketManager(mac);
            StatResult statResult = bucketManager.stat("NONE_EXIST_BUCKET", "NONE_EXIST_KEY");
            if (statResult.ResponseInfo.isNetworkBroken())
            {
                Log.Error("stat file network error, " + statResult.ResponseInfo.ToString());
                Dispatcher.Invoke(new Action(delegate
                {
                    this.SettingsErrorTextBlock.Text = "网络故障";
                }));
            }
            else
            {
                if (statResult.ResponseInfo.StatusCode == 401)
                {
                    Log.Error("ak & sk wrong");
                    Dispatcher.Invoke(new Action(delegate
                    {
                        this.SettingsErrorTextBlock.Text = "AK 或 SK 设置不正确";
                    }));
                }
                else if (statResult.ResponseInfo.StatusCode == 612 || statResult.ResponseInfo.StatusCode == 631)
                {
                    Log.Info("ak & sk is valid");
                    Dispatcher.Invoke(new Action(delegate
                    {
                        this.SettingsErrorTextBlock.Text = "";
                        this.mainWindow.GotoHomePage();
                    }));
                }
                else
                {
                    Log.Error(string.Format("valid ak&sk unknown error, {0}:{1}:{2}:{3}", statResult.ResponseInfo.StatusCode,
                        statResult.ResponseInfo.Error, statResult.ResponseInfo.ReqId, statResult.Response));
                    Dispatcher.Invoke(new Action(delegate
                    {
                        this.SettingsErrorTextBlock.Text = "未知错误，请联系七牛";
                    }));
                }
            }
        }


        /// <summary>
        /// save account settings button click handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveAccountSettings_EventHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            string accessKey = this.AccessKeyTextBox.Text.Trim();
            string secretKey = this.SecretKeyTextBox.Text.Trim();
            bool isAbroad = this.IsAbroadAccountCheckBox.IsChecked.Value;
            Account account = new Account();
            account.AccessKey = accessKey;
            account.SecretKey = secretKey;
            account.IsAbroad = isAbroad;
            new Thread(new ParameterizedThreadStart(this.SaveAccountSetting)).Start(account);
        }

        private void IsAbroadChecked_EvnentHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            this.IsAbroadEventHandle(true);
        }

        private void IsAbroadUnchecked_EventHandler(object sender, System.Windows.RoutedEventArgs e)
        {
            this.IsAbroadEventHandle(false);
        }

        private void IsAbroadEventHandle(bool checkBoxVal)
        {
            if (checkBoxVal)
            {
                this.myAKSKLink = "https://portal.gdipper.com/setting/key";
                Qiniu.Common.Config.RS_HOST = "http://rs.gdipper.com";
            }
            else
            {
                this.myAKSKLink = "https://portal.qiniu.com/setting/key";
                Qiniu.Common.Config.RS_HOST = "http://rs.qiniu.com";
            }
        }
    }
}
