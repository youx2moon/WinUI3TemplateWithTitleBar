using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace $safeprojectname$.Services
{
    public static class MarkdownHelper
    {
        public static void SetMarkdownText(RichTextBlock richTextBlock, string text)
        {
            richTextBlock.Blocks.Clear();

            if (string.IsNullOrEmpty(text)) return;

            // 改行で分割（空行を維持）
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (var rawLine in lines)
            {
                string line = rawLine.TrimEnd();

                // 1. 水平線 (---) の判定
                if (Regex.IsMatch(line, @"^(\s*[-*_]){3,}\s*$"))
                {
                    richTextBlock.Blocks.Add(CreateHorizontalRule());
                    continue;
                }

                // 2. 見出し (# ) の判定
                var headerMatch = Regex.Match(line, @"^(#{1,6})\s+(.*)$");
                if (headerMatch.Success)
                {
                    int level = headerMatch.Groups[1].Length;
                    string content = headerMatch.Groups[2].Value;
                    richTextBlock.Blocks.Add(CreateHeaderBlock(content, level));
                    continue;
                }

                // 3. 通常の段落
                if (string.IsNullOrWhiteSpace(line))
                {
                    // 空行の場合は少し隙間を空ける段落を追加
                    var emptyPara = new Paragraph { Margin = new Thickness(0, 0, 0, 8) };
                    richTextBlock.Blocks.Add(emptyPara);
                }
                else
                {
                    var paragraph = new Paragraph { Margin = new Thickness(0, 0, 0, 4) };
                    ParseInlineElements(paragraph.Inlines, line);
                    richTextBlock.Blocks.Add(paragraph);
                }
            }
        }

        private static Paragraph CreateHeaderBlock(string text, int level)
        {
            var paragraph = new Paragraph();
            double fontSize = level switch
            {
                1 => 24,
                2 => 20,
                3 => 18,
                _ => 16
            };

            paragraph.Margin = new Thickness(0, level == 1 ? 12 : 8, 0, 4);
            var bold = new Bold();
            bold.Inlines.Add(new Run { Text = text, FontSize = fontSize });
            paragraph.Inlines.Add(bold);
            return paragraph;
        }

        private static Paragraph CreateHorizontalRule()
        {
            var paragraph = new Paragraph { Margin = new Thickness(0, 8, 0, 8) };
            var lineShape = new Rectangle
            {
                Height = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Fill = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"],
                Opacity = 0.6,
                Margin = new Thickness(0, 4, 0, 4)
            };
            paragraph.Inlines.Add(new InlineUIContainer { Child = lineShape });
            return paragraph;
        }

        private static void ParseInlineElements(InlineCollection inlines, string line)
        {
            // 正規表現の修正ポイント:
            // (?!\s)  -> 後ろに空白が来てはいけない (開始記号の条件)
            // (?<!\s) -> 前に空白が来てはいけない (終了記号の条件)
            // これにより "* " (箇条書き) や " * " (単なる記号) が装飾として誤認されるのを防ぎます。
            var pattern = @"(" +
                @"\*\*\*(?!\s).+?(?<!\s)\*\*\*|" + // 太字斜体
                @"\*\*(?!\s).+?(?<!\s)\*\*|" +     // 太字
                @"\*(?!\s).+?(?<!\s)\*|" +         // 斜体
                @"`.*?`" +                         // コード
                @")";

            var parts = Regex.Split(line, pattern);

            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part)) continue;

                // 太字斜体 (***text***)
                if (part.Length >= 7 && part.StartsWith("***") && part.EndsWith("***"))
                {
                    var italic = new Italic();
                    italic.Inlines.Add(new Run { Text = part.Substring(3, part.Length - 6) });
                    var bold = new Bold();
                    bold.Inlines.Add(italic);
                    inlines.Add(bold);
                }
                // 太字 (**text**)
                else if (part.Length >= 5 && part.StartsWith("**") && part.EndsWith("**"))
                {
                    inlines.Add(new Bold { Inlines = { new Run { Text = part.Substring(2, part.Length - 4) } } });
                }
                // 斜体 (*text*) - ここで箇条書きの "* " は除外されている
                else if (part.Length >= 3 && part.StartsWith("*") && part.EndsWith("*"))
                {
                    inlines.Add(new Italic { Inlines = { new Run { Text = part.Substring(1, part.Length - 2) } } });
                }
                // インラインコード (`text`)
                else if (part.Length >= 3 && part.StartsWith("`") && part.EndsWith("`"))
                {
                    inlines.Add(new Run
                    {
                        Text = " " + part.Substring(1, part.Length - 2) + " ",
                        FontFamily = new FontFamily("Consolas"),
                        Foreground = (Brush)Application.Current.Resources["SystemFillColorAttentionBrush"]
                    });
                }
                // 通常テキスト (箇条書きの "*" などを含む)
                else
                {
                    inlines.Add(new Run { Text = part });
                }
            }
        }
    }
}