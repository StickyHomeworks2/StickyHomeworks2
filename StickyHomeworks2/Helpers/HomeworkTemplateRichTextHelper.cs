using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace StickyHomeworks2.Helpers;

public static class HomeworkTemplateRichTextHelper
{
    public static bool ParagraphLeadsWithBook(string paragraphPlainText, string bookName)
    {
        if (string.IsNullOrEmpty(bookName))
            return false;

        var t = (paragraphPlainText ?? "").TrimStart();
        if (!t.StartsWith(bookName, StringComparison.Ordinal))
            return false;
        if (t.Length == bookName.Length)
            return true;
        return char.IsWhiteSpace(t[bookName.Length]);
    }

    public static bool LineContainsPartToken(string linePlainText, string partText)
    {
        if (string.IsNullOrEmpty(partText))
            return false;

        foreach (var tok in (linePlainText ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (tok == partText)
                return true;
        }

        return false;
    }

    public static bool DocumentContainsBookLine(RichTextBox? rtb, string bookName)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName))
            return false;

        foreach (var block in rtb.Document.Blocks)
        {
            if (block is not Paragraph para)
                continue;
            var t = new TextRange(para.ContentStart, para.ContentEnd).Text ?? "";
            if (ParagraphLeadsWithBook(t, bookName))
                return true;
        }

        return false;
    }

    private static Paragraph? FindFirstBookParagraph(RichTextBox? rtb, string bookName)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName))
            return null;

        foreach (var block in rtb.Document.Blocks)
        {
            if (block is not Paragraph para)
                continue;
            var t = new TextRange(para.ContentStart, para.ContentEnd).Text ?? "";
            if (ParagraphLeadsWithBook(t, bookName))
                return para;
        }

        return null;
    }

    public static void MoveCaretToParagraphContaining(RichTextBox? rtb, string bookName)
    {
        var p = FindFirstBookParagraph(rtb, bookName);
        if (p != null && rtb != null)
            rtb.CaretPosition = p.ContentEnd;
    }

    public static void RestoreCaretAfterTemplateSave(RichTextBox? rtb, string? bookNameIfPresentInDoc)
    {
        if (rtb?.Document == null)
            return;

        if (!string.IsNullOrEmpty(bookNameIfPresentInDoc) &&
            DocumentContainsBookLine(rtb, bookNameIfPresentInDoc))
            MoveCaretToParagraphContaining(rtb, bookNameIfPresentInDoc);
        else if (rtb.Document.Blocks.LastBlock is Paragraph last)
            rtb.CaretPosition = last.ContentEnd;
        else
            rtb.CaretPosition = rtb.Document.ContentStart;

        Keyboard.Focus(rtb);
    }

    public static string GetCaretParagraphPlainText(RichTextBox? rtb)
    {
        if (rtb?.Document == null)
            return "";

        var ptr = rtb.CaretPosition;
        if (ptr?.Paragraph is Paragraph direct)
            return new TextRange(direct.ContentStart, direct.ContentEnd).Text ?? "";

        try
        {
            var backward = ptr?.GetInsertionPosition(LogicalDirection.Backward);
            if (backward?.Paragraph is Paragraph pb)
                return new TextRange(pb.ContentStart, pb.ContentEnd).Text ?? "";
        }
        catch (ArgumentException)
        {
            // 指针落在无效边界
        }

        if (rtb.Document.Blocks.LastBlock is Paragraph last)
            return new TextRange(last.ContentStart, last.ContentEnd).Text ?? "";

        return "";
    }

    public static void InsertPlainAtCaret(RichTextBox? rtb, string text)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(text))
            return;

        var start = rtb.Selection.Start;
        var insert = text;
        try
        {
            var docStart = rtb.Document.ContentStart;
            if (start.CompareTo(docStart) > 0)
            {
                var before = start.GetPositionAtOffset(-1);
                var prevChar = new TextRange(before, start).Text;
                if (prevChar.Length > 0)
                {
                    var c = prevChar[0];
                    if (c != ' ' && c != '\n' && c != '\r')
                        insert = " " + insert;
                }
            }
        }
        catch (ArgumentOutOfRangeException)
        {
            // 光标在文档边界等，直接插入
        }

        try
        {
            var range = new TextRange(rtb.Selection.Start, rtb.Selection.End);
            range.Text = insert;
            rtb.Focus();
        }
        catch (InvalidOperationException)
        {
            rtb.Document.Blocks.Add(new Paragraph(new Run(insert)));
            rtb.CaretPosition = rtb.Document.ContentEnd;
            rtb.Focus();
        }
    }

    public static void EnsureBookLine(RichTextBox? rtb, string bookName)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName))
            return;
        if (DocumentContainsBookLine(rtb, bookName))
            return;

        var doc = rtb.Document;

        if (doc.Blocks.Count == 1 && doc.Blocks.FirstBlock is Paragraph onlyPara)
        {
            var existing = new TextRange(onlyPara.ContentStart, onlyPara.ContentEnd).Text ?? "";
            if (string.IsNullOrWhiteSpace(existing))
            {
                onlyPara.Inlines.Clear();
                onlyPara.Inlines.Add(new Run(bookName));
                rtb.CaretPosition = onlyPara.ContentEnd;
                return;
            }
        }

        if (doc.Blocks.LastBlock is Paragraph lastPara && lastPara.Inlines.Count > 0)
        {
            var lastRange = new TextRange(lastPara.ContentStart, lastPara.ContentEnd);
            lastRange.Text = (lastRange.Text ?? "").TrimEnd();
        }

        var newPara = new Paragraph(new Run(bookName));
        doc.Blocks.Add(newPara);
        rtb.CaretPosition = newPara.ContentEnd;
    }

    public static void RemoveBookLine(RichTextBox? rtb, string bookName)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName))
            return;

        var doc = rtb.Document;
        Paragraph? found = null;
        foreach (var block in doc.Blocks.ToList())
        {
            if (block is not Paragraph para)
                continue;
            var t = new TextRange(para.ContentStart, para.ContentEnd).Text ?? "";
            if (ParagraphLeadsWithBook(t, bookName))
            {
                found = para;
                break;
            }
        }

        if (found == null)
            return;

        doc.Blocks.Remove(found);

        if (doc.Blocks.LastBlock is Paragraph last)
            rtb.CaretPosition = last.ContentEnd;
        else
            rtb.CaretPosition = doc.ContentStart;
    }

    public static void ToggleBookLine(RichTextBox? rtb, string bookName)
    {
        if (DocumentContainsBookLine(rtb, bookName))
            RemoveBookLine(rtb, bookName);
        else
            EnsureBookLine(rtb, bookName);
    }

    public static void EnsurePartOnBookParagraph(RichTextBox? rtb, string bookName, string partText)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName) || string.IsNullOrEmpty(partText))
            return;

        var para = FindFirstBookParagraph(rtb, bookName);
        if (para == null)
            return;

        var range = new TextRange(para.ContentStart, para.ContentEnd);
        var lineText = range.Text ?? "";
        if (LineContainsPartToken(lineText, partText))
            return;

        var trimmed = lineText.TrimEnd();
        var spacer = trimmed.Length > 0 ? " " : "";
        range.Text = trimmed + spacer + partText;

        rtb.CaretPosition = para.ContentEnd;
    }

    public static void RemoveLastPartOnBookParagraph(RichTextBox? rtb, string bookName, string partText)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(bookName) || string.IsNullOrEmpty(partText))
            return;

        var para = FindFirstBookParagraph(rtb, bookName);
        if (para == null)
            return;

        var range = new TextRange(para.ContentStart, para.ContentEnd);
        var lineText = range.Text ?? "";
        var tokens = lineText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            if (tokens[i] != partText)
                continue;
            tokens.RemoveAt(i);
            range.Text = string.Join(" ", tokens).TrimEnd();
            rtb.CaretPosition = para.ContentEnd;
            return;
        }
    }

    public static void TogglePartInCurrentParagraph(RichTextBox? rtb, string partText)
    {
        if (rtb?.Document == null || string.IsNullOrEmpty(partText))
            return;

        var caret = rtb.CaretPosition;
        var para = caret?.Paragraph;
        if (para == null)
        {
            rtb.Document.Blocks.Add(new Paragraph(new Run(partText)));
            rtb.CaretPosition = rtb.Document.ContentEnd;
            return;
        }

        var range = new TextRange(para.ContentStart, para.ContentEnd);
        var lineText = range.Text ?? "";
        var tokens = lineText.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
        for (var i = tokens.Count - 1; i >= 0; i--)
        {
            if (tokens[i] != partText)
                continue;
            tokens.RemoveAt(i);
            range.Text = string.Join(" ", tokens).TrimEnd();
            rtb.CaretPosition = para.ContentEnd;
            return;
        }

        var trimmed = lineText.TrimEnd();
        var spacer = trimmed.Length > 0 ? " " : "";
        range.Text = trimmed + spacer + partText;

        rtb.CaretPosition = para.ContentEnd;
    }
}
