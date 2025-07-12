using BearOffice.LoggingLib;

namespace BearWordsMaui.Services;

public interface ILogService
{
    public event EventHandler? Changed;
    public string GetLogs();
    public void ClearLogs();
    public void PublishLog(LogLevel level, string message);
    public void Debug(string message) => PublishLog(LogLevel.Debug, message);
    public void Info(string message) => PublishLog(LogLevel.Info, message);
    public void Warn(string message) => PublishLog(LogLevel.Warn, message);
    public void Error(string message) => PublishLog(LogLevel.Error, message);
    public void Critical(string message) => PublishLog(LogLevel.Crit, message);
}