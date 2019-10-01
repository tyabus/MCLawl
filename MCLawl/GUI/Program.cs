using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;

namespace MCLawl.Gui
{
    public static class Program
    {
        public static bool usingConsole = false;

        [DllImport("kernel32")]
        public static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        public static void GlobalExHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Server.ErrorLog(ex);
            Thread.Sleep(500);

            if (!Server.restartOnError)
                Program.restartMe();
            else
                Program.restartMe(false);
        }

        public static void ThreadExHandler(object sender, ThreadExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            Server.ErrorLog(ex);
            Thread.Sleep(500);

            if (!Server.restartOnError)
                Program.restartMe();
            else
                Program.restartMe(false);
        }

        public static void Main(string[] args)
        {
            if (Process.GetProcessesByName("MCLawl").Length != 1)
            {
                foreach (Process pr in Process.GetProcessesByName("MCLawl"))
                {
                    if (pr.MainModule.BaseAddress == Process.GetCurrentProcess().MainModule.BaseAddress)
                        if (pr.Id != Process.GetCurrentProcess().Id)
                            pr.Kill();
                }
            }

            PidgeonLogger.Init();
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(Program.GlobalExHandler);
            Application.ThreadException += new ThreadExceptionEventHandler(Program.ThreadExHandler);
            bool skip = false;
        remake:
            try
            {
                if (!File.Exists("viewmode.cfg") || skip)
                {
                    StreamWriter SW = new StreamWriter(File.Create("viewmode.cfg"));
                    SW.WriteLine("#This file controls how the console window is shown to the server host");
                    SW.WriteLine("#cli:             True or False (Determines whether a CLI interface is used) (Set True if on Mono)");
                    SW.WriteLine("#high-quality:    False only!!!");
                    SW.WriteLine();
                    SW.WriteLine("high-quality = false");
                    SW.WriteLine("cli = false");
                    SW.Flush();
                    SW.Close();
                    SW.Dispose();
                }

                if (File.ReadAllText("viewmode.cfg") == "") { skip = true; goto remake; }

                string[] foundView = File.ReadAllLines("viewmode.cfg");
                if (foundView[0][0] != '#') { skip = true; goto remake; }

                if (foundView[4].Split(' ')[2].ToLower() == "true")
                {
                    Server s = new Server();
                    s.OnLog += Console.WriteLine;
                    s.OnCommand += Console.WriteLine;
                    s.OnSystem += Console.WriteLine;
                    s.Start();

                    Console.Title = Server.name + " MCLawl Version: " + Server.Version;
                    usingConsole = true;
                    handleComm(Console.ReadLine());
                }
                else
                {

                    IntPtr hConsole = GetConsoleWindow();
                    if (IntPtr.Zero != hConsole)
                    {
                        ShowWindow(hConsole, 0);
                    }
                    if (foundView[5].Split(' ')[2].ToLower() == "true")
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                    }

                    Application.Run(new MCLawl.Gui.Window());
                }
            }
            catch (Exception e) { Server.ErrorLog(e); return; }
        }

        public static void handleComm(string s)
        {
            string sentCmd = "", sentMsg = "";

            if (s.IndexOf(' ') != -1)
            {
                sentCmd = s.Split(' ')[0];
                sentMsg = s.Substring(s.IndexOf(' ') + 1);
            }
            else if (s != "")
            {
                sentCmd = s;
            }
            else
            {
                goto talk;
            }

            try
            {
                Command cmd = Command.all.Find(sentCmd);
                if (cmd != null)
                {
                    cmd.Use(null, sentMsg);
                    Console.WriteLine("CONSOLE: USED /" + sentCmd + " " + sentMsg);
                    handleComm(Console.ReadLine());
                    return;
                }
            }
            catch (Exception e)
            {
                Server.ErrorLog(e);
                Console.WriteLine("CONSOLE: Failed command.");
                handleComm(Console.ReadLine());
                return;
            }

        talk: handleComm("say " + MCLawl.Group.findPerm(LevelPermission.Admin).color + "Console: &f" + s);
            handleComm(Console.ReadLine());
        }

        static public void ExitProgram(Boolean AutoRestart)
        {
            Thread exitThread;
            Server.Exit();

            exitThread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    if (MCLawl.Gui.Window.thisWindow.notifyIcon1 != null)
                    {
                        MCLawl.Gui.Window.thisWindow.notifyIcon1.Icon = null;
                        MCLawl.Gui.Window.thisWindow.notifyIcon1.Visible = false;
                    }
                }
                catch { }

                try
                {
                    saveAll();

                    if (AutoRestart == true) restartMe();
                    else Server.process.Kill();
                }
                catch
                {
                    Server.process.Kill();
                }
            })); exitThread.Start();
        }

        static public void restartMe(bool fullRestart = true)
        {
            Thread restartThread = new Thread(new ThreadStart(delegate
            {
                saveAll();

                Server.shuttingDown = true;
                try
                {
                    if (MCLawl.Gui.Window.thisWindow.notifyIcon1 != null)
                    {
                        MCLawl.Gui.Window.thisWindow.notifyIcon1.Icon = null;
                        MCLawl.Gui.Window.thisWindow.notifyIcon1.Visible = false;
                    }
                }
                catch { }

                if (Server.listen != null) Server.listen.Close();
                if (!Server.mono || fullRestart)
                {
                    Application.Restart();
                    Server.process.Kill();
                }
                else
                {
                    Server.s.Start();
                }
            }));
            restartThread.Start();
        }
        static public void saveAll()
        {
            try
            {
                List<Player> kickList = new List<Player>();
                kickList.AddRange(Player.players);
                foreach (Player p in kickList) { p.Kick("Server restarted! Rejoin!"); }
            }
            catch (Exception exc) { Server.ErrorLog(exc); }

            try
            {
                string level = null;
                foreach (Level l in Server.levels)
                {
                    level = level + l.name + "=" + l.physics + System.Environment.NewLine;
                    l.Save();
                    l.saveChanges();
                }

                File.WriteAllText("text/autoload.txt", level);
            }
            catch (Exception exc) { Server.ErrorLog(exc); }
        }
    }
}