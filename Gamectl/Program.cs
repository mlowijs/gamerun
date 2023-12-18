﻿using System.CommandLine;
using System.Diagnostics;
using Gamectl;

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
var cArgument = new Argument<string[]?>("command", () => null, "The command to run");

var rootCommand = new RootCommand();
rootCommand.AddOption(eOption);
rootCommand.AddOption(tOption);
rootCommand.AddOption(gOption);
rootCommand.AddOption(mOption);
rootCommand.AddArgument(cArgument);

rootCommand.SetHandler((e, t, g, m, c) =>
{
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

    // Regain privileges
    Libc.SetEffectiveUserId(0);
    
    if (t is not null)
        Ryzenadj.SetTdp(Configuration.DefaultTdp);
    
    if (e is not null)
        Sysfs.SetEnergyPerformancePreference(Configuration.DefaultEpp);
}, eOption, tOption, gOption, mOption, cArgument);

await rootCommand.InvokeAsync(args);