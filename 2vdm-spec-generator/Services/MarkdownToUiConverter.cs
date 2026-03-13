using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using _2vdm_spec_generator.ViewModel;

namespace _2vdm_spec_generator.Services
{
    /// <summary>
    /// Markdown 仕様を解析し、CTM編集の基礎となる GUIElement 群を生成する。
    /// 「画面一覧仕様」と「画面仕様（有効ボタン一覧／イベント一覧）」を対象に elements を生成する。
    /// </summary>
    internal class MarkdownToUiConverter
    {
        // 箇条書き（- / * / ・ / • / ⦁）の本文を取り出す
        private static readonly Regex BulletPattern =
            new Regex(@"^\s*(?:-|\*|・|•|⦁)\s+(?<Text>.+?)\s*$", RegexOptions.Compiled);

        // "左 → 右"
        private static readonly Regex ArrowPattern =
            new Regex(@"^(?<Left>.*?)\s*→\s*(?<Right>.*)$", RegexOptions.Compiled);

        // "ボタン 押下 → xxx"（ボタン名部分は空白を許容）
        private static readonly Regex ButtonOperationPattern =
            new Regex(@"^(?<Button>.*?)(?<Trigger>押下)\s*→\s*(?<Right>.*)$", RegexOptions.Compiled);

        private static bool TryGetBulletText(string line, out string text)
        {
            text = string.Empty;
            if (string.IsNullOrWhiteSpace(line)) return false;

            var m = BulletPattern.Match(line.TrimEnd('\r'));
            if (!m.Success) return false;

            text = (m.Groups["Text"].Value ?? string.Empty).Trim();
            return text.Length > 0;
        }

        private static bool TrySplitArrow(string text, out string left, out string right)
        {
            left = string.Empty;
            right = string.Empty;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var m = ArrowPattern.Match(text.Trim());
            if (!m.Success) return false;

            left = (m.Groups["Left"].Value ?? string.Empty).Trim();
            right = (m.Groups["Right"].Value ?? string.Empty).Trim();
            return true;
        }

        private static bool IsHeading(string line, string headingName)
        {
            var t = (line ?? string.Empty).Trim();
            if (t == headingName) return true;

            if (t.StartsWith("#"))
            {
                var name = t.TrimStart('#').Trim();
                return string.Equals(name, headingName, StringComparison.Ordinal);
            }
            return false;
        }

        private static string NormalizeToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            // 卒論の記述に合わせ、空白は取り除いて同一視する（日本語仕様での揺れ吸収）
            return Regex.Replace(s.Trim(), "\\s+", "");
        }

        /// <summary>
        /// 初期配置。
        /// （厳密な数式再現ではなく、列構造と対応関係の維持を優先）
        /// </summary>
        private void ArrangeElements(List<GuiElement> elements)
        {
            const float leftX = 40f;
            const float midX = 260f;

            const float topMargin = 10f;
            const float spacingY = 80f;
            const float noTimeoutBaseY = 40f;

            // サイズ調整（Timeout は楕円扱いなので幅を小さくする）
            foreach (var e in elements)
            {
                if (e.Type == GuiElementType.Timeout)
                {
                    e.Width = 160f * 0.7f;
                    e.Height = 45f;
                    e.IsMovable = false;
                }
                else
                {
                    e.Width = 160f;
                    e.Height = 45f;
                    // movable は生成側で必要に応じて true にする
                }
            }

            var timeout = elements.FirstOrDefault(e => e.Type == GuiElementType.Timeout);

            if (timeout != null)
            {
                timeout.X = leftX;
                timeout.Y = topMargin;
            }

            // Screen は左列（Timeout があっても同列に置く）
            int iScreen = 0;
            foreach (var s in elements.Where(e => e.Type == GuiElementType.Screen))
            {
                s.X = leftX;
                s.Y = topMargin + (iScreen * spacingY);
                s.IsMovable = true;
                iScreen++;
            }

            // Button は Timeout の下から開始（無い場合は固定値）
            float buttonBaseY = timeout != null ? (timeout.Y + timeout.Height + 10f) : noTimeoutBaseY;
            int iButton = 0;
            foreach (var b in elements.Where(e => e.Type == GuiElementType.Button))
            {
                b.X = leftX;
                b.Y = buttonBaseY + (iButton * spacingY);
                b.IsMovable = true;
                iButton++;
            }

            // Event は「関連する Button/Timeout と同じ Y」に寄せる（無ければ末尾へ）
            float orphanBaseY = buttonBaseY + (iButton * spacingY) + spacingY;
            int iOrphan = 0;

            foreach (var ev in elements.Where(e => e.Type == GuiElementType.Event))
            {
                ev.IsMovable = true;

                // Timeout に紐づく Event（ev.Target が timeout.Name）
                if (timeout != null
                    && !string.IsNullOrWhiteSpace(ev.Target)
                    && string.Equals(NormalizeToken(ev.Target), NormalizeToken(timeout.Name), StringComparison.Ordinal))
                {
                    ev.X = midX;
                    ev.Y = timeout.Y;
                    continue;
                }

                // Button に紐づく Event（button.Target == ev.Name）
                var linkedBtn = elements.FirstOrDefault(b =>
                    b.Type == GuiElementType.Button
                    && !string.IsNullOrWhiteSpace(b.Target)
                    && !string.IsNullOrWhiteSpace(ev.Name)
                    && string.Equals(NormalizeToken(b.Target), NormalizeToken(ev.Name), StringComparison.Ordinal));

                if (linkedBtn != null)
                {
                    ev.X = midX;
                    ev.Y = linkedBtn.Y;
                    continue;
                }

                ev.X = midX;
                ev.Y = orphanBaseY + (iOrphan * spacingY);
                iOrphan++;
            }
        }

