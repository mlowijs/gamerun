﻿using System.CommandLine;
using System.Diagnostics;
using Gamectl;

var zz = "trala {Fps}";

var fps = 60;
var xx = string.Format(zz, fps);

if (Libc.GetEffectiveUserId() != 0 && !Debugger.IsAttached)
{
    Console.WriteLine("This application must be run suid root");
    return;
}

Configuration.LoadConfiguration();

var eOption = new Option<string?>("-e", "Energy Performance Preference");
var tOption = new Option<int?>("-t", "TDP (in W)");
var gOption = new Option<int?>("-g", "Enable Gamescope scaling with specified FPS");
var mOption = new Option<string?>("-m", "Set display mode with format: widthxheight@refreshRate)");
var dOption = new Option<bool>("-d", "Daemon mode, for autosetting TDP on AC");
var cArgument = new Argument<string[]?>("command", () => null, "The command to run");

var rootCommand = new RootCommand();
rootCommand.AddOption(eOption);
rootCommand.AddOption(tOption);
rootCommand.AddOption(gOption);
rootCommand.AddOption(mOption);
rootCommand.AddArgument(cArgument);

rootCommand.SetHandler((e, t, g, m, d, c) =>
{
    if (d)
    {
        // daemon mode
    }
    
    if (t is not null)
        Ryzenadj.SetTdp(t.Value);
    
    if (e is not null)
        Sysfs.SetEnergyPerformancePreference(e);
    
    // Drop privileges
    Libc.SetEffectiveUserId(Libc.GetUserId());
    
    if (m is not null)
        DisplayMode.SetDisplayMode(m);

    if (c is null || c.Length == 0)
        return;
    
    for (var i = 0; i < c.Length; i++)
    {
        if (c[i].Contains(' '))
            c[i] = $"\"{c[i]}\"";
    }
    
    var commandString = string.Join(' ', c);
    var commandToExecute = g is not null ? Gamescope.GetGamescopeCommandLine(g.Value, commandString) : commandString;

    Process
        .Start(
            "systemd-inhibit",
            $"""--what=idle:sleep --who=gamectl --why="Running game" -- {commandToExecute}""")
        .WaitForExit();

    if (m is not null && Configuration.DefaultMode is not null)
        DisplayMode.SetDisplayMode(Configuration.DefaultMode);
    
    // Regain privileges
    Libc.SetEffectiveUserId(0);
    
    if (t is not null && Configuration.DefaultTdp is not null)
        Ryzenadj.SetTdp(Configuration.DefaultTdp.Value);
    
    if (e is not null && Configuration.DefaultEpp is not null)
        Sysfs.SetEnergyPerformancePreference(Configuration.DefaultEpp);
}, eOption, tOption, gOption, mOption, dOption, cArgument);

await rootCommand.InvokeAsync(args);