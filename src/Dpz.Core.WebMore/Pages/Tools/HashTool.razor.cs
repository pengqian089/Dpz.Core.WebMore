using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages.Tools;

public partial class HashTool : ComponentBase
{
    [Inject]
    private IJSRuntime JSRuntime { get; set; } = null!;

    private enum HashAlgorithmType
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512,
    }

    private enum HashSource
    {
        Text,
        File,
    }

    private readonly HashAlgorithmType[] _algorithms = Enum.GetValues<HashAlgorithmType>();
    private HashAlgorithmType _algorithm = HashAlgorithmType.SHA256;

    private string _inputText = string.Empty;
    private string _hashValue = string.Empty;
    private string _verifyHash = string.Empty;
    private bool? _verifyMatched;
    private string _errorMessage = string.Empty;
    private bool _isProcessing;
    private bool _justCopied;

    private IBrowserFile? _selectedFile;
    private string _fileName = string.Empty;
    private long _fileSize;
    private HashSource _source = HashSource.Text;
    private bool _isDragging;

    private string CurrentSourceLabel =>
        _source == HashSource.File && !string.IsNullOrWhiteSpace(_fileName)
            ? $"FILE: {_fileName}"
            : "TEXT";

    private Task OnTextChanged()
    {
        _source = HashSource.Text;
        _errorMessage = string.Empty;

        if (string.IsNullOrEmpty(_inputText))
        {
            _hashValue = string.Empty;
            UpdateVerifyState();
            return Task.CompletedTask;
        }

        _hashValue = ComputeHash(Encoding.UTF8.GetBytes(_inputText));
        UpdateVerifyState();
        return Task.CompletedTask;
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        _errorMessage = string.Empty;
        _isProcessing = true;
        StateHasChanged();

        try
        {
            var file = e.File;
            if (file.Size > AppTools.MaxFileSize)
            {
                _errorMessage =
                    $"文件过大，请选择小于 {AppTools.MaxFileSize / 1024d / 1024d:F2} MB 的文件";
                return;
            }

            _selectedFile = file;
            _fileName = file.Name;
            _fileSize = file.Size;
            _source = HashSource.File;

            _hashValue = await ComputeHashFromFileAsync(file);
            UpdateVerifyState();
        }
        catch (Exception ex)
        {
            _errorMessage = $"读取文件失败：{ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task SelectAlgorithmAsync(HashAlgorithmType algorithm)
    {
        if (_algorithm == algorithm)
        {
            return;
        }

        _algorithm = algorithm;
        if (_source == HashSource.File && _selectedFile != null)
        {
            await RecomputeFileHashAsync();
            return;
        }

        await OnTextChanged();
    }

    private async Task RecomputeFileHashAsync()
    {
        if (_selectedFile == null)
        {
            _hashValue = string.Empty;
            UpdateVerifyState();
            return;
        }

        _errorMessage = string.Empty;
        _isProcessing = true;
        StateHasChanged();

        try
        {
            _hashValue = await ComputeHashFromFileAsync(_selectedFile);
            UpdateVerifyState();
        }
        catch (Exception ex)
        {
            _errorMessage = $"计算哈希失败：{ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private Task OnVerifyChanged()
    {
        UpdateVerifyState();
        return Task.CompletedTask;
    }

    private void UpdateVerifyState()
    {
        if (string.IsNullOrWhiteSpace(_verifyHash) || string.IsNullOrWhiteSpace(_hashValue))
        {
            _verifyMatched = null;
            return;
        }

        _verifyMatched = NormalizeHash(_verifyHash) == NormalizeHash(_hashValue);
    }

    private static string NormalizeHash(string value)
    {
        return new string(value.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToUpperInvariant();
    }

    private async Task CopyHashAsync()
    {
        if (string.IsNullOrWhiteSpace(_hashValue))
        {
            return;
        }

        try
        {
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _hashValue);
            _justCopied = true;
            StateHasChanged();
            await Task.Delay(2000);
            _justCopied = false;
            StateHasChanged();
        }
        catch (Exception)
        {
            // 忽略复制失败
        }
    }

    private void ClearAll()
    {
        _inputText = string.Empty;
        _hashValue = string.Empty;
        _verifyHash = string.Empty;
        _verifyMatched = null;
        _errorMessage = string.Empty;
        _selectedFile = null;
        _fileName = string.Empty;
        _fileSize = 0;
        _source = HashSource.Text;
        _isDragging = false;
    }

    private void ClearFile()
    {
        _selectedFile = null;
        _fileName = string.Empty;
        _fileSize = 0;
        _hashValue = string.Empty;
        _errorMessage = string.Empty;
        UpdateVerifyState();
    }

    private void HandleDragEnter()
    {
        if (!_isProcessing)
        {
            _isDragging = true;
        }
    }

    private void HandleDragLeave()
    {
        _isDragging = false;
    }

    private async Task HandleDrop(Microsoft.AspNetCore.Components.Web.DragEventArgs e)
    {
        _isDragging = false;

        if (_isProcessing)
        {
            return;
        }

        // 注意：Blazor WebAssembly 不支持直接从 DragEventArgs 获取文件
        // 用户需要使用 InputFile 组件来选择文件
        // 这里只是重置拖拽状态
        await Task.CompletedTask;
    }

    private string ComputeHash(byte[] bytes)
    {
        using var algorithm = CreateAlgorithm();
        var hashBytes = algorithm.ComputeHash(bytes);
        return ToHexString(hashBytes);
    }

    private async Task<string> ComputeHashFromFileAsync(IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(AppTools.MaxFileSize);
        using var algorithm = CreateAlgorithm();
        var hashBytes = await algorithm.ComputeHashAsync(stream);
        return ToHexString(hashBytes);
    }

    private HashAlgorithm CreateAlgorithm()
    {
        return _algorithm switch
        {
            HashAlgorithmType.MD5 => MD5Hash.Create(),
            HashAlgorithmType.SHA1 => SHA1.Create(),
            HashAlgorithmType.SHA256 => SHA256.Create(),
            HashAlgorithmType.SHA384 => SHA384.Create(),
            HashAlgorithmType.SHA512 => SHA512.Create(),
            _ => SHA256.Create(),
        };
    }

    private static string ToHexString(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("X2"));
        }

        return sb.ToString();
    }
}
