using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace StickyHomeworks2.Models;

public class RichDocument
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("metadata")]
    public DocumentMetadata Metadata { get; set; } = new();

    [JsonPropertyName("blocks")]
    public List<RichBlock> Blocks { get; set; } = new();

    public static RichDocument CreateEmpty()
    {
        return new RichDocument
        {
            Metadata = new DocumentMetadata
            {
                CreatedAt = DateTime.Now,
                ModifiedAt = DateTime.Now
            },
            Blocks = new List<RichBlock>
            {
                new RichBlock
                {
                    Type = BlockType.Paragraph,
                    Content = new List<RichInline>
                    {
                        new RichRun { Text = "" }
                    }
                }
            }
        };
    }

    public static RichDocument FromPlainText(string text)
    {
        return new RichDocument
        {
            Metadata = new DocumentMetadata(),
            Blocks = new List<RichBlock>
            {
                new RichBlock
                {
                    Type = BlockType.Paragraph,
                    Content = new List<RichInline>
                    {
                        new RichRun { Text = text }
                    }
                }
            }
        };
    }

    public string ToPlainText()
    {
        var sb = new StringBuilder();
        foreach (var block in Blocks)
        {
            foreach (var inline in block.Content)
            {
                if (inline is RichRun run)
                {
                    sb.Append(run.Text);
                }
            }
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }
}

public class DocumentMetadata
{
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("modified_at")]
    public DateTime ModifiedAt { get; set; } = DateTime.Now;

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("word_count")]
    public int WordCount { get; set; }
}

public enum BlockType
{
    Paragraph,
    Heading1,
    Heading2,
    Heading3,
    BulletList,
    NumberedList,
    Quote,
    CodeBlock,
    HorizontalRule,
    ImageBlock
}

public class RichBlock
{
    [JsonPropertyName("type")]
    public BlockType Type { get; set; } = BlockType.Paragraph;

    [JsonPropertyName("alignment")]
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;

    [JsonPropertyName("content")]
    public List<RichInline> Content { get; set; } = new();

    [JsonPropertyName("list_items")]
    public List<ListItem>? ListItems { get; set; }

    [JsonPropertyName("image_data")]
    public EmbeddedImage? ImageData { get; set; }

    [JsonPropertyName("code_language")]
    public string? CodeLanguage { get; set; }
}

public enum TextAlignment
{
    Left,
    Center,
    Right,
    Justify
}

[JsonDerivedType(typeof(RichRun), "run")]
[JsonDerivedType(typeof(RichLineBreak), "linebreak")]
[JsonDerivedType(typeof(RichImage), "image")]
[JsonDerivedType(typeof(RichHyperlink), "hyperlink")]
[JsonDerivedType(typeof(RichEmoji), "emoji")]
public abstract class RichInline
{
    [JsonPropertyName("type")]
    public abstract InlineType InlineType { get; set; }
}

public enum InlineType
{
    Run,
    LineBreak,
    Image,
    Hyperlink,
    Emoji
}

public class RichRun : RichInline
{
    public override InlineType InlineType { get; set; } = InlineType.Run;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("font_weight")]
    public FontWeight? FontWeight { get; set; }

    [JsonPropertyName("font_style")]
    public FontStyle? FontStyle { get; set; }

    [JsonPropertyName("text_decoration")]
    public TextDecoration? TextDecoration { get; set; }

    [JsonPropertyName("foreground_color")]
    public string? ForegroundColor { get; set; }

    [JsonPropertyName("background_color")]
    public string? BackgroundColor { get; set; }

    [JsonPropertyName("font_size")]
    public double? FontSize { get; set; }

    [JsonPropertyName("font_family")]
    public string? FontFamily { get; set; }
}

public enum FontWeight
{
    Normal,
    Bold
}

public enum FontStyle
{
    Normal,
    Italic
}

public enum TextDecoration
{
    None,
    Underline,
    Strikethrough
}

public class RichLineBreak : RichInline
{
    public override InlineType InlineType { get; set; } = InlineType.LineBreak;
}

public class RichImage : RichInline
{
    public override InlineType InlineType { get; set; } = InlineType.Image;

    [JsonPropertyName("image")]
    public EmbeddedImage Image { get; set; } = new();

    [JsonPropertyName("width")]
    public double? DisplayWidth { get; set; }

    [JsonPropertyName("height")]
    public double? DisplayHeight { get; set; }
}

public class RichHyperlink : RichInline
{
    public override InlineType InlineType { get; set; } = InlineType.Hyperlink;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public List<RichInline> Content { get; set; } = new();

    [JsonPropertyName("tooltip")]
    public string? Tooltip { get; set; }
}

public class RichEmoji : RichInline
{
    public override InlineType InlineType { get; set; } = InlineType.Emoji;

    [JsonPropertyName("codepoint")]
    public string CodePoint { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class ListItem
{
    [JsonPropertyName("content")]
    public List<RichInline> Content { get; set; } = new();

    [JsonPropertyName("level")]
    public int Level { get; set; } = 0;
}

public class EmbeddedImage
{
    [JsonPropertyName("base64_data")]
    public string Base64Data { get; set; } = string.Empty;

    [JsonPropertyName("original_width")]
    public int OriginalWidth { get; set; }

    [JsonPropertyName("original_height")]
    public int OriginalHeight { get; set; }

    [JsonPropertyName("mime_type")]
    public string MimeType { get; set; } = "image/jpeg";

    public string ToLegacyMarker()
    {
        return $"\u2318IMG:{Base64Data}|{OriginalWidth}|{OriginalHeight}";
    }

    public static EmbeddedImage FromLegacyMarker(string marker)
    {
        if (!marker.StartsWith("\u2318IMG:"))
            throw new FormatException("Invalid image marker format");

        var parts = marker.Substring(5).Split('|');
        if (parts.Length != 3)
            throw new FormatException("Invalid marker structure");

        return new EmbeddedImage
        {
            Base64Data = parts[0],
            OriginalWidth = int.Parse(parts[1]),
            OriginalHeight = int.Parse(parts[2])
        };
    }
}
