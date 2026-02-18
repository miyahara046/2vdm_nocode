using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using _2vdm_spec_generator.ViewModel;

namespace _2vdm_spec_generator.Services
{
    /// <summary>
    /// プロジェクト管理
    /// フォルダの読み込み、更新などを処理する
    /// </summary>
    internal class ProjectManagementService
    {
        private readonly GuiPositionStore _positionStore = new();
        private readonly ScreenListService _screenListService = new();

        private static readonly Regex BulletNormalizeRegex =
            new Regex(@"^(?<indent>\s*)(?:\*|•|⦁)\s+", RegexOptions.Compiled);

        public sealed record FolderLoadResult(
            ObservableCollection<FolderItem> Items,
            Dictionary<string, string> ScreenIndex
        );

        public sealed record FileLoadResult(
            string Markdown,
            string Vdm,
            IReadOnlyList<GuiElement> Elements,
            string DiagramTitle,
            bool IsClassAddButtonVisible,
            bool IsScreenListAddButtonVisible,
            bool IsClassAllButtonVisible,
            IEnumerable<string> ScreenNamesForRenderer
        );

        private static string NormalizeMarkdownText(string markdown)
        {
            if (markdown == null) return string.Empty;

            if (markdown.Length > 0 && markdown[0] == '\uFEFF')
                markdown = markdown[1..];

            markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");
            markdown = markdown.Replace('\u00A0', ' ');

            var lines = markdown.Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = BulletNormalizeRegex.Replace(lines[i], m => $"{m.Groups["indent"].Value}- ");

            return string.Join("\n", lines);
        }

        private static string ReadAndNormalizeMarkdown(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return string.Empty;

            using var sr = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            return NormalizeMarkdownText(sr.ReadToEnd());
        }

        private static string GetFirstNonEmptyLine(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;
            foreach (var l in text.Split('\n'))
            {
                var t = (l ?? string.Empty).Trim();
                if (t.Length != 0) return t;
            }
            return string.Empty;
        }

        // 4.3.2 相当：フォルダ探索（FolderItems と ScreenIndex を生成）
        public FolderLoadResult BuildFolderItems(string selectedFolderPath)
        {
            var folderItems = new ObservableCollection<FolderItem>();
            var screenIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(selectedFolderPath) || !Directory.Exists(selectedFolderPath))
                return new FolderLoadResult(folderItems, screenIndex);

            var root = new FolderItem
            {
                Name = Path.GetFileName(selectedFolderPath),
                FullPath = selectedFolderPath,
                Level = -1,
                IsExpanded = true,
                IsVisible = true
            };
            folderItems.Add(root);

            foreach (var dir in Directory.GetDirectories(selectedFolderPath))
                AddFolderRecursive(folderItems, screenIndex, dir, level: 0);

            foreach (var file in Directory.GetFiles(selectedFolderPath)
                     .Where(f => string.Equals(Path.GetExtension(f), ".md", StringComparison.OrdinalIgnoreCase)))
                folderItems.Add(CreateMarkdownFileItem(screenIndex, file, level: 0));

            return new FolderLoadResult(folderItems, screenIndex);
        }

        private static void AddFolderRecursive(ObservableCollection<FolderItem> folderItems, Dictionary<string, string> screenIndex, string path, int level)
        {
            folderItems.Add(new FolderItem
            {
                Name = Path.GetFileName(path),
                FullPath = path,
                Level = level,
                IsExpanded = false
            });

            foreach (var file in Directory.GetFiles(path)
                     .Where(f => string.Equals(Path.GetExtension(f), ".md", StringComparison.OrdinalIgnoreCase)))
                folderItems.Add(CreateMarkdownFileItem(screenIndex, file, level: level + 1));

            foreach (var dir in Directory.GetDirectories(path))
                AddFolderRecursive(folderItems, screenIndex, dir, level + 1);
        }