        /// <summary>
        /// Markdown を解析し GUIElement を生成する。
        /// - "# 画面一覧" : 画面一覧仕様
        /// - "## xxx" : 画面仕様（有効ボタン一覧／イベント一覧）
        /// </summary>
        public IEnumerable<GuiElement> Convert(string markdown)
        {
            var elements = new List<GuiElement>();
            if (string.IsNullOrWhiteSpace(markdown)) return elements;

            var lines = markdown
                .Replace("\r\n", "\n")
                .Replace("\r", "\n")
                .Split('\n')
                .ToList();

            if (lines.Count == 0) return elements;

            var first = (lines[0] ?? string.Empty).Trim();

            // 画面一覧仕様
            if (string.Equals(first, "# 画面一覧", StringComparison.Ordinal))
            {
                // ヘッダ要素（ツール側の扱いに合わせ、Screen型で「画面一覧」を入れる）
                elements.Add(new GuiElement
                {
                    Type = GuiElementType.Screen,
                    Name = "# 画面一覧",
                    Target = null,
                    Description = string.Empty
                });

                for (int i = 1; i < lines.Count; i++)
                {
                    if (!TryGetBulletText(lines[i], out var screenName)) continue;
                    elements.Add(new GuiElement
                    {
                        Type = GuiElementType.Screen,
                        Name = screenName.Trim(),
                        Target = null,
                        Description = string.Empty
                    });
                }

                ArrangeElements(elements);
                return elements;
            }

            // 画面仕様
            if (!first.StartsWith("## ")) return elements;

            var screenName0 = first.Trim();
            elements.Add(new GuiElement
            {
                Type = GuiElementType.Screen,
                Name = screenName0,
                Target = null,
                Description = string.Empty
            });

            // タイムアウト（2行目の箇条書き: "- xxxでタイムアウト" など）
            string timeoutName = null;
            if (lines.Count > 1 && TryGetBulletText(lines[1], out var timeoutText))
            {
                var idx = timeoutText.IndexOf('で');
                timeoutName = (idx > 0 ? timeoutText.Substring(0, idx) : timeoutText).Trim();

                if (!string.IsNullOrWhiteSpace(timeoutName))
                {
                    elements.Add(new GuiElement
                    {
                        Type = GuiElementType.Timeout,
                        Name = timeoutName,
                        Target = null,
                        Description = string.Empty,
                        IsMovable = false
                    });
                }
            }

            // 有効ボタン一覧
            int buttonHeadingIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsHeading(lines[i], "有効ボタン一覧")) { buttonHeadingIdx = i; break; }
            }

            var buttonNames = new HashSet<string>(StringComparer.Ordinal);
            if (buttonHeadingIdx != -1)
            {
                for (int i = buttonHeadingIdx + 1; i < lines.Count; i++)
                {
                    var t = (lines[i] ?? string.Empty).TrimStart();
                    if (t.StartsWith("###")) break;

                    if (!TryGetBulletText(lines[i], out var btn)) continue;
                    btn = btn.Trim();
                    if (!buttonNames.Add(btn)) continue;

                    elements.Add(new GuiElement
                    {
                        Type = GuiElementType.Button,
                        Name = btn,
                        Target = null,
                        Description = string.Empty
                    });
                }
            }

            // イベント一覧
            int eventHeadingIdx = -1;
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsHeading(lines[i], "イベント一覧")) { eventHeadingIdx = i; break; }
            }

            if (eventHeadingIdx != -1)
            {
                for (int i = eventHeadingIdx + 1; i < lines.Count; i++)
                {
                    if (!TryGetBulletText(lines[i], out var evtText)) continue;

                    // 1) "ボタン 押下 → ..."（分岐は → の右が空）
                    var op = ButtonOperationPattern.Match(evtText);
                    if (op.Success)
                    {
                        var btnName = (op.Groups["Button"].Value ?? string.Empty).Trim();
                        var right = (op.Groups["Right"].Value ?? string.Empty).Trim();

                        var btn = elements.FirstOrDefault(e => e.Type == GuiElementType.Button
                            && string.Equals(e.Name?.Trim(), btnName, StringComparison.Ordinal));
                        if (btn == null) continue;

                        // 分岐（"押下 →"）
                        if (string.IsNullOrWhiteSpace(right))
                        {
                            var branchEventName = $"{btnName}押下";
                            btn.Target = branchEventName;

                            var branchEvent = new GuiElement
                            {
                                Type = GuiElementType.Event,
                                Name = branchEventName,
                                Target = null,
                                Description = string.Empty,
                                Branches = new List<GuiElement.EventBranch>()
                            };

                            int j = i + 1;
                            for (; j < lines.Count; j++)
                            {
                                if (!TryGetBulletText(lines[j], out var branchLine)) break;
                                if (ButtonOperationPattern.IsMatch(branchLine)) break;

                                // 分岐行は "条件 → 遷移先" の形式を期待する
                                if (!TrySplitArrow(branchLine, out var condRaw, out var tarRaw)) break;

                                var bLeft = NormalizeToken(condRaw);
                                if (string.Equals(bLeft, "タイムアウト", StringComparison.Ordinal)) break;

                                var cond = NormalizeToken(condRaw);
                                var tar = NormalizeToken(tarRaw);
                                if (tar.EndsWith("へ", StringComparison.Ordinal))
                                    tar = tar.Substring(0, tar.Length - 1);

                                branchEvent.Branches.Add(new GuiElement.EventBranch
                                {
                                    Condition = cond,
                                    Target = tar
                                });
                            }

                            elements.Add(branchEvent);

                            // 分岐行を読み飛ばす
                            i = j - 1;
                            continue;
                        }

                        // 非分岐: "押下 → 右"
                        btn.Target = right;

                        // Event 要素は「遷移先」や「イベント名」を Name に持たせる（UIToMarkdown側で正とする）
                        elements.Add(new GuiElement
                        {
                            Type = GuiElementType.Event,
                            Name = right,
                            Target = right,
                            Description = string.Empty
                        });
                        continue;
                    }

                    // 2) "タイムアウト → xxx"
                    if (TrySplitArrow(evtText, out var left, out var right2))
                    {
                        left = NormalizeToken(left);
                        right2 = NormalizeToken(right2);

                        if (string.Equals(left, "タイムアウト", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(timeoutName))
                        {
                            elements.Add(new GuiElement
                            {
                                Type = GuiElementType.Event,
                                Name = right2,
                                Target = timeoutName, // Timeout と関連付け
                                Description = string.Empty
                            });
                        }
                    }
                }
            }

            ArrangeElements(elements);
            return elements;
        }
    }
}
