using System.Collections.ObjectModel;
using Serilog;

namespace Dpz.Core.WebMore.App.Pages.Tools;

public partial class LogViewerPage : ContentPage
{
    private readonly ObservableCollection<LogFileItem> _files = [];
    private readonly string _logDirectory;

    public LogViewerPage()
    {
        InitializeComponent();
        _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
        fileList.ItemsSource = _files;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            await LoadFilesAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载日志文件失败");
            await DisplayAlertAsync("日志", $"加载日志文件失败：{ex.Message}", "确定");
        }
    }

    private async void OnRefresh(object sender, EventArgs e)
    {
        await LoadFilesAsync();
    }

    private async void OnClear(object sender, EventArgs e)
    {
        var confirm = await DisplayAlertAsync("清空日志", "确定要删除所有日志文件吗？", "删除", "取消");
        if (!confirm)
        {
            return;
        }

        foreach (var file in Directory.GetFiles(_logDirectory, "*.log"))
        {
            File.Delete(file);
        }

        _files.Clear();
        logContent.Text = string.Empty;
        selectedFileLabel.Text = "未选择日志文件";
    }

    private async void OnFileSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not LogFileItem item)
        {
            return;
        }

        selectedFileLabel.Text = item.Name;
        logContent.Text = "加载中...";

        var text = await ReadLogFileAsync(item.Path);
        logContent.Text = text;
        fileList.SelectedItem = null;
    }

    private async Task LoadFilesAsync()
    {
        _files.Clear();
        if (!Directory.Exists(_logDirectory))
        {
            Directory.CreateDirectory(_logDirectory);
        }

        var files = Directory.GetFiles(_logDirectory, "*.log")
            .Select(path => new FileInfo(path))
            .OrderByDescending(f => f.LastWriteTime)
            .Select(f => new LogFileItem(f.Name, f.FullName, f.LastWriteTime))
            .ToList();

        foreach (var file in files)
        {
            _files.Add(file);
        }

        if (_files.Count == 0)
        {
            selectedFileLabel.Text = "暂无日志文件";
            logContent.Text = string.Empty;
        }
        else
        {
            await LoadFirstAsync();
        }
    }

    private async Task LoadFirstAsync()
    {
        var first = _files.FirstOrDefault();
        if (first == null)
        {
            return;
        }
        selectedFileLabel.Text = first.Name;
        logContent.Text = await ReadLogFileAsync(first.Path);
    }

    private static async Task<string> ReadLogFileAsync(string path)
    {
        const int maxChars = 200_000;
        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        var text = await reader.ReadToEndAsync();

        if (text.Length <= maxChars)
        {
            return text;
        }

        var tail = text[^maxChars..];
        return $"...（已截断，仅显示最后 {maxChars} 字符）\n{tail}";
    }

    private sealed record LogFileItem(string Name, string Path, DateTime LastWrite)
    {
        public string LastWriteText => LastWrite.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
