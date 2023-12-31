namespace Gamerun;

public static class Gamescope
{
    public static string GetGamescopeCommandLine(string command)
    {
        if (Sysfs.GetDrmCards().Length > 1 && !Configuration.Values.GamescopeOnExternalGpu)
            return command;
        
        return $"gamescope {Configuration.Values.GamescopeArguments ?? ""} -- {command}";
    }
}