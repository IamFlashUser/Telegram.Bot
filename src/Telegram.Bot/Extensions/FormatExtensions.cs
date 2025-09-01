using System.Text;
using System.Threading.Tasks;

namespace Telegram.Bot.Extensions;

#pragma warning disable MA0001, MA0076, IDE0055

/// <summary>Helpers/Extensions for MarkdownV2 texts</summary>
public static class Markdown
{
    /// <summary>Generate MarkdownV2 text from the Message Text or Caption</summary>
    /// <param name="msg">The message</param>
    public static string? ToMarkdown(this Message msg)
    {
        if (msg.Text != null) return ToMarkdown(msg.Text, msg.Entities);
        if (msg.Caption != null) return ToMarkdown(msg.Caption, msg.CaptionEntities);
        return null;
    }

    /// <summary>Converts the (plain text + entities) format used by Telegram messages into a <a href="https://core.telegram.org/bots/api/#markdownv2-style">MarkdownV2 text</a></summary>
    /// <param name="message">The plain text, typically obtained from <see cref="Message.Text"/></param>
    /// <param name="entities">The array of formatting entities, typically obtained from <see cref="Message.Entities"/></param>
    /// <returns>The message text with MarkdownV2 formattings </returns>
    public static string ToMarkdown(string message, MessageEntity[]? entities)
    {
        if (entities == null || entities.Length == 0) return Escape(message);
        var closings = new List<(int offset, string md)>();
        var sb = new StringBuilder(message);
        int entityIndex = 0;
        var nextEntity = entities[entityIndex];
        bool inBlockQuote = false;
        char lastCh = '\0';
        for (int offset = 0, i = 0; ; offset++, i++)
        {
            while (closings.Count != 0 && offset == closings[0].offset)
            {
                var md = closings[0].md;
                closings.RemoveAt(0);
                if (i > 0 && md[0] == '_' && sb[i - 1] == '_') md = '\r' + md;
                if (md[0] == '>') { inBlockQuote = false; md = md[1..]; if (lastCh != '\n' && i < sb.Length && sb[i] != '\n') md += '\n'; }
                sb.Insert(i, md); i += md.Length;
            }
            if (i == sb.Length) break;
            if (lastCh == '\n' && inBlockQuote) sb.Insert(i++, '>');
            for (; offset == nextEntity?.Offset; nextEntity = ++entityIndex < entities.Length ? entities[entityIndex] : null)
            {
                if (EntityToMD.TryGetValue(nextEntity.Type, out var md))
                {
                    var closing = (nextEntity.Offset + nextEntity.Length, md);
                    if (md[0] is '[' or '!')
                    {
                        if (nextEntity.Type is MessageEntityType.TextLink)
                            closing.md = $"]({nextEntity.Url?.Replace("\\", "\\\\").Replace(")", "\\)").Replace(">", "%3E")})";
                        else if (nextEntity.Type is MessageEntityType.TextMention)
                            closing.md = $"](tg://user?id={nextEntity.User?.Id})";
                        else if (nextEntity.Type is MessageEntityType.CustomEmoji)
                            closing.md = $"](tg://emoji?id={nextEntity.CustomEmojiId})";
                    }
                    else if (md[0] == '>')
                    { inBlockQuote = true; md = lastCh is not '\n' and not '\0' ? "\n>" : ">"; }
                    else if (nextEntity.Type is MessageEntityType.Pre)
                        md = $"```{nextEntity.Language}\n";
                    int index = ~closings.BinarySearch(closing, Comparer<(int, string)>.Create((x, y) => x.Item1.CompareTo(y.Item1) | 1));
                    closings.Insert(index, closing);
                    if (i > 0 && md[0] == '_' && sb[i - 1] == '_') md = '\r' + md;
                    sb.Insert(i, md); i += md.Length;
                }
            }
            switch (lastCh = sb[i])
            {
                case '_': case '*': case '~': case '#': case '+': case '-': case '=': case '.': case '!':
                case '[': case ']': case '(': case ')': case '{': case '}': case '>': case '|': case '\\':
                    if (closings.Count != 0 && closings[0].md[0] == '`') break;
                    goto case '`';
                case '`':
                    sb.Insert(i++, '\\');
                    break;
            }
        }
        return sb.ToString();
    }

    private static readonly Dictionary<MessageEntityType, string> EntityToMD = new()
    {
        [MessageEntityType.Bold] = "*",
        [MessageEntityType.Italic] = "_",
        [MessageEntityType.Code] = "`",
        [MessageEntityType.Pre] = "```",
        [MessageEntityType.TextLink] = "[",
        [MessageEntityType.TextMention] = "[",
        [MessageEntityType.Underline] = "__",
        [MessageEntityType.Strikethrough] = "~",
        [MessageEntityType.Spoiler] = "||",
        [MessageEntityType.CustomEmoji] = "![",
        [MessageEntityType.Blockquote] = ">",
        [MessageEntityType.ExpandableBlockquote] = ">||",
    };

    /// <summary>Insert backslashes in front of MarkdownV2 reserved characters</summary>
    /// <param name="text">The text to escape</param>
    /// <returns>The escaped text (may return null if input is null)</returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? Escape(string? text)
    {
        if (text == null) return null;
        StringBuilder? sb = null;
        for (int index = 0, added = 0; index < text.Length; index++)
        {
            switch (text[index])
            {
                case '_': case '*': case '~': case '`': case '#': case '+': case '-': case '=': case '.': case '!':
                case '[': case ']': case '(': case ')': case '{': case '}': case '>': case '|': case '\\':
                    sb ??= new StringBuilder(text, text.Length + 32);
                    sb.Insert(index + added++, '\\');
                    break;
            }
        }
        return sb?.ToString() ?? text;
    }
}

