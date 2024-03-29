﻿using BrookW.Extend;
using BrookW.Helper;
using BrookW.Model;
using BrookW.Model.Enum;
using BrookW.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace BrookW.UC
{
    public partial class Home : UserControl
    {
        /// <summary>
        /// 是否运行中
        /// </summary>
        private bool isRunning;
        private List<Server> servers;
        private Image onImage = Properties.Resources.on;
        private Image offImage = Properties.Resources.off;

        private FileDownloader downloader;
        public BrookProcessService? brookClient;


        /// <summary>
        /// 
        /// </summary>
        public Home()
        {
            InitializeComponent();
            LoadServers();
            statusLabel.Text = string.Empty;
        }
        /// <summary>
        /// 刷新数据源
        /// </summary>
        public void LoadServers()
        {
            servers = JsonHelper.ToObject<List<Server>>(Properties.Settings.Default.Servers) ?? new List<Server>();
            cbSelectServer.DataSource = null;
            // 绑定下拉框数据源
            cbSelectServer.DataSource = servers;
            cbSelectServer.DisplayMember = "Url";
            if (cbSelectServer.Items.Count > 0)
            {
                cbSelectServer.SelectedIndex = 0;
            }

            // 初始化为关状态
            pbRun.BackgroundImage = isRunning ? onImage : offImage;
            pbRun.Show();

        }

        /// <summary>
        /// 启动 & 停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pbRun_Click(object sender, EventArgs e)
        {
            var server = cbSelectServer.SelectedItem as Server;
            if (server != null)
            {
                try
                {
                    if (!isRunning)
                    {
                        brookClient = new BrookProcessService(server);
                        statusLabel.Text = "Brook client started.";
                        //http
                        SetProxyHelper.EnableProxy($"{brookClient.ListenAddress}:{brookClient.ListenHttpPort}");
                        //socks5
                        //SetProxyHelper.EnableProxy($"socks5={brookClient.ListenAddress}:{brookClient.ListenSocks5Port}");
                    }
                    else
                    {
                        // 停止 Brook 客户端
                        brookClient?.Stop();

                        statusLabel.Text = "Brook client stopped.";

                        SetProxyHelper.DisableProxy();
                    }
                    ChangePbRunBackgroundImage();
                }
                catch (Exception ex)
                {
                    Msg.ShowError(ex.Message);
                }
            }
            else
            {
                Msg.ShowError(Msg.UNSELECTED);
            }
        }

        /// <summary>
        /// SelectedIndexChanged
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSelectServer_SelectedIndexChanged(object sender, EventArgs e)
        {
            Server selectedServer = (Server)cbSelectServer.SelectedItem;
            // 切换下拉框选项时更新标签显示的值
            if (selectedServer != null)
            {
                lblServer.Text = selectedServer.Type.ToString();
                if (isRunning)
                {
                    if (brookClient != null)
                    {
                        brookClient?.Stop();
                        isRunning = false;
                    }
                    //启动
                    pbRun_Click(sender, e);
                }
            }
        }

        /// <summary>
        /// 更新core
        /// </summary>
        private async void UpdateCore()
        {
            // 下载器
            if (downloader == null)
            {
                downloader = new FileDownloader();
                downloader.DownloadProgressChanged += (sender, e) =>
                {
                    // 异步更新 statusLabel.Text
                    statusLabel.BeginInvoke((MethodInvoker)(() =>
                    {
                        statusLabel.Text = e.Percentage.ToString();
                    }));
                };
            }
            var success = await downloader.DownloadFileAsync(Gobal.BrookCoreUrl, brookClient);
            if (success)
            {
                Thread.Sleep(1500);
                // statusLabel.Text = "更新完毕";
                if (isRunning)
                    cbSelectServer_SelectedIndexChanged(EventArgs.Empty, EventArgs.Empty);
            }
            else
            {
                //statusLabel.Text = "下载失败，可能无法获取远程文件，\n请启动代理更新.";
            }
        }


        /// <summary>
        /// 更换图片
        /// </summary>
        private void ChangePbRunBackgroundImage()
        {
            // 切换图片
            isRunning = !isRunning;
            pbRun.BackgroundImage = isRunning ? onImage : offImage;
        }

        /// <summary>
        /// 查看
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lblView_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {

            // 查看服务器详情
            var selectedItem = cbSelectServer.SelectedItem as Server;
            if (selectedItem != null)
            {
                Msg.ShowInfo($"SERVER: {selectedItem.Type.ToString()}\nURL: {selectedItem.Url}\nPASSWORD: {selectedItem.Password}");
            }
            else
            {
                Msg.ShowError(Msg.UNSELECTED);
            }
        }

        /// <summary>
        ///  检查更新事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void brookCoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateCore();
        }

        private void brookWToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
    }
}