        private static FolderItem CreateMarkdownFileItem(Dictionary<string, string> screenIndex, string filePath, int level)
        {
            var item = new FolderItem
            {
                Name = Path.GetFileName(filePath),
                FullPath = filePath,
                Level = level
            };

            var byName = Path.GetFileNameWithoutExtension(filePath);
            if (!string.IsNullOrWhiteSpace(byName))
                screenIndex[byName] = filePath;

            IndexFromFileHeading(screenIndex, filePath);
            return item;
        }

        private static void IndexFromFileHeading(Dictionary<string, string> screenIndex, string mdPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(mdPath) || !File.Exists(mdPath)) return;

                foreach (var line in File.ReadLines(mdPath).Take(30))
                {
                    var t = (line ?? "").Trim();
                    if (t.Length == 0) continue;

                    if (t.StartsWith("## ", StringComparison.Ordinal))
                    {
                        var name = t[3..].Trim();
                        if (!string.IsNullOrWhiteSpace(name)) screenIndex[name] = mdPath;
                        break;
                    }
                    if (t.StartsWith("# ", StringComparison.Ordinal))
                    {
                        var name = t[2..].Trim();
                        if (!string.Equals(name, "画面一覧", StringComparison.OrdinalIgnoreCase) &&
                            !string.IsNullOrWhiteSpace(name))
                        {
                            screenIndex[name] = mdPath;
                        }
                        break;
                    }
                }
            }
            catch { }
        }

        // 4.3.3 相当：ファイル読込（正規化→VDM→UI要素→positions反映→表示フラグ）
        public FileLoadResult LoadFile(string selectedFolderPath, string mdPath, FolderItem selectedItemOrNull)
        {
            var markdown = ReadAndNormalizeMarkdown(mdPath);
            var vdm = new MarkdownToVdmConverter().ConvertToVdm(markdown);

            bool isClassAdd, isScreenListAdd, isClassAll;
            var firstLine = GetFirstNonEmptyLine(markdown);

            if (firstLine.TrimStart().StartsWith("##", StringComparison.OrdinalIgnoreCase))
            {
                isClassAdd = false; isScreenListAdd = false; isClassAll = true;
            }
            else if (firstLine.StartsWith("# 画面一覧", StringComparison.OrdinalIgnoreCase))
            {
                isClassAdd = false; isScreenListAdd = true; isClassAll = false;
            }
            else
            {
                isClassAdd = true; isScreenListAdd = false; isClassAll = false;
            }

            var screenNamesForRenderer = _screenListService.GetScreenNames(selectedFolderPath);

            var elements = new MarkdownToUiConverter().Convert(markdown).ToList();
            _positionStore.ApplyPositions(mdPath, elements);

            var title = ExtractDiagramTitleFromMarkdown(markdown, selectedItemOrNull, mdPath);

            return new FileLoadResult(
                Markdown: markdown,
                Vdm: vdm,
                Elements: elements,
                DiagramTitle: title,
                IsClassAddButtonVisible: isClassAdd,
                IsScreenListAddButtonVisible: isScreenListAdd,
                IsClassAllButtonVisible: isClassAll,
                ScreenNamesForRenderer: screenNamesForRenderer
            );
        }

        private static string ExtractDiagramTitleFromMarkdown(string markdown, FolderItem fileItem, string fallbackPath)
        {
            const string defaultTitle = "Condition Transition Map";

            if (string.IsNullOrWhiteSpace(markdown))
                return Path.GetFileNameWithoutExtension(fileItem?.FullPath ?? fallbackPath) ?? defaultTitle;

            foreach (var l in markdown.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var t = l?.Trim();
                if (string.IsNullOrEmpty(t)) continue;

                if (t.StartsWith("# ") || t.StartsWith("## "))
                {
                    var title = t.TrimStart('#').Trim();
                    if (string.Equals(title, "画面一覧", StringComparison.OrdinalIgnoreCase))
                        return defaultTitle;
                    return title;
                }
            }

            return Path.GetFileNameWithoutExtension(fileItem?.FullPath ?? fallbackPath) ?? defaultTitle;
        }
    }
}