/// <summary>Helpers/Extensions for HTML texts</summary>
public static class HtmlText
{
    /// <summary>Generate HTML text from the Message Text or Caption</summary>
    /// <param name="msg">The message</param>
    public static string? ToHtml(this Message msg)
    {
        if (msg.Text != null) return ToHtml(msg.Text, msg.Entities);
        if (msg.Caption != null) return ToHtml(msg.Caption, msg.CaptionEntities);
        return null;
    }

    /// <summary>Converts the (plain text + entities) format used by Telegram messages into an <a href="https://core.telegram.org/bots/api/#html-style">HTML-formatted text</a></summary>
    /// <param name="message">The plain text, typically obtained from <see cref="Message.Text"/></param>
    /// <param name="entities">The array of formatting entities, typically obtained from <see cref="Message.Entities"/></param>
    /// <returns>The message text with HTML formatting tags</returns>
    public static string ToHtml(string message, MessageEntity[]? entities)
    {
        if (entities == null || entities.Length == 0) return Escape(message);
        var closings = new List<(int offset, string tag)>();
        var sb = new StringBuilder(message);
        int entityIndex = 0;
        var nextEntity = entities[entityIndex];
        for (int offset = 0, i = 0; ; offset++, i++)
        {
            while (closings.Count != 0 && offset == closings[0].offset)
            {
                var tag = closings[0].tag;
                sb.Insert(i, tag); i += tag.Length;
                closings.RemoveAt(0);
            }
            if (i == sb.Length) break;
            for (; offset == nextEntity?.Offset; nextEntity = ++entityIndex < entities.Length ? entities[entityIndex] : null)
            {
                if (EntityToTag.TryGetValue(nextEntity.Type, out var tag))
                {
                    var closing = (nextEntity.Offset + nextEntity.Length, $"</{tag}>");
                    if (tag[0] == 'a')
                    {
                        if (nextEntity.Type is MessageEntityType.TextLink)
                            tag = $"<a href=\"{Escape(nextEntity.Url)}\">";
                        else if (nextEntity.Type is MessageEntityType.TextMention)
                            tag = $"<a href=\"tg://user?id={nextEntity.User?.Id}\">";
                    }
                    else if (nextEntity.Type is MessageEntityType.CustomEmoji)
                        tag = $"<tg-emoji emoji-id=\"{nextEntity.CustomEmojiId}\">";
                    else if (nextEntity.Type is MessageEntityType.Pre && !string.IsNullOrEmpty(nextEntity.Language))
                    {
                        closing.Item2 = "</code></pre>";
                        tag = $"<pre><code class=\"language-{nextEntity.Language}\">";
                    }
                    else if (nextEntity.Type is MessageEntityType.ExpandableBlockquote)
                        tag = "<blockquote expandable>";
                    else
                        tag = $"<{tag}>";
                    int index = ~closings.BinarySearch(closing, Comparer<(int, string)>.Create((x, y) => x.Item1.CompareTo(y.Item1) | 1));
                    closings.Insert(index, closing);
                    sb.Insert(i, tag); i += tag.Length;
                }
            }
            switch (sb[i])
            {
                case '&': sb.Insert(i + 1, "amp;"); i += 4; break;
                case '<': sb.Insert(i, "&lt"); sb[i += 3] = ';'; break;
                case '>': sb.Insert(i, "&gt"); sb[i += 3] = ';'; break;
            }
        }
        return sb.ToString();
    }

    private static readonly Dictionary<MessageEntityType, string> EntityToTag = new()
    {
        [MessageEntityType.Bold] = "b",
        [MessageEntityType.Italic] = "i",
        [MessageEntityType.Code] = "code",
        [MessageEntityType.Pre] = "pre",
        [MessageEntityType.TextLink] = "a",
        [MessageEntityType.TextMention] = "a",
        [MessageEntityType.Underline] = "u",
        [MessageEntityType.Strikethrough] = "s",
        [MessageEntityType.Spoiler] = "tg-spoiler",
        [MessageEntityType.CustomEmoji] = "tg-emoji",
        [MessageEntityType.Blockquote] = "blockquote",
        [MessageEntityType.ExpandableBlockquote] = "blockquote",
    };

    /// <summary>Replace special HTML characters with their &amp;xx; equivalent</summary>
    /// <param name="text">The text to make HTML-safe</param>
    /// <returns>The HTML-safe text (may return null if input is null)</returns>
    [return: NotNullIfNotNull(nameof(text))]
    public static string? Escape(string? text)
        => text?.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>Calculate the length of the plain text (excluding the &lt;tags&gt;) from the HTML text</summary>
    /// <param name="html">HTML text</param>
    /// <returns>Number of characters (HTML &amp;entities; are counted as 1)</returns>
    public static int PlainLength(string html)
    {
        var span = html.AsSpan();
        int len = 0, index;
        while ((index = span.IndexOfAny('&', '<')) != -1)
        {
            len += index;
            var c = span[index];
            if (c == '<') c = '>'; else { c = ';'; len++; }
            span = span[(index + 1)..];
            index = span.IndexOf(c);
            if (index < 0) { span = default; break; }
            span = span[(index + 1)..];
        }
        return len + span.Length;
    }

    /// <summary>Convert the HTML text to plain text (excluding the &lt;tags&gt;)</summary>
    /// <param name="html">HTML text</param>
    /// <returns>Plain text (only &amp;lt; &amp;gt; &amp;amp; &amp;quot; entities are converted)</returns>
    public static string ToPlain(string html)
    {
        var sb = new StringBuilder(html.Length);
        var span = html.AsSpan();
        int index;
        while ((index = span.IndexOfAny('&', '<')) != -1)
        {
            sb.Append(span[..index]);
            var c = span[index];
            span = span[(index + 1)..];
            c = c == '<' ? '>' : ';';
            index = span.IndexOf(c);
            if (index < 0) { span = default; break; }
            if (c == ';')
                if (index == 2 && span[0] == 'l' && span[1] == 't') sb.Append('<');
                else if (index == 2 && span[0] == 'g' && span[1] == 't') sb.Append('>');
                else if (index == 3 && span[0] == 'a' && span[1] == 'm' && span[2] == 'p') sb.Append('&');
                else if (index == 4 && span[0] == 'q' && span[1] == 'u' && span[2] == 'o' && span[3] == 't') sb.Append('"');
                else sb.Append('&').Append(span[..(index + 1)]);
            span = span[(index + 1)..];
        }
        sb.Append(span);
        return sb.ToString();
    }

    /// <summary>Truncate the HTML text to the specified number of plain-text characters</summary>
    /// <param name="html">HTML text</param>
    /// <param name="count">Target count of Unicode characters (including the suffix)</param>
    /// <param name="suffix">Suffix to append if a truncation was done</param>
    /// <returns>The HTML eventually truncated</returns>
    public static string Truncate(string html, int count, string suffix = "…")
    {
        int len = html.Length;
        if (len <= count) return html;
        count -= suffix.Length;
        if (count < 0) throw new ArgumentException("Invalid count", nameof(count));
        var closingTags = new StringBuilder();
        int index = 0;
        for (; count > 0 && index < len; index++)
        {
            var c = html[index];
            if (c == '&')
                index = html.IndexOf(';', index + 1);
            else if (c == '<')
            {
                int end = html.IndexOf('>', index + 1);
                if (html[index + 1] == '/')
                {
                    int idx = 3;
                    while (closingTags[idx++] != '>') { }
                    closingTags.Remove(0, idx);
                }
                else if (html[end - 1] != '/')
                {
                    int gap = html.IndexOf(' ', index + 2, end - index - 2);
                    var tag = html.AsSpan()[(index == 0 ? 0 : index - 1)..(gap < 0 ? end + 1 : gap + 1)];
                    closingTags.Insert(0, tag);
                    closingTags[tag.Length - 1] = '>';
                    if (index == 0) closingTags.Insert(0, '<');
                    else closingTags[0] = '<';
                    closingTags[1] = '/';
                }
                index = end;
                continue;
            }
            else if (char.IsLowSurrogate(c)) // surrogate pairs are counted as 1
                continue;
            count--;
        }
        return index == len ? html : html[..index] + suffix + closingTags.ToString();
    }
 
#if NET6_0_OR_GREATER
    /// <summary>
    /// All-in-one helper method to send messages from HTML string with optional (multiple) media and keyboard attached.
    /// <para>Media caption can be placed above or below their tag</para>
    /// </summary>
    /// <param name="botClient">An instance of <see cref="ITelegramBotClient"/></param>
    /// <param name="chatId">Unique identifier for the target chat or username of the target channel (in the format <c>@channelusername</c>)</param>
    /// <param name="html">The message in Html, with optional &lt;img&gt;, &lt;video&gt;, &lt;file&gt; tags for media, and inline/reply &lt;keyboard&gt;</param>
    /// <param name="replyParameters">Description of the message to reply to</param>
    /// <param name="messageThreadId">Unique identifier for the target message thread (topic) of the forum; for forum supergroups only</param>
    /// <param name="protectContent">Protects the contents of the sent messages from forwarding and saving</param>
    /// <param name="businessConnectionId">Unique identifier of the business connection on behalf of which the message will be sent</param>
    /// <returns>The sent <see cref="Message"/> is returned. (the first one for media group)</returns>
    /// <exception cref="FormatException">Malformed HTML</exception>
    public static async Task<Message> SendHtml(this ITelegramBotClient botClient, ChatId chatId, string html, ReplyParameters? replyParameters = null, int? messageThreadId = null, bool protectContent = false, string? businessConnectionId = null)
    {
        var span = html.AsSpan().Trim();
        ReplyMarkup? replyMarkup = null;
        if (span.EndsWith("</keyboard>", StringComparison.OrdinalIgnoreCase))
        {
            var index = span.LastIndexOf("<keyboard", StringComparison.OrdinalIgnoreCase);
            if (index < 0) throw new FormatException("Invalid <keyboard> tag");
            replyMarkup = ParseHtmlKeyboard(span[(index + 9)..^11]);
            span = span[..index].TrimEnd();
        }
        //TO-DO: support a <preview> tag to control LinkPreviewOptions
        List<IAlbumInputMedia>? media = null;
        bool captionAbove = false;
        InputMedia? im = null;
        while (true)
        {
            int iImg = span.IndexOf("<img src=\"", StringComparison.OrdinalIgnoreCase);
            int iVid = span.IndexOf("<video src=\"", StringComparison.OrdinalIgnoreCase);
            int iFile = span.IndexOf("<file src=\"", StringComparison.OrdinalIgnoreCase);
            int index = (uint)iImg < (uint)iVid ? (uint)iImg < (uint)iFile ? iImg : iFile : (uint)iVid < (uint)iFile ? iVid : iFile;
            //static readonly SearchValues<string> SpecialHtmlTags = SearchValues.Create(["<img src=\"", "<video src=\"", "<file src=\""], StringComparison.OrdinalIgnoreCase);
            //var index = span.IndexOfAny(SpecialHtmlTags);
            if (index < 0)
            {
                if (im is { Caption: null })
                {
                    im.Caption = Truncate(span.ToString(), 1024);
                    im.ParseMode = ParseMode.Html;
                }
                break;
            }
            var caption = span[..index].Trim();
            if (caption.Length > 0)
                if (im is { Caption: null })
                {
                    im.Caption = Truncate(caption.ToString(), 1024);
                    im.ParseMode = ParseMode.Html;
                    caption = default;
                }
                else
                    captionAbove = true;
            if (index == iImg)
            {
                var end = span[(index + 10)..].IndexOf('"');
                if (end < 0) throw new FormatException("Invalid <img> tag");
                var imp = new InputMediaPhoto(span.Slice(index + 10, end).ToString());
                span = span[(index + 11 + end)..];
                if (captionAbove) imp.ShowCaptionAboveMedia = true;
                if (span.StartsWith(" spoiler", StringComparison.OrdinalIgnoreCase)) imp.HasSpoiler = true;
                im = imp;
            }
            else if (index == iVid)
            {
                var end = span[(index + 12)..].IndexOf('"');
                if (end < 0) throw new FormatException("Invalid <video> tag");
                var imv = new InputMediaVideo(span.Slice(index + 12, end).ToString()) { SupportsStreaming = true };
                span = span[(index + 13 + end)..];
                if (captionAbove) imv.ShowCaptionAboveMedia = true;
                if (span.StartsWith(" spoiler", StringComparison.OrdinalIgnoreCase)) imv.HasSpoiler = true;
                im = imv;
            }
            else
            {
                var end = span[(index + 11)..].IndexOf('"');
                if (end < 0) throw new FormatException("Invalid <file> tag");
                im = new InputMediaDocument(span.Slice(index + 11, end).ToString());
                span = span[(index + 12 + end)..];
            }
            if (caption.Length > 0)
            {
                im.Caption = Truncate(caption.ToString(), 1024);
                im.ParseMode = ParseMode.Html;
            }
            (media ??= []).Add((IAlbumInputMedia)im);
            index = span.IndexOf('>');
            if (index < 0) throw new FormatException("Malformed tag: missing '>'");
            span = span[(index + 1)..].TrimStart();
        }

        if (media == null) return await botClient.SendMessage(chatId, Truncate(span.Trim().ToString(), 4095), ParseMode.Html, replyParameters, replyMarkup, messageThreadId: messageThreadId, protectContent: protectContent, businessConnectionId: businessConnectionId).ConfigureAwait(false);
        if (replyMarkup == null) return (await botClient.SendMediaGroup(chatId, media, replyParameters, messageThreadId, protectContent: protectContent, businessConnectionId: businessConnectionId).ConfigureAwait(false))[0];
        if (media.Count > 1) throw new FormatException("Cannot use keyboard with media group");
        return media[0] switch
        {
            InputMediaPhoto p => await botClient.SendPhoto(chatId, p.Media, p.Caption, ParseMode.Html, replyParameters, replyMarkup, messageThreadId: messageThreadId, showCaptionAboveMedia: p.ShowCaptionAboveMedia, hasSpoiler: p.HasSpoiler, protectContent: protectContent, businessConnectionId: businessConnectionId).ConfigureAwait(false),
            InputMediaVideo v => await botClient.SendVideo(chatId, v.Media, v.Caption, ParseMode.Html, replyParameters, replyMarkup, messageThreadId: messageThreadId, showCaptionAboveMedia: v.ShowCaptionAboveMedia, hasSpoiler: v.HasSpoiler, supportsStreaming: v.SupportsStreaming, protectContent: protectContent, businessConnectionId: businessConnectionId).ConfigureAwait(false),
            InputMediaDocument d => await botClient.SendDocument(chatId, d.Media, d.Caption, ParseMode.Html, replyParameters, replyMarkup, messageThreadId: messageThreadId, protectContent: protectContent, businessConnectionId: businessConnectionId).ConfigureAwait(false),
            _ => throw new FormatException("Unsupported media type")
        };
    }

