using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dpz.Core.EnumLibrary;

namespace Dpz.Core.WebMore.Models;

public class CommentModel
{
    public required string Id { get; set; }

    /// <summary>
    /// 评论类型
    /// </summary>
    [JsonConverter(typeof(EnumConverter<CommentNode>))]
    public CommentNode Node { get; set; }

    /// <summary>
    /// 关联
    /// </summary>
    public required string Relation { get; set; }

    /// <summary>
    /// 回复时间
    /// </summary>
    public DateTime PublishTime { get; set; }

    /// <summary>
    /// 评论人
    /// </summary>
    [JsonConverter(typeof(CommenterConverter))]
    public required Commenter Commenter { get; set; }

    /// <summary>
    /// 回复内容
    /// </summary>
    public string? CommentText { get; set; }

    /// <summary>
    /// 回复ID
    /// </summary>
    public List<string> Replies { get; set; } = [];

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDelete { get; set; }

    /// <summary>
    /// 回复
    /// </summary>
    public List<CommentChildren> Children { get; set; } = [];

    public bool ShowReply { get; set; }
}

public class CommentChildren
{
    public required string Id { get; set; }

    /// <summary>
    /// 回复时间
    /// </summary>
    public DateTime PublishTime { get; set; }

    /// <summary>
    /// 评论人
    /// </summary>
    [JsonConverter(typeof(CommenterConverter))]
    public required Commenter Commenter { get; set; }

    /// <summary>
    /// 回复内容
    /// </summary>
    public string? CommentText { get; set; }

    /// <summary>
    /// 回复ID
    /// </summary>
    public List<string> Replies { get; set; } = [];

    /// <summary>
    /// 是否删除
    /// </summary>
    public bool IsDelete { get; set; }

    public bool ShowReply { get; set; }
}

public abstract class Commenter
{
    /// <summary>
    /// 昵称
    /// </summary>
    public required string NickName { get; set; }
}

/// <summary>
/// 登录会员评论
/// </summary>
public class MemberCommenter : Commenter
{
    /// <summary>
    /// 头像
    /// </summary>
    public required string Avatar { get; set; }

    /// <summary>
    /// 身份标识
    /// </summary>
    public string? Identity { get; set; }
}

/// <summary>
/// 匿名评论
/// </summary>
public class GuestCommenter : Commenter
{
    /// <summary>
    /// 网站
    /// </summary>
    public string? Site { get; set; }

    /// <summary>
    /// 邮箱MD5
    /// </summary>
    public required string EmailMd5 { get; set; }
}

public class CommenterConverter : JsonConverter<Commenter>
{
    public override Commenter? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("JsonTokenType.StartObject not found.");
        }

        if (
            !reader.Read()
            || reader.TokenType != JsonTokenType.PropertyName
            || reader.GetString() != "$type"
        )
        {
            throw new JsonException("Property $type not found.");
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Value at $type is invalid.");
        }

        var typeStr = reader.GetString();
        var type = typeStr switch
        {
            nameof(GuestCommenter) => typeof(GuestCommenter),
            nameof(MemberCommenter) => typeof(MemberCommenter),
            _ => typeof(Commenter),
        };
        using var output = new MemoryStream();
        ReadObject(ref reader, output, options);
        var result = JsonSerializer.Deserialize(output.ToArray(), type, options);
        return result as Commenter;
    }

    private void ReadObject(ref Utf8JsonReader reader, Stream output, JsonSerializerOptions options)
    {
        using var writer = new Utf8JsonWriter(
            output,
            new JsonWriterOptions { Encoder = options.Encoder, Indented = options.WriteIndented }
        );
        writer.WriteStartObject();
        var objectIntend = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.None:
                case JsonTokenType.Null:
                    writer.WriteNullValue();
                    break;
                case JsonTokenType.StartObject:
                    writer.WriteStartObject();
                    objectIntend++;
                    break;
                case JsonTokenType.EndObject:
                    writer.WriteEndObject();
                    if (objectIntend == 0)
                    {
                        writer.Flush();
                        return;
                    }
                    objectIntend--;
                    break;
                case JsonTokenType.StartArray:
                    writer.WriteStartArray();
                    break;
                case JsonTokenType.EndArray:
                    writer.WriteEndArray();
                    break;
                case JsonTokenType.PropertyName:
                    writer.WritePropertyName(reader.GetString() ?? "");
                    break;
                case JsonTokenType.Comment:
                    writer.WriteCommentValue(reader.GetComment());
                    break;
                case JsonTokenType.String:
                    writer.WriteStringValue(reader.GetString());
                    break;
                case JsonTokenType.Number:
                    writer.WriteNumberValue(reader.GetInt32());
                    break;
                case JsonTokenType.True:
                case JsonTokenType.False:
                    writer.WriteBooleanValue(reader.GetBoolean());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public override void Write(
        Utf8JsonWriter writer,
        Commenter value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();
        //var valueType = value.GetType();
        //var valueAssemblyName = valueType.Assembly.GetName();
        //writer.WriteString("$type", $"{valueType.FullName}, {valueAssemblyName.Name}");

        var json = JsonSerializer.Serialize(value, value.GetType(), options);
        using (
            var document = JsonDocument.Parse(
                json,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = options.AllowTrailingCommas,
                    MaxDepth = options.MaxDepth,
                }
            )
        )
        {
            foreach (var jsonProperty in document.RootElement.EnumerateObject())
            {
                jsonProperty.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
    }
}
