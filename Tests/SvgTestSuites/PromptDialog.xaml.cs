﻿using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.NetworkInformation;
#if DOTNET40
using Ionic.Zip;
#else
using System.Net.Http;
#endif

using System.Windows;
using System.Windows.Interop;
using System.Windows.Controls;

using FolderBrowserDialog = ShellFileDialogs.FolderBrowserDialog;

namespace SvgTestSuites
{
    /// <summary>
    /// Interaction logic for PromptDialog.xaml
    /// </summary>
    public partial class PromptDialog : Window
    {
        private string _downloadeFilePath;
        private OptionSettings _optionSettings;

        public PromptDialog()
        {
            InitializeComponent();

            this.Loaded  += OnWindowLoaded;
            this.Closing += OnWindowClosing;
        }

        public OptionSettings OptionSettings
        {
            get {
                return _optionSettings;
            }
            set {
                if (value != null)
                {
                    _optionSettings = value;
                }
            }
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (_optionSettings != null)
            {
                txtSvgSuitePath.Text    = _optionSettings.LocalSuitePath;
                txtSvgSuitePathWeb.Text = _optionSettings.WebSuitePath;

                btnOK.IsEnabled       = OptionSettings.IsTestSuiteAvailable(_optionSettings.LocalSuitePath);
                btnDownload.IsEnabled = NetworkInterface.GetIsNetworkAvailable();
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(_downloadeFilePath) &&
                File.Exists(_downloadeFilePath))
            {
                File.Delete(_downloadeFilePath);
            }
        }

        private void OnBrowseForSvgSuitePath(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            string selectedDirectory = FolderBrowserDialog.ShowDialog(windowHandle,
                "Select the location of the W3C SVG 1.1 Full Test Suite", null);
            if (!string.IsNullOrWhiteSpace(selectedDirectory))
            {
                txtSvgSuitePath.Text = selectedDirectory;

                UpdateStates();
            }
        }

        private void OnOpenSvgSuitePath(object sender, RoutedEventArgs e)
        {
            var filePath = txtSvgSuitePath.Text;
            if (string.IsNullOrWhiteSpace(filePath) || Directory.Exists(filePath) == false)
            {
                return;
            }

            OptionSettings.OpenFolderAndSelectItem(filePath, null);
        }

#if DOTNET40
        private void OnDownloadClicked(object sender, RoutedEventArgs e)
        {
            LoadingAdorner.IsAdornerVisible = true;

            string url = _optionSettings.WebSuitePath;

            _downloadeFilePath = Path.Combine(_optionSettings.LocalSuitePath, "FullTestSuite.zip");
            if (File.Exists(_downloadeFilePath))
            {
                File.Delete(_downloadeFilePath);
            }

            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; //TLS 1.2
            //ServicePointManager.SecurityProtocol = (SecurityProtocolType)768; //TLS 1.1
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)768 | (SecurityProtocolType)3072;

            using (WebClient client = new WebClient())
            {
                client.DownloadFileCompleted += delegate(object other, AsyncCompletedEventArgs args) {
                    bool result = !args.Cancelled;
                    if (!result)
                    {
                        return;
                    }
                    using (ZipFile zip = ZipFile.Read(_downloadeFilePath))
                    {
                        zip.ExtractAll(_optionSettings.LocalSuitePath);
                    }

                    this.DialogResult = true;
                    this.Close();
                };
                client.DownloadFileAsync(new Uri(url), _downloadeFilePath);
            }
        }
#else
        private async void OnDownloadClicked(object sender, RoutedEventArgs e)
        {
            LoadingAdorner.IsAdornerVisible = true;

            string url = _optionSettings.WebSuitePath;

            _downloadeFilePath = Path.Combine(_optionSettings.LocalSuitePath, "FullTestSuite.zip");
            if (File.Exists(_downloadeFilePath))
            {
                File.Delete(_downloadeFilePath);
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            using (HttpClient client = new HttpClient())
            {
                using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
                using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
                {
                    using (Stream streamToWriteTo = File.Open(_downloadeFilePath, FileMode.Create))
                    {
                        await streamToReadFrom.CopyToAsync(streamToWriteTo);
                    }

                    ZipFile.ExtractToDirectory(_downloadeFilePath, _optionSettings.LocalSuitePath);

                    this.DialogResult = true;
                    this.Close();
                }
            }
        }
#endif

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void OnOKClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void OnSvgSuitePathTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateStates();
        }

        private void OnSvgSuitePathFocusableChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateStates();
        }

        private void UpdateStates()
        {
            if (!this.IsLoaded)
            {
                return;
            }

            string selectePath = txtSvgSuitePath.Text;
            if (selectePath != null)
            {
                selectePath = selectePath.Trim();
            }
            if (string.IsNullOrWhiteSpace(selectePath) || !Directory.Exists(selectePath))
            {
                btnOK.IsEnabled         = false;
                btnDownload.IsEnabled   = false;
                btnPathLocate.IsEnabled = false;

                return;
            }

            _optionSettings.LocalSuitePath = selectePath;

            btnPathLocate.IsEnabled = true;
            btnDownload.IsEnabled   = NetworkInterface.GetIsNetworkAvailable();

            btnOK.IsEnabled = OptionSettings.IsTestSuiteAvailable(_optionSettings.LocalSuitePath);
        }
    }
}
