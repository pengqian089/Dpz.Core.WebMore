using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Dpz.Core.WebMore.Pages.Tools;

public partial class ShowBsonToJson(IAppDialogService dialogService) : ComponentBase
{
    private long _fileSize;
    private string _objectIdValue = "";
    private bool? _parseObjectIdSuccess;
    private ObjectId? _objectId;
    private string _bsonValue = "{}";
    private string? _errorMessage;
    private Guid _renderKey = Guid.NewGuid();

    private async Task OnSelectedFile(InputFileChangeEventArgs e)
    {
        _errorMessage = null;
        _bsonValue = "{}";
        // 重置 key 强制重新渲染
        _renderKey = Guid.NewGuid();
        var file = e.File;

        if (file.Size > AppTools.MaxFileSize)
        {
            await dialogService.AlertAsync(
                $"文件过大，请选择小于 {AppTools.MaxFileSize / 1024d / 1024d:F2} MB 的文件"
            );
            return;
        }

        try
        {
            await using var stream = file.OpenReadStream(AppTools.MaxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            _fileSize = memoryStream.Length;
            memoryStream.Position = 0;

            var data = ParseBson(memoryStream);

            if (data.Count == 0)
            {
                await dialogService.AlertAsync("未解析到任何 BSON 文档，请确认文件格式正确。");
            }
            else
            {
                var json = data.ToJson(new JsonWriterSettings { Indent = true });
                _bsonValue = json;
                // 内容更新后再次刷新 key
                _renderKey = Guid.NewGuid();
            }
        }
        catch (Exception ex)
        {
            _errorMessage = "处理文件时发生错误：" + ex.Message;
            await dialogService.AlertAsync(_errorMessage);
            Console.WriteLine(ex);
        }

        StateHasChanged();
    }

    private List<BsonDocument> ParseBson(MemoryStream memoryStream)
    {
        var list = new List<BsonDocument>();
        try
        {
            using var reader = new BsonBinaryReader(memoryStream);
            while (!reader.IsAtEndOfFile())
            {
                var bsonType = reader.ReadBsonType();
                if (bsonType == BsonType.EndOfDocument)
                {
                    break;
                }

                var document = ReadBsonDocument(reader);
                list.Add(document);
            }
        }
        catch (Exception e)
        {
            dialogService.AlertAsync("解析 BSON 文档时发生错误：" + e.Message);
            return list;
        }

        return list;
    }

    private static BsonDocument ReadBsonDocument(BsonBinaryReader reader)
    {
        var context = BsonDeserializationContext.CreateRoot(reader);
        var bsonDocumentSerializer = BsonSerializer.LookupSerializer<BsonDocument>();
        return bsonDocumentSerializer.Deserialize(context);
    }

    private void OnObjectIdValueChange()
    {
        if (string.IsNullOrWhiteSpace(_objectIdValue))
        {
            _parseObjectIdSuccess = null;
            _objectId = null;
            return;
        }

        if (ObjectId.TryParse(_objectIdValue, out var oid))
        {
            _objectId = oid;
            _parseObjectIdSuccess = true;
        }
        else
        {
            _parseObjectIdSuccess = false;
            _objectId = null;
        }
    }
}