    [Flags] enum SwitchInlineTarget { User, Bot, Group, Channel }
    private static ReplyMarkup ParseHtmlKeyboard(ReadOnlySpan<char> keyboard)
    {
        var isReply = keyboard.StartsWith(" reply", StringComparison.OrdinalIgnoreCase);
        if (isReply)
            if (keyboard.StartsWith(" reply_remove", StringComparison.OrdinalIgnoreCase)) return new ReplyKeyboardRemove();
            else if (CheckArg(ref keyboard, " reply_force=\"", out var placeholder)) return new ForceReplyMarkup { InputFieldPlaceholder = placeholder };
        ReplyKeyboardMarkup? reply = isReply ? new(resizeKeyboard: true) : null;
        InlineKeyboardMarkup? inline = isReply ? null : new();
        keyboard = keyboard[(keyboard.IndexOf('>') + 1)..].Trim();
        while (keyboard.Length != 0 && keyboard[0] == '<')
        {
            if (keyboard.StartsWith("<row>", StringComparison.OrdinalIgnoreCase))
                if (inline != null) inline.AddNewRow(); else reply!.AddNewRow();
            else if (keyboard.StartsWith("</row>", StringComparison.OrdinalIgnoreCase)) { }
            else if (CheckArg(ref keyboard, "<button text=\"", out var text))
            {
                if (inline != null)
                {
                    if (CheckArg(ref keyboard, " url=\"", out var url))
                        inline.AddButton(InlineKeyboardButton.WithUrl(text, url));
                    else if (CheckArg(ref keyboard, " callback=\"", out var data))
                        inline.AddButton(InlineKeyboardButton.WithCallbackData(text, data));
                    else if (CheckArg(ref keyboard, " app=\"", out var app))
                        inline.AddButton(InlineKeyboardButton.WithWebApp(text, app));
                    else if (CheckArg(ref keyboard, " copy=\"", out var copy))
                        inline.AddButton(InlineKeyboardButton.WithCopyText(text, copy));
                    else if (CheckArg(ref keyboard, " switch_inline=\"", out var query))
                        if (CheckArg(ref keyboard, " target=\"", out var target))
                            if (Enum.TryParse<SwitchInlineTarget>(target, ignoreCase: true, out var targets))
                                inline.AddButton(InlineKeyboardButton.WithSwitchInlineQueryChosenChat(text, new()
                                {
                                    AllowUserChats = targets.HasFlag(SwitchInlineTarget.User),
                                    AllowBotChats = targets.HasFlag(SwitchInlineTarget.Bot),
                                    AllowGroupChats = targets.HasFlag(SwitchInlineTarget.Group),
                                    AllowChannelChats = targets.HasFlag(SwitchInlineTarget.Channel),
                                }));
                            else
                                inline.AddButton(InlineKeyboardButton.WithSwitchInlineQuery(text, query));
                        else
                            inline.AddButton(InlineKeyboardButton.WithSwitchInlineQueryCurrentChat(text, query));
                    else
                        throw new FormatException("Unrecognized inline <button> type");
                }
                else if (reply != null)
                {
                    if (keyboard[0] == '>')
                        reply.AddButton(text);
                    else if (CheckArg(ref keyboard, " request_contact", out _))
                        reply.AddButton(KeyboardButton.WithRequestContact(text));
                    else if (CheckArg(ref keyboard, " request_location", out _))
                        reply.AddButton(KeyboardButton.WithRequestLocation(text));
                    else if (CheckArg(ref keyboard, " request_poll=\"", out var pollType))
                        reply.AddButton(KeyboardButton.WithRequestPoll(text, pollType is "" or "any" ? (PollType?)null : Enum.Parse<PollType>(pollType, ignoreCase: true)));
                    //TO-DO: support request_users and request_chat?
                    else if (CheckArg(ref keyboard, " app=\"", out var app))
                        reply.AddButton(KeyboardButton.WithWebApp(text, app));
                    else
                        throw new FormatException("Unrecognized reply <button> type");
                }
            }
            keyboard = keyboard[(keyboard.IndexOf('>') + 1)..].Trim();
        }
        return isReply ? reply! : inline!;

        static bool CheckArg(ref ReadOnlySpan<char> kb, string match, [NotNullWhen(true)] out string? arg)
        {
            if (!kb.StartsWith(match, StringComparison.OrdinalIgnoreCase)) { arg = null; return false; }
            kb = kb[match.Length..];
            if (match[^1] != '"') { arg = ""; return true; }
            var end = kb.IndexOf('"');
            if (end < 0) throw new FormatException("Quote missing in <button> tag");
            arg = kb[..end].ToString();
            kb = kb[(end + 1)..];
            return true;
        }
    }
#else //!NET6_0_OR_GREATER
    private static StringBuilder Append(this StringBuilder sb, ReadOnlySpan<char> value) => sb.Append(value.ToString());
    private static StringBuilder Insert(this StringBuilder sb, int index, ReadOnlySpan<char> value) => sb.Insert(index, value.ToString());
#endif
}
