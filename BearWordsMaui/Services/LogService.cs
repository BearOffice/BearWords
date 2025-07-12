using BearOffice.LoggingLib;

namespace BearWordsMaui.Services;

public class LogService: ILogService
{
    public event EventHandler? Changed;
    private readonly string _logFilePath;

    public LogService(ConfigService configService)
    {
        _logFilePath = configService.LogPath;

        Logging.Root.Level = LogLevel.Info;
        Logging.Root.Path = _logFilePath;
        Logging.Root.Format = "(linenum)\t(time:yyyy/MM/d HH:mm:ss)\t(level)\t(name):\t(message)";
        Logging.Broadcast += _ => Changed?.Invoke(this, new EventArgs());
    }

    public string GetLogs()
    {
        if (!Path.Exists(_logFilePath)) return string.Empty;

        using var sw = new StreamReader(_logFilePath);
        return sw.ReadToEnd();
    }

    public void ClearLogs()
    {
        if (!Path.Exists(_logFilePath)) return;

        try
        {
            using var sw = new StreamWriter(_logFilePath);
            sw.Write(string.Empty);
        }
        catch { }
    }

    public void PublishLog(LogLevel level, string message)
    {
        Logging.PublishLog(level, message);
    }
}