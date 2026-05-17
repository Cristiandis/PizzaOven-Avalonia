using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Path = System.IO.Path;

namespace PizzaOven;

public class PLUSTutorial
{
    public static async Task<bool> WaitUntilTutorialDownloaded(int checkDelayMs = 16)
    {
        var modPath = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}Ronnie Oven Mod";
        var jsonFile = Path.Combine(modPath, "mod.json");

        while (true)
        {
            if (Directory.Exists(modPath) && File.Exists(jsonFile))
                try
                {
                    var jsonText = await File.ReadAllTextAsync(jsonFile);
                    using var doc = JsonDocument.Parse(jsonText);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("homepage", out var homepage))
                        return homepage.ValueKind == JsonValueKind.Null ||
                               string.IsNullOrWhiteSpace(homepage.GetString());
                }
                catch
                {
                }

            await Task.Delay(checkDelayMs);
        }
    }

    public static string TutorialModPath()
    {
        var currentModDirectory = $@"{Global.assemblyLocation}{Global.s}Mods";

        foreach (var mod in Directory.GetDirectories(currentModDirectory))
        {
            var jsonFile = Path.Combine(mod, "mod.json");

            if (!File.Exists(jsonFile))
                continue;

            try
            {
                var jsonText = File.ReadAllText(jsonFile);
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                var preview = root.TryGetProperty("preview", out var p) ? p.GetString() : null;
                var avi = root.TryGetProperty("avi", out var a) ? a.GetString() : null;
                var upic = root.TryGetProperty("upic", out var u) ? u.GetString() : null;

                if (preview == "avares://PizzaOven/TutorialMod/mod.png" ||
                    avi == "avares://PizzaOven/TutorialMod/profile.png" ||
                    upic == "avares://PizzaOven/TutorialMod/upic.gif")
                    return mod;
            }
            catch
            {
            }
        }

        return "";
    }

    public static bool IsTutorialDownloaded()
    {
        var modPath = $"{Global.assemblyLocation}{Global.s}Mods{Global.s}Ronnie Oven Mod";
        var jsonFile = Path.Combine(modPath, "mod.json");

        if (Directory.Exists(modPath) && File.Exists(jsonFile))
            try
            {
                var jsonText = File.ReadAllText(jsonFile);
                using var doc = JsonDocument.Parse(jsonText);
                var root = doc.RootElement;

                if (root.TryGetProperty("homepage", out var homepage))
                    return homepage.ValueKind == JsonValueKind.Null ||
                           string.IsNullOrWhiteSpace(homepage.GetString());
            }
            catch
            {
            }

        return false;
    }

    public static async Task WaitForRonnieModClick()
    {
        while (true)
        {
            var first = Global.config.ModList.FirstOrDefault(x => x.enabled);

            if (first != null && first.name == "Ronnie Oven Mod")
                return;

            await Task.Delay(100);
        }
    }

    public static async Task RunTutorial(MainWindow window)
    {
        var tutorialskip = false;
        if (File.Exists($@"{Global.appdata}{Global.s}PizzaTower_GM2{Global.s}RonnieTutorial.ini"))
            File.Delete($@"{Global.appdata}{Global.s}PizzaTower_GM2{Global.s}RonnieTutorial.ini");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            window.ModManager.IsSelected = true;
            window.ModBrowser.IsEnabled = false;
            window.SettingsTab.IsEnabled = false;
            window.PatchNotes.IsEnabled = false;
        });

        PLUSMUSIC.PlayJingle();

        window.PatchNotes.IsEnabled = false;


        window.tutorialanimator = new PLUSRonnieAnimate();
        window.tutorialanimator.Initialize(window, window.Bounds.Width / 6, -100, 1.5);
        window.ModBrowser.IsEnabled = false;
        window.SettingsTab.IsEnabled = false;

        window.SizeChanged += (s, e) =>
        {
            window.tutorialanimator._overlayCanvas.Width = window.Bounds.Width;
            window.tutorialanimator._overlayCanvas.Height = window.Bounds.Height;
        };


        window.tutorialanimator.GlideTo(window.Bounds.Width / 6, 250);

        await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() >= 250);
        await Task.Delay(2000);

        window.tutorialanimator.SetExpression("normal");
        PLUSMUSIC.Play_TutorialMusic();
        var curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25, "Hi!! Hello!! Hi!![click to proceed]");

        await window.tutorialanimator.WaitForClickOnImageAsync();

        window.tutorialanimator.SetExpression("dumb");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25, "I'm Ronnie, Ronnie the Oven!");
        RonnieVariables.RonnieModSkip = IsTutorialDownloaded();
        if (!RonnieVariables.RonnieModSkip)
        {
            PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
            PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("happy2");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "Welcome to PizzaOven+ (Plus)");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("inspect");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "Nice to meet you, Random Pizza Tower fan! or so I think you are");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("happy2");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "I will be your personnel and annoying guide to understand this wacky and wonderful tool!\r\n");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("normal");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "What's this for you ask? You are using a Pizza Oven extension! It's basically the same thing but with more stuff.");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("normal");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "Allow me to give you a bunch of carefree tips to make your life easier! Consider me as a useful buddy.\r\n");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("sad");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "You can choose to hate me but I don't really care because I don't make much friends anyway");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("happy");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "but in case you don't, GREAT! Let me show you around...");

            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("sad");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "If you'd like to skip the tutorial the button is there... heheh... or you can click me and listen to me");

            await window.tutorialanimator.MakeSkipButtonAsync(window.tutorialanimator._overlayCanvas,
                () => { tutorialskip = true; });


            window.tutorialanimator._overlayCanvas.IsHitTestVisible = false;


            if (tutorialskip)
            {
                PLUSMUSIC.FadeOutTutorialMusic();
                if (PLUSSavesystem.read_ini("Tutorial", "Replay", "false") == "true")
                    PLUSSavesystem.write_ini("Tutorial", "ReplaySkip", "true"); // now your doing it on purpose
                else
                    PLUSSavesystem.write_ini("Tutorial", "Skip", "true"); // (?) Ronnie will remember that you
                PLUSSavesystem.write_ini("Tutorial", "Finished", "true");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Oh.. I guess I'll leave sigh");
                window.tutorialanimator.GlideTo(window.Bounds.Width / 6, 100, 1);
                await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() <= 100);

                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Are you sure?");

                await PLUSWait.WaitSeconds(3);

                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "No no you're right bye...");
                window.tutorialanimator.GlideTo(window.tutorialanimator.GetX(), -100, 1);

                await PLUSWait.WaitUntil(() => window.tutorialanimator.GetY() <= -100);

                TutorialState();
            }
            else
            {
                window.tutorialanimator.SetExpression("happy2");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "REALLY WOWOWOW");

                await window.tutorialanimator.WaitForClickOnImageAsync();

                window.tutorialanimator.SetExpression("happy2");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "THIS MAKES ME WANNA DANCE");
                await window.tutorialanimator.DanceAsync(10, 150);

                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Let's start off by looking for a mod to download.");

                var relativePoint = window.ModBrowser.TranslatePoint(new Point(0, 0), window) ?? new Point(0, 0);

                var tabX = relativePoint.X + 100;
                var tabY = relativePoint.Y;

                window.tutorialanimator.SetExpression("pointerup");
                window.tutorialanimator.MoveTo(tabX - 50, tabY + 50);
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25,
                    "Click on browse mods to see what kind of goofy shenaenaes fellow people have been up to!");


                window.ModBrowser.IsEnabled = true;

                await PLUSWait.WaitUntil(() => window.ModBrowser.IsSelected);

                window.ModManager.IsEnabled = false;

                window.tutorialanimator.MoveTo(window.Bounds.Width / 2, 200);
                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Hmmm let's see...");

                await window.tutorialanimator.WaitForClickOnImageAsync();

                window.tutorialanimator.SetExpression("happy");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "OH LOOK THERE I AM!");

                await window.tutorialanimator.WaitForClickOnImageAsync();

                window.tutorialanimator.SetExpression("sad");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25,
                    "sniff... How heartwarming... someone made a mod for me! WOW!");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25,
                    "If you ever want a mod added to your collection, simply download it!");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("inspect");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Press on more info to check out the mod description!");

                await window.tutorialanimator.WaitForClickOnImageAsync();
                window.tutorialanimator.SetExpression("normal");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Download this for me real quick, it really won't take long!");

                RonnieVariables.AllowDownloadMod = true;

                await WaitUntilTutorialDownloaded();

                window.tutorialanimator.SetExpression("happy2");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Yippee!!");

                await window.tutorialanimator.WaitForClickOnImageAsync();

                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25,
                    "OKAY now that you have a cool mod to your collection, give it a lil swirl!");

                await window.tutorialanimator.WaitForClickOnImageAsync();

                var relativePoint_2 = window.ModManager.TranslatePoint(new Point(0, 0), window) ?? new Point(0, 0);

                var tabX_2 = relativePoint_2.X + 100;
                var tabY_2 = relativePoint_2.Y;

                window.tutorialanimator.SetExpression("pointerup");
                window.tutorialanimator.MoveTo(tabX_2 - 50, tabY_2 + 50);
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Now Let's now click back");


                window.ModManager.IsEnabled = true;

                await PLUSWait.WaitUntil(() => window.ModManager.IsSelected);

                window.tutorialanimator.MoveTo(window.Bounds.Width / 6, 250);
                window.ModBrowser.IsEnabled = false;
            }
        }
        else
        {
            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("thinking");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "Oh it seems like you have my mod installed");
            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("pointerup");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "We can get to launching!!");

            if (PLUSSavesystem.read_ini("Tutorial", "BrokenModSkip", "false") == "true")
            {
                await PLUSWait.WaitSeconds(3);
                window.tutorialanimator.SetExpression("thinking");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                if (PLUSSavesystem.read_ini("Tutorial", "SettingsSection", "false") == "true")
                {
                    window.tutorialanimator.SetExpression("sad");
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Wait I NOW REMEMBER THIS MOD WAS BROKEN. we should move to settings....");
                    RonnieVariables.BrokenModSkip = true;
                    await window.tutorialanimator.WaitForClickOnImageAsync();
                }
                else
                {
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Oh the mod might be broken uhm you can skip if you want lol to settings section or you can click me to try again");

                    await PLUSWait.WaitSeconds(1);
                    await window.tutorialanimator.MakeSkipButtonAsync(window.tutorialanimator._overlayCanvas,
                        () => { RonnieVariables.BrokenModSkip = true; });

                    window.tutorialanimator._overlayCanvas.IsHitTestVisible = false;
                }
            }
            else
            {
                await window.tutorialanimator.WaitForClickOnImageAsync();
            }
        }

        if (!RonnieVariables.BrokenModSkip)
        {
            RonnieVariables.SetupAllow = true;
            window.tutorialanimator.SetExpression("thinking");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25,
                "Before we do this though, we need to make sure we have your files in check! Click on setup just to make sure...");


            var setupTimer = new DispatcherTimer();
            setupTimer.Interval = TimeSpan.FromMilliseconds(16);

            setupTimer.Tick += async (s, e) =>
            {
                if (RonnieVariables.SetupSucessful == 1)
                {
                    setupTimer.Stop();
                    return;
                }

                if (RonnieVariables.SetupSucessful == 0)
                {
                    await PLUSWait.WaitSeconds(3);
                    if (RonnieVariables.SetupSucessful == 0)
                    {
                        window.tutorialanimator.SetExpression("sad");
                        window.tutorialanimator.DestroyTextbox(curtextbox);

                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                            window.tutorialanimator.GetY() + 25,
                            "Looks like I wasn't able to find your Pizza Tower folder.. Could you click on it for me, pretty pleaaaase?");

                        RonnieVariables.SetupSucessful = -1;
                    }
                }
            };


            setupTimer.Start();

            await PLUSWait.WaitUntil(() => RonnieVariables.SetupSucessful == 1);

            setupTimer.Stop();

            window.tutorialanimator.SetExpression("happy");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "Alright! Now we're all fired up! First select the mod...");

            await WaitForRonnieModClick();

            window.tutorialanimator.SetExpression("normal");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "And then launch it!");

            if (!IsTutorialDownloaded())
            {
                await PLUSWait.WaitSeconds(3);
                window.tutorialanimator.SetExpression("sad");
                window.tutorialanimator.DestroyTextbox(curtextbox);
                curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                    window.tutorialanimator.GetY() + 25, "Wait... You were not meant to delete the mod...");
                await PLUSWait.WaitSeconds(3);
                TutorialState("false");
            }

            RonnieVariables.LauncherAllow = true;

            var path = Path.Combine(Global.appdata, "PizzaTower_GM2", "RonnieTutorial.ini");

            var exetimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };

            exetimer.Tick += async (s, e) =>
            {
                var processName = Path.GetFileNameWithoutExtension(Global.config.Launcher);

                if (RonnieVariables.FailedPatch)
                {
                    RonnieVariables.FailedPatch = false;
                    RonnieVariables.LauncherAllow = false;
                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Oh... that's weird... Uhmmmm why is it... not... working...");
                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("sad");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Oh god im such a useless failure aren't I... I can't do anything right!");
                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("thinking");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Hmm... oh I know! you should try messing around your steam settings! Trust me, it's super simple...");
                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "Then, going into your steam, go to Pizza Tower and click on properties.");
                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "You're gonna want to click on Installed files and then verify integrity Once you've done that and it says no files are missing");
                    await window.tutorialanimator.WaitForClickOnImageAsync();

                    window.tutorialanimator.SetExpression("happy");
                    window.tutorialanimator.DestroyTextbox(curtextbox);
                    curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                        window.tutorialanimator.GetY() + 25,
                        "try launching the mod again after that. make sure this will overwrite your existing modifed pt files");
                    RonnieVariables.LauncherAllow = true;
                }

                if (File.Exists(path))
                {
                    exetimer.Stop();
                }
                else
                {
                    PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "true");
                    RonnieVariables.ModLaunchAmount += 1;
                    await PLUSWait.WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length > 0);
                    PLUSMUSIC.SetTutorialMusicPaused(true);
                    await PLUSWait.WaitUntil(() =>
                        Process.GetProcessesByName(processName).Length == 0);
                    PLUSMUSIC.SetTutorialMusicPaused(false);
                    if (!File.Exists(path) && !RonnieVariables.FinishedLaunch)
                    {
                        window.tutorialanimator.SetExpression("sad");
                        window.tutorialanimator.DestroyTextbox(curtextbox);
                        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                            window.tutorialanimator.GetY() + 25, "Atleast finish the intro...");
                        await PLUSWait.WaitSeconds(3);
                        if (RonnieVariables.ModLaunchAmount > 1)
                        {
                            window.tutorialanimator.SetExpression("thinking");
                            window.tutorialanimator.DestroyTextbox(curtextbox);
                            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                                window.tutorialanimator.GetY() + 25,
                                "Or I mean if the mod isn't working you can close PizzaOven+ and I will offer a skip");
                        }

                        if (Process.GetProcessesByName(processName).Length == 0)
                        {
                            var ps = new ProcessStartInfo(Global.config.Launcher)
                            {
                                WorkingDirectory = Path.GetDirectoryName(Global.config.Launcher),
                                UseShellExecute = true,
                                Verb = "open"
                            };
                            Process.Start(ps);
                        }

                        exetimer.Stop();
                        exetimer.Start();
                    }
                }
            };


            exetimer.Start();

            await PLUSWait.WaitUntil(() => File.Exists(path));
            RonnieVariables.FinishedLaunch = true;
            window.ConfigButton.IsEnabled = false;
            window.LaunchButton.IsEnabled = false;
            exetimer.Stop();
            PLUSMUSIC.SetTutorialMusicPaused(false);
            File.Delete(path);
            window.tutorialanimator.SetExpression("sad");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "true");
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "someone is trolling me that mod SUCKED...");


            await window.tutorialanimator.WaitForClickOnImageAsync();
            window.tutorialanimator.SetExpression("dumb");
            window.tutorialanimator.DestroyTextbox(curtextbox);
            curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
                window.tutorialanimator.GetY() + 25, "ANYWAYS I think that was pretty helpful, don't you think?");
        }

        var relativePoint_3 = window.SettingsTab.TranslatePoint(new Point(0, 0), window) ?? new Point(0, 0);

        var tabX_3 = relativePoint_3.X + 100;
        var tabY_3 = relativePoint_3.Y;

        window.tutorialanimator.SetExpression("pointerup");
        window.tutorialanimator.MoveTo(tabX_3 - 50, tabY_3 + 50);

        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25, "Let's go to settings!");
        window.SettingsTab.IsEnabled = true;

        await PLUSWait.WaitUntil(() => window.SettingsTab.IsSelected);

        PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "true");
        window.SettingsTab.IsEnabled = false;
        window.ModManager.IsEnabled = false;
        window.tutorialanimator.MoveTo(window.Bounds.Width / 8, window.Bounds.Height / 2);
        window.tutorialanimator.SetExpression("normal");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25, "This where you can configure stuff");

        await window.tutorialanimator.WaitForClickOnImageAsync();
        window.tutorialanimator.SetExpression("happy");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        RonnieVariables.RonnieExplainSettings = 0;
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25,
            "Feel free to look around to see what I can cater pookie, you may also click on me when you're done or hover over settings for a brief overview");

        await PLUSWait.WaitUntil(() => RonnieVariables.RonnieExplainSettings == 1);

        window.tutorialanimator.DestroyTextbox(curtextbox);

        await window.tutorialanimator.WaitForClickOnImageAsync();
        window.tutorialanimator.MoveTo(window.Bounds.Width / 6, 250);
        window.ModManager.IsEnabled = true;
        window.SettingsTab.IsEnabled = false;
        window.ModManager.IsSelected = true;

        window.tutorialanimator.SetExpression("thinking");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25,
            "And that's about all the wacky things you can do here, really! Not so wacky now, huh?");

        await window.tutorialanimator.WaitForClickOnImageAsync();
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25,
            "If you want me to improve on this tool, then make sure to spell it out for me on feedback in the Links section, because believe it or not, I can in fact read.");

        await window.tutorialanimator.WaitForClickOnImageAsync();
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25,
            "I won't take too much of your time, so how about you embark on your own journey!");

        await window.tutorialanimator.WaitForClickOnImageAsync();
        window.tutorialanimator.SetExpression("happy2");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        curtextbox = window.tutorialanimator.MakeTextbox(window.tutorialanimator.GetX() + 110,
            window.tutorialanimator.GetY() + 25,
            "I am still saying right here by your side, probably toiling away for you for the rest of eternity. YAY!!");

        PLUSSavesystem.write_ini("Tutorial", "Finished", "true");
        window.tutorialanimator.DestroyTextbox(curtextbox);
        await window.tutorialanimator.DanceAsync(3, 60);

        TutorialState();
    }

    public static async Task RunIntro(MainWindow window)
    {
        window.SettingsTab.IsEnabled = false;
        var curtextbox =
            window.introanimator.MakeTextbox(window.introanimator.GetX() + 110, window.introanimator.GetY() + 25, "");
        if (!RonnieVariables.ModDeleted)
        {
            window.introanimator.SetExpression("happy2");
            window.introanimator.DestroyTextbox(curtextbox);
            curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110,
                window.introanimator.GetY() + 25,
                "I saw that broken mod's thumbnail and deleted the mod for you! You are welcome");
            await PLUSWait.WaitSeconds(5);
        }

        if (RonnieVariables.KeptMod)
        {
            window.introanimator.SetExpression("inspect");
            window.introanimator.DestroyTextbox(curtextbox);
            curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110,
                window.introanimator.GetY() + 25, "I also know you kept the mod and played it after the tutorial");
            await PLUSWait.WaitSeconds(5);
        }

        window.introanimator.DestroyTextbox(curtextbox);
        window.introanimator.SetExpression("normal");
        curtextbox = window.introanimator.MakeTextbox(window.introanimator.GetX() + 110,
            window.introanimator.GetY() + 25, "Safe Travels");
        var _followtimer = new DispatcherTimer();
        _followtimer.Interval = TimeSpan.FromSeconds(0.01);
        _followtimer.Tick += (s, e) =>
        {
            Canvas.SetLeft(window.introanimator.GetTextbox(curtextbox), window.introanimator.GetX() + 110);
            Canvas.SetTop(window.introanimator.GetTextbox(curtextbox), window.introanimator.GetY() + 25);
        };
        _followtimer.Start();
        window.SettingsTab.IsEnabled = true;

        window.introanimator.GlideTo(window.Bounds.Width / 2, -300);
        await PLUSWait.WaitUntil(() => window.introanimator.GetY() <= 0);
        _followtimer.Stop();
        window.introanimator.DestroyTextbox(curtextbox);
        await PLUSWait.WaitUntil(() => window.introanimator.GetY() <= -300);

        window.introanimator.Destroy();
    }

    //1.0.7 [future update] - GMLoader explaination
    public static async Task GMLoaderExplaination(MainWindow window)
    {
        window.gmloaderanimator = new PLUSRonnieAnimate();
        window.gmloaderanimator.StepEvent = () =>
        {
            window.gmloaderanimator.SetVisible(window.ModManager.IsEnabled && !PLUSRonnieAnimate.GetActive().Any());
            MessageBoxManager.GetMessageBoxStandard("Notice", window.ModManager.IsEnabled.ToString())
                .ShowAsync();
        };
        window.gmloaderanimator.Initialize(window, window.Bounds.Width / 6, -100, 1.5);
        window.gmloaderanimator.SetExpression("thinking");
        var curtextbox = window.gmloaderanimator.MakeTextbox(window.gmloaderanimator.GetX() + 110,
            window.gmloaderanimator.GetY() + 25, "Oh... I see you have GMLoader installed...");
        await window.gmloaderanimator.WaitForClickOnImageAsync();
        curtextbox = window.gmloaderanimator.MakeTextbox(window.gmloaderanimator.GetX() + 110,
            window.gmloaderanimator.GetY() + 25, "You can enable them by turning on \"GMLoader\"");
    }

    public static async Task ReplayTutorial(MainWindow window)
    {
        if (window.replayanimator != null || Global.ronnietutorial) return;
        PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
        PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
        var box = MessageBoxManager.GetMessageBoxStandard(
            "Confirm Replay",
            "Do you want to replay the tutorial?",
            ButtonEnum.YesNo,
            Icon.Question
        );
        var result = await box.ShowWindowDialogAsync(window);
        if (result == ButtonResult.Yes)
        {
            PLUSSavesystem.write_ini("Tutorial", "Replay", "true");
            PLUSSavesystem.write_ini("Tutorial", "ForcedReplay", "false");
            PLUSSavesystem.write_ini("Tutorial", "Finished", "false");
            window.replayanimator = new PLUSRonnieAnimate();
            window.replayanimator.Initialize(window, window.Bounds.Width / 6, -100, 1.5);
            window.replayanimator.SetExpression("happy");

            window.replayanimator.GlideTo(window.Bounds.Width / 6, 250, 40);

            await PLUSWait.WaitUntil(() => window.replayanimator.GetY() >= 250);
            var curtextbox = window.replayanimator.MakeTextbox(window.replayanimator.GetX() + 110,
                window.replayanimator.GetY() + 25, "TAKE IT FROM THE TOP");
            await PLUSWait.WaitSeconds(5);
            TutorialState("false");
        }
        else
        {
            RonnieVariables.DeclineReplay += 1;
            window.replayanimator = new PLUSRonnieAnimate();
            window.replayanimator.Initialize(window, window.Bounds.Width / 6, -100, 1.5);
            var curtextbox = window.replayanimator.MakeTextbox(window.replayanimator.GetX() + 110,
                window.replayanimator.GetY() + 25, "");
            window.replayanimator.DestroyTextbox(curtextbox);
            window.replayanimator.GlideTo(window.Bounds.Width / 6, 250, 40);
            window.replayanimator.SetExpression("sad");
            await PLUSWait.WaitUntil(() => window.replayanimator.GetY() >= 250);
            if (RonnieVariables.DeclineReplay == 3)
            {
                window.replayanimator.DestroyTextbox(curtextbox);
                curtextbox = window.replayanimator.MakeTextbox(window.replayanimator.GetX() + 110,
                    window.replayanimator.GetY() + 25, "Stop it! or else");
                await PLUSWait.WaitSeconds(3);
                window.replayanimator.DestroyTextbox(curtextbox);
            }
            else if (RonnieVariables.DeclineReplay > 3)
            {
                PLUSSavesystem.write_ini("Tutorial", "Replay", "false");
                PLUSSavesystem.write_ini("Tutorial", "ForcedReplay", "true");
                PLUSSavesystem.write_ini("Tutorial", "Finished", "false");
                window.replayanimator.DestroyTextbox(curtextbox);
                curtextbox = window.replayanimator.MakeTextbox(window.replayanimator.GetX() + 110,
                    window.replayanimator.GetY() + 25, "You asked for this");
                await PLUSWait.WaitSeconds(3);
                window.replayanimator.DestroyTextbox(curtextbox);
                TutorialState("false");
            }

            window.replayanimator.GlideTo(window.Bounds.Width / 6, -250, 40);
            await PLUSWait.WaitUntil(() => window.replayanimator.GetY() <= -250);
            window.replayanimator.Destroy();
            window.replayanimator = null;
        }
    }

    public static void TutorialState(string finished = "true")
    {
        PLUSSavesystem.write_ini("Tutorial", "Finished", finished);
        PLUSSavesystem.write_ini("Tutorial", "BrokenModSkip", "false");
        PLUSSavesystem.write_ini("Tutorial", "SettingsSection", "false");
        var exePath = $"{AppDomain.CurrentDomain.BaseDirectory}{Global.s}{AppDomain.CurrentDomain.FriendlyName}";
        Process.Start(exePath);
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.Shutdown();
    }

    public static class RonnieVariables
    {
        public static bool BrokenModSkip;
        public static int ModLaunchAmount;
        public static bool LauncherAllow;
        public static bool SetupAllow;
        public static bool AllowDownloadMod;
        public static bool RonnieModSkip;
        public static int SetupSucessful = -1;
        public static bool FinishedLaunch;
        public static bool FailedPatch;
        public static bool KeptMod = false;
        public static bool ModDeleted = false;
        public static int DeclineReplay;
        public static int RonnieExplainSettings = -1;
        public static int publictextbox = 0;
    }
}