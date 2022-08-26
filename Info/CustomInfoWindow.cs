using BuzzGUI.Common;
using BuzzGUI.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace WDE.Info
{
    public class CustomInfoWindow : NativeWindow, IDisposable
    {
        public bool Integrated { get; }
        public Window InfoWindow { get; set; }

        public static InfoSettings Settings = new InfoSettings();

        internal RTFBoxInfo rTFBox;

        public static bool OneInstanceCreated = false;
        private bool oldInfoDisabled;

        public CustomInfoWindow(bool integrated = false)
        {
            Integrated = integrated;
            InfoWindow = new Window();
            InfoWindow.WindowStyle = WindowStyle.None;
            //InfoWindow.Content = infoMachine.GetEditor();
            InfoWindow.ShowInTaskbar = false;
            InfoWindow.BorderThickness = new Thickness(0);
            InfoWindow.AllowsTransparency = false;
            InfoWindow.ResizeMode = ResizeMode.NoResize;
            InfoWindow.Effect = null;
            InfoWindow.Topmost = true;
            InfoWindow.PreviewKeyDown += InfoWindow_PreviewKeyDown;

            if(Integrated)
            {
                Global.Buzz.OpenSong += Buzz_OpenSong;
                Global.Buzz.SaveSong += Buzz_SaveSong;
            }

            oldInfoDisabled = false;

            rTFBox = new RTFBoxInfo();
            System.Windows.Controls.RichTextBox richTextBox = rTFBox.GetRichTextBox();
            try
            {
                richTextBox.Background = new SolidColorBrush(Global.Buzz.ThemeColors["IV BG"]);
                richTextBox.Foreground = new SolidColorBrush(Global.Buzz.ThemeColors["IV Text"]);
                richTextBox.FontSize = 16;
            } catch (Exception)
            {
            }
            richTextBox.TextChanged += (sender, e) =>
            {   
                IMachine mac = Global.Buzz.Song.Machines.FirstOrDefault(m => m.Name == "Master");
                int val = mac.ParameterGroups[0].Parameters[0].GetValue(0);
                mac.ParameterGroups[0].Parameters[0].SetValue(0, val);
            };

            IntPtr buzzClassicInfoViewHwnd = FindClassicInfoView();
            if (buzzClassicInfoViewHwnd != IntPtr.Zero)
            {
                SetWindowZOrder();
                this.AssignHandle(buzzClassicInfoViewHwnd);
            }

            Global.Buzz.PropertyChanged += Buzz_PropertyChanged;

            if (Global.Buzz.ActiveView == BuzzView.SongInfoView)
            {
                DisableBuzzInfo();
                UpdateSize();
            }

            InfoWindow.Content = rTFBox;


            Settings.PropertyChanged += Settings_PropertyChanged;
            SettingsWindow.AddSettings("Info", Settings);

            OneInstanceCreated = true;
        }

        private void Buzz_SaveSong(ISaveSong ss)
        {
            Stream s = ss.CreateSubSection("Info");
            var bw = new BinaryWriter(s);
            bw.Write(DataVersion);
            bw.Write(rTFBox.GetRTF());
            bw.Write(((SolidColorBrush)rTFBox.GetRichTextBox().Background).Color.ToString());
            bw.Write(((SolidColorBrush)rTFBox.GetRichTextBox().Foreground).Color.ToString());

        }

        int DataVersion = 1;

        void Buzz_OpenSong(IOpenSong os)
        {
            RTFBoxInfo rtfb = rTFBox;
            rtfb.Clear();
            Stream s = os.GetSubSection("Info");
            if (s == null) return;

            var br = new BinaryReader(s);
            string infodata = br.ReadString();
            Color bgc = InfoMachine.UIntToColor((uint)br.ReadInt32());
            Color fgc = InfoMachine.UIntToColor((uint)br.ReadInt32());

            if (rtfb != null)
            {
                rtfb.SetRTF(infodata);
                rtfb.GetRichTextBox().Background = new SolidColorBrush(bgc);
                rtfb.GetRichTextBox().Foreground = new SolidColorBrush(fgc);
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ShowStatusBar")
            {
                rTFBox.StatusBar.Visibility = Settings.ShowStatusBar == true ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void DisableBuzzInfo()
        {
            IntPtr buzzClassicInfoViewHwnd = FindClassicInfoView();
            var childWnds = GetChildWindows(buzzClassicInfoViewHwnd);

            if (!oldInfoDisabled)
            {
                if (childWnds.Count > 1)
                    SendMessage(childWnds[1], WM_SETREDRAW, false, 0); // Disble window updates
                else if (childWnds.Count > 0)
                    SendMessage(childWnds[0], WM_SETREDRAW, false, 0); // Disble window updates

                oldInfoDisabled = true;
            }
        }

        private void InvalidateChildWindowRect(IntPtr hwnd, bool relative )
        {
            RECT wRect;
            List<IntPtr> chldWnds = GetChildWindows(hwnd);
            if (chldWnds.Count > 0)
            {
                IntPtr editWnd = chldWnds[0];
                GetWindowRect(editWnd, out wRect);
                UpdateWindow(wRect, relative);
            }
        }

        private void InfoWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {

            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                if (e.Key == Key.Up || e.Key == Key.Down)
                {
                    e.Handled = true;
                }
                else if (e.Key == Key.S)
                {
                    Global.Buzz.ExecuteCommand(BuzzCommand.SaveFile);
                    e.Handled = true;
                }
                else if (e.Key == Key.O)
                {
                    Global.Buzz.ExecuteCommand(BuzzCommand.OpenFile);
                    e.Handled = true;
                }
                else if (e.Key == Key.N)
                {
                    Global.Buzz.ExecuteCommand(BuzzCommand.NewFile);
                    e.Handled = true;
                }
            }
            else if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt))
            {
                if (e.Key == Key.I)
                {
                    string classicInfoText = GetClassicInfoViewText();
                    string oldText = rTFBox.GetRTF();
                    if (classicInfoText != "")
                    {
                        System.Windows.Clipboard.SetData(System.Windows.DataFormats.Text, classicInfoText);
                        this.rTFBox.RichTextControl.Paste();
                        //this.rTFBox.SetRTF(oldText + "\n\r" + classicInfoText);
                    }
                    
                    e.Handled = true;
                }
            }
            else
            {
                if (e.Key == Key.F2)
                {
                    Global.Buzz.ActiveView = BuzzView.PatternView;
                    e.Handled = true;
                }
                else if (e.Key == Key.F3)
                {
                    Global.Buzz.ActiveView = BuzzView.MachineView;
                    e.Handled = true;
                }
                else if (e.Key == Key.F4)
                {
                    Global.Buzz.ActiveView = BuzzView.SequenceView;
                    e.Handled = true;
                }
                else if (e.Key == Key.F5)
                {
                    Global.Buzz.Playing = true;
                    e.Handled = true;
                }
                else if (e.Key == Key.F6)
                {
                    Global.Buzz.Playing = true;
                    e.Handled = true;
                }
                else if (e.Key == Key.F7)
                {
                    Global.Buzz.Recording = true;
                    e.Handled = true;
                }
                else if (e.Key == Key.F8)
                {
                    Global.Buzz.Recording = false;
                    Global.Buzz.Playing = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.F9)
                {
                    Global.Buzz.ActiveView = BuzzView.WaveTableView;
                    e.Handled = true;
                }
                else if (e.Key == Key.F10)
                {
                    Global.Buzz.ActiveView = BuzzView.SongInfoView;
                    e.Handled = true;
                }
                else if (e.Key == Key.F12)
                {
                    Global.Buzz.AudioDeviceDisabled = !Global.Buzz.AudioDeviceDisabled;
                    e.Handled = true;
                }
            }
        }

        public void FocusBuzzWindow()
        {
            SetForegroundWindow(GetBuzzWindow());
        }

        private void SetWindowZOrder()
        {
            WindowInteropHelper wif = new WindowInteropHelper(InfoWindow);

            IntPtr buzzOldInfo = FindClassicInfoView();
            wif.Owner = buzzOldInfo;
            SetParent(wif.Handle, buzzOldInfo);
        }

        private void UpdateSize()
        {
            RECT wRect;
            IntPtr buzzClassicInfoViewHwnd = FindClassicInfoView();
            if (buzzClassicInfoViewHwnd != IntPtr.Zero)
            {
                GetWindowRect(buzzClassicInfoViewHwnd, out wRect);
                UpdateWindow(wRect, true);
            }
        }

        private void Buzz_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(!buzzLoaded)
            {
                ResourceDictionary rd = InfoMachine.GetBuzzThemeResources();
                if (rd != null)
                    rTFBox.Resources.MergedDictionaries.Add(rd);

                rTFBox.StatusBar.Visibility = Settings.ShowStatusBar == true ? Visibility.Visible : Visibility.Collapsed;
                buzzLoaded = true;
            }
            if (e.PropertyName == "ActiveView")
            {   
                UpdateView();
            }
        }

        public void UpdateView()
        {
            if (Global.Buzz.ActiveView != BuzzGUI.Interfaces.BuzzView.SongInfoView)
            {
                InfoWindow.Visibility = Visibility.Collapsed;
            }
            else if (Global.Buzz.ActiveView == BuzzGUI.Interfaces.BuzzView.SongInfoView)
            {
                FocusBuzzWindow();
                if (!InfoWindow.IsActive)
                {
                    SetWindowZOrder();
                    UpdateSize();
                    DisableBuzzInfo();
                    InfoWindow.Show();
                }

                InfoWindow.Visibility = Visibility.Visible;
                InfoWindow.Focus();
                UpdateSize();
            }
        }

        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            // Listen for operating system messages
            if (Global.Buzz.ActiveView == BuzzGUI.Interfaces.BuzzView.SongInfoView)
            {
                if (m.Msg == WM_PAINT)
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_PRINTCLIENT)
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WS_EX_COMPOSITED)
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_SETFOCUS)
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_NCACTIVATE)
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_DESTROY)
                {
                    Dispose();
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_SIZE)
                {
                    UpdateSize();
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_PAINT || m.Msg == WM_PRINTCLIENT )
                {
                    base.WndProc(ref m);
                }
                else if (m.Msg == WM_SETREDRAW) // Testtest
                {
                    base.WndProc(ref m);
                }
                else
                {
                    //Global.Buzz.DCWriteLine("WM: " + m.Msg.ToString("X"));
                    base.WndProc(ref m);
                }
            }
            else
            {
                base.WndProc(ref m);
            }
        }

        private void UpdateWindow(RECT wRect, bool relative)
        {   
            if (relative)
                UpdateWindow(0, 0, Math.Max(0, wRect.Right - wRect.Left), Math.Max(0, wRect.Bottom - wRect.Top - rTFBox.StatusBar.Height));
            else
                UpdateWindow(wRect.Left, wRect.Top, Math.Max(0, wRect.Right - wRect.Left), Math.Max(0, wRect.Bottom - wRect.Top ));
        }

        private void UpdateWindow(double top, double left, double width, double height)
        {
            if (Global.Buzz.ActiveView != BuzzView.SongInfoView)
            {
                InfoWindow.Visibility = Visibility.Collapsed;
                return;
            }
            
            if (!InfoWindow.IsActive)
            {   
                InfoWindow.Show();
                SetWindowZOrder();
            }

            InfoWindow.Visibility = Visibility.Visible;
            WindowInteropHelper wif = new WindowInteropHelper(InfoWindow);
            SetWindowPos(wif.Handle, IntPtr.Zero, (int)top, (int)left, (int)width, (int)height, 0);
        }

        public static IntPtr FindBuzzMDIClient()
        {
            IntPtr result;
            IntPtr hWndParent = GetBuzzWindow();
            result = EnumAllWindows(hWndParent, "MDIClient").FirstOrDefault();

            return result;
        }

        public static IntPtr FindClassicInfoView()
        {
            IntPtr result;
            IntPtr hWndParent = GetBuzzWindow();
            result = EnumAllWindowCaptions(hWndParent, "Info").FirstOrDefault();
            
            return result;
        }

        public void Dispose()
        {   
            InfoWindow.Close();
            InfoWindow.Content = null;
            this.ReleaseHandle();
            Global.Buzz.PropertyChanged -= Buzz_PropertyChanged;
            Settings.PropertyChanged -= Settings_PropertyChanged;

            IntPtr buzzClassicInfoViewHwnd = FindClassicInfoView();

            if (Integrated)
            {
                Global.Buzz.OpenSong -= Buzz_OpenSong;
                Global.Buzz.SaveSong -= Buzz_SaveSong;
            }

            if (buzzClassicInfoViewHwnd != IntPtr.Zero)
            {
                var childWnds = GetChildWindows(buzzClassicInfoViewHwnd);
                if (childWnds.Count > 0)
                    SendMessage(childWnds[0], WM_SETREDRAW, true, 0); // Enable window updates
            }

            InfoWindow.PreviewKeyDown -= InfoWindow_PreviewKeyDown;
            OneInstanceCreated = false;
        }

        public static IntPtr GetBuzzWindow()
        {
            // Beautiful!
            IntPtr buzzWnd = Global.Buzz.MachineViewHWND;
            buzzWnd = GetParent(buzzWnd);
            buzzWnd = GetParent(buzzWnd);
            buzzWnd = GetParent(buzzWnd);

            return buzzWnd;
        }


        #region Parameters

        public const int WM_DESTROY = 0x2;
        public const int WM_MOVE = 0x3;
        public const int WM_SIZE = 0x5;
        public const int WM_SETFOCUS = 0x7;
        public const int WM_NCACTIVATE = 0x86;
        public const int WM_MDIACTIVATE = 0x222;
        public const int WM_MDIDEACTIVATE = 0x229;
        public const int WM_PAINT = 0xf;
        public const int WM_PRINTCLIENT = 0x318;
        public const int WS_EX_COMPOSITED = 0x2000000;

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public struct PAINTSTRUCT
        {   
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
        }

        #endregion

        #region user32_calls_and_structs
        // Stuff

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = false)]
        internal static extern IntPtr BeginPaint(HandleRef hWnd, [In][Out] ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = false)]
        internal static extern bool EndPaint(HandleRef hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        public static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        static readonly IntPtr HWND_TOP = new IntPtr(0);
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private bool buzzLoaded;

        /// <summary>
        /// Window handles (HWND) used for hWndInsertAfter
        /// </summary>
        public static class HWND
        {
            public static IntPtr
            NoTopMost = new IntPtr(-2),
            TopMost = new IntPtr(-1),
            Top = new IntPtr(0),
            Bottom = new IntPtr(1);
        }

        /// <summary>
        /// SetWindowPos Flags
        /// </summary>
        public struct SetWindowPosFlags
        {
            public static readonly int
            NOSIZE = 0x0001,
            NOMOVE = 0x0002,
            NOZORDER = 0x0004,
            NOREDRAW = 0x0008,
            NOACTIVATE = 0x0010,
            DRAWFRAME = 0x0020,
            FRAMECHANGED = 0x0020,
            SHOWWINDOW = 0x0040,
            HIDEWINDOW = 0x0080,
            NOCOPYBITS = 0x0100,
            NOOWNERZORDER = 0x0200,
            NOREPOSITION = 0x0200,
            NOSENDCHANGING = 0x0400,
            DEFERERASE = 0x2000,
            ASYNCWINDOWPOS = 0x4000;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        public static string GetText(IntPtr hWnd)
        {
            // Allocate correct string length first
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        public static string GetClassicInfoViewText()
        {
            string ret = "";
            IntPtr infoWnd = FindClassicInfoView();

            if (infoWnd != IntPtr.Zero)
            {
                var wnds = GetChildWindows(infoWnd);
                if (wnds.Count == 2)
                    ret = GetText(wnds[1]);
            }

            return ret;
        }

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            list.Add(handle);
            return true;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetWinClass(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;
            StringBuilder classname = new StringBuilder(100);
            IntPtr result = GetClassName(hwnd, classname, classname.Capacity);
            if (result != IntPtr.Zero)
                return classname.ToString();
            return null;
        }

        public static string GetWinCaption(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;
            StringBuilder classname = new StringBuilder(100);
            int result = GetWindowText(hwnd, classname, classname.Capacity);
            if (result > 0)
                return classname.ToString();

            return null;
        }

        public static IEnumerable<IntPtr> EnumAllWindows(IntPtr hwnd, string childClassName)
        {
            List<IntPtr> children = GetChildWindows(hwnd);
            if (children == null)
                yield break;
            foreach (IntPtr child in children)
            {
                if (GetWinClass(child) == childClassName)
                    yield return child;
                foreach (var childchild in EnumAllWindows(child, childClassName))
                    yield return childchild;
            }
        }

        public static IEnumerable<IntPtr> EnumAllWindowCaptions(IntPtr hwnd, string childClassName)
        {
            List<IntPtr> children = GetChildWindows(hwnd);
            if (children == null)
                yield break;
            foreach (IntPtr child in children)
            {
                if (GetWinCaption(child) == childClassName)
                    yield return child;
                foreach (var childchild in EnumAllWindowCaptions(child, childClassName))
                    yield return childchild;
            }
        }

        #endregion
    }
}
