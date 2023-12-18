namespace Gamectl;

public class Gamescope
{
    public static string GetGamescopeCommandLine(int fps, string command)
    {
        return Drm.GetCards().Length > 1 && !Configuration.GamescopeOnExternalGpu ? command : $"gamescope -f -F fsr -h 720 -H 1080 -r {fps} -- {command}";

    }
}