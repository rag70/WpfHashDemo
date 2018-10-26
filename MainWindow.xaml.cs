// Authors:
//  Roberto Alonso Gómez  <bob@lynza.com>
//
// Copyright (C) 2008 Lynza (http://lynza.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Resources;
using WpfHashDemo.Code;

namespace WpfHashDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private long LastTick { set; get; }
        private ulong Folders { set; get; }
        private ulong HashCount { set; get; }
        private bool  IsClosing { set; get; }
        private bool IsRunning { set; get; }
        private bool Collision { set; get; }

        public MainWindow()
        {
            InitializeComponent();
            IsClosing = false;
            IsRunning = false;

            VisualMd vm = new VisualMd
            {
                Ls32 = this.Ls32,
                Ls64 = this.Ls64
            };

            DataContext = vm;
            Stream source = (Stream)Assembly.GetExecutingAssembly().GetManifestResourceStream("WpfHashDemo.Content.Main.html");
            WebB.NavigateToStream(source);
            Clear();
        }

        private void Clear()
        {
            foreach (Hash32Rec hr in Ls32)
            {
                hr.Dic = new Dictionary<uint, string[]>();
                hr.Collision = new List<uint>();
            }

            foreach (Hash64Rec hr in Ls64)
            {
                hr.Dic = new Dictionary<ulong, string[]>();
                hr.Collision = new List<ulong>();
            }
        }

        private void SelectFolder(object sender, RoutedEventArgs e)
        {
            if (!IsRunning)
            {
                using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
                {
                    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        try
                        {
                            Folders = 0;
                            HashCount = 0;
                            Refresh();
                            Collision = false;

                            WebB.InvokeScript("AddInfo", new object[] { "Select for start to test" });
                            var t = new Thread(StartSearch);
                            t.Start(dialog.SelectedPath);
                        }
                        catch (Exception)
                        {
                            // Console.WriteLine("The process failed: {0}", ex.ToString());
                        }
                    }

                }
            }
            else
            {
                MessageBoxResult result = MessageBox.Show("Do you want to stop current processing?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    IsClosing = true;
                }
            }
        }

        private void StartSearch(object par)
        {
            IsRunning = true;
            LastTick = DateTime.Now.Ticks;
            DirSearch((string)par);
            if (!IsClosing)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    Refresh();
                    TbStatus.Text = $"Finish with ({Folders}) Folders check ({HashCount}) Hashes";
                }));
            }
            IsRunning = false;
            IsClosing = false;
        }

        private void DirSearch(string sDir)
        {
            if (!IsClosing)
            {
                try
                {
                    CheckHash(sDir);
                    foreach (string d in Directory.GetDirectories(sDir))
                    {
                        string fn = Path.GetFileName(d);
                        if (fn[0] != '.')  // don't goes to the SVN or CVS directory
                        {
                            string[] files = Directory.GetFiles(d, "*.*");
                            foreach (string s in files)
                            {
                                CheckHash(s);
                            }
                            DirSearch(d);
                        }
                    }
                    Folders++;
                    if (TimeSpan.FromTicks(DateTime.Now.Ticks - LastTick).TotalMilliseconds > 300)
                    {
                        if (!IsClosing)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                Refresh();
                            }));
                        }
                        LastTick = DateTime.Now.Ticks;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        private void CheckHash(string s)
        {
            HashCount++;
            List<string> ls;
            foreach (Hash32Rec hr in Ls32)
            {
                if (hr.Check)
                {
                    uint v = hr.Hash(s);
                    if (hr.Dic.TryGetValue(v, out string[] r))
                    {
                        if (hr.Collision.IndexOf(v) == -1)
                        {
                            hr.Collision.Add(v);
                        }
                        ls = new List<string>(r)
                        {
                            s
                        };
                        hr.Dic[v] = ls.ToArray();
                        Collision = true;
                    }
                    else
                    {
                        hr.Dic[v] = new string[] { s };
                    }
                }
            }

            foreach (Hash64Rec hr in Ls64)
            {
                if (hr.Check)
                {
                    ulong v = hr.Hash(s);
                    if (hr.Dic.TryGetValue(v, out string[] r))
                    {
                        if (hr.Collision.IndexOf(v) == -1)
                        {
                            hr.Collision.Add(v);
                        }
                        ls = new List<string>(r)
                        {
                            s
                        };
                        hr.Dic[v] = ls.ToArray();
                        Collision = true;
                    }
                    else
                    {
                        hr.Dic[v] = new string[] { s };
                    }
                }
            }
        }

        /** I update the WEB if any new change, or only the number of folder proceser */
        private void Refresh()
        {
            TbStatus.Text = $"({Folders}) Folders, ({HashCount}) Hashes";

            if (Collision)
            {
                StringBuilder sb = new StringBuilder();
                StringBuilder sbif = new StringBuilder();
                string[] r;
                string s;
                foreach (Hash32Rec hr in Ls32)
                {
                    s = $"<h3>{hr.Name}, {hr.Collision.Count} Collisions</h3>";
                    sbif.Append(s);
                    if (hr.Collision.Count>0)
                    {
                        sb.Append(s);
                        foreach (uint v in hr.Collision)
                        {
                            if (hr.Dic.TryGetValue(v, out r))
                            {
                                sb.Append($"{v}<br /> {string.Join("<br />", r)}<br />");
                            }
                        }
                    }
                }

                foreach (Hash64Rec hr in Ls64)
                {
                    s = $"<h3>{hr.Name}, {hr.Collision.Count} Collisions</h3>";
                    sbif.Append(s);
                    if (hr.Collision.Count > 0)
                    {
                        sb.Append(s);
                        foreach (ulong v in hr.Collision)
                        {
                            if (hr.Dic.TryGetValue(v, out r))
                            {
                                sb.Append($"{v}<br /> {string.Join("<br />", r)}<br />");
                            }
                        }
                    }
                }

                WebB.InvokeScript("AddCollision", new object[] { sb.ToString() });
                WebB.InvokeScript("AddInfo", new object[] { sbif.ToString() });

            }
        }
        #region Tables32
        private Hash32Rec[] Ls32 = new Hash32Rec[]
        {
            new Hash32Rec
            {
                Name = "Djb32",
                Check = true,
                Hash = Hash.Djb32,
                Dic = new Dictionary<uint, string[]>(),
                Collision = new List<uint>()
            },
            new Hash32Rec
            {
                Name = "Sdbm32",
                Check = true,
                Hash = Hash.Sdbm32,
                Dic = new Dictionary<uint, string[]>(),
                Collision = new List<uint>()
            },
            new Hash32Rec
            {
                Name = "DjbSdbm16X16",
                Check = true,
                Hash = Hash.DjbSdbm16X16,
                Dic = new Dictionary<uint, string[]>(),
                Collision = new List<uint>()
            },
            new Hash32Rec
            {
                Name = "StringHashCode",
                Check = true,
                Hash = Hash.StringHashCode,
                Dic = new Dictionary<uint, string[]>(),
                Collision = new List<uint>()
            },

            new Hash32Rec
            {
                Name = "PolynomialRolling32",
                Check = true,
                Hash = Hash.PolynomialRolling32,
                Dic = new Dictionary<uint, string[]>(),
                Collision = new List<uint>()
            }
        };
        #endregion Tables32

        #region Tables64
        private Hash64Rec[] Ls64 = new Hash64Rec[]
        {
            new Hash64Rec
            {
                Name = "Djb64",
                Check = true,
                Hash = Hash.Djb64,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            },
            new Hash64Rec
            {
                Name = "Sdbm64",
                Check = true,
                Hash = Hash.Sdbm64,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            },
            new Hash64Rec
            {
                Name = "DjbSdbm32x32",
                Check = true,
                Hash = Hash.DjbSdbm32x32,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            },
            new Hash64Rec
            {
                Name = "StringHashCode64",
                Check = false,
                Hash = Hash.StringHashCode64,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            },
            new Hash64Rec
            {
                Name = "MD5Trunk64",
                Check = true,
                Hash = Hash.MD5Trunk64,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            },
            new Hash64Rec
            {
                Name = "SHA256Trunk64",
                Check = true,
                Hash = Hash.SHA256Trunk64,
                Dic = new Dictionary<ulong, string[]>(),
                Collision = new List<ulong>()
            }

        };
        #endregion Tables64

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            IsClosing = true;
        }
    }

    #region Classes and utils
    delegate uint Hash32Fn(string str);
    class Hash32Rec
    {
        public bool Check { set; get; }
        public string Name { set; get; }
        public string Url { set; get; }
        public Hash32Fn Hash { set; get; }
        public Dictionary<uint, string[]> Dic { set; get; }
        public List<uint> Collision { set; get; }
    }

    delegate ulong Hash64Fn(string str);
    class Hash64Rec
    {
        public bool Check { set; get; }
        public string Name { set; get; }
        public string Url { set; get; }
        public Hash64Fn Hash { set; get; }
        public Dictionary<ulong, string[]> Dic { set; get; }
        public List<ulong> Collision { set; get; }
    }

    class VisualMd
    {
        public Hash32Rec[] Ls32 { set; get; }
        public Hash64Rec[] Ls64 { set; get; }
    }

    #endregion Classes and utils
}
