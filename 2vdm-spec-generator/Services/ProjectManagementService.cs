using _2vdm_spec_generator.ViewModel;
using Markdig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

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
        public string SelectedFolderPath { get; private set; } = string.Empty;
        public string SelectedFilePath { get; private set; } = string.Empty;
        public string MarkdownText { get; private set; } = string.Empty;
        public Dictionary<string, string> ScreenIndex { get; private set; }
            = new (StringComparer.OrdinalIgnoreCase);

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

        public void EnsurePositionsJsonExists(string mdPath, IEnumerable<GuiElement> elements)
            => _positionStore.EnsureExists(mdPath, elements);

        public void SavePositions(string mdPath, IEnumerable<GuiElement> elements)
            => _positionStore.SaveAll(mdPath, elements);
        public void ApplyPositions(string mdPath, IList<GuiElement> elements)
            => _positionStore.ApplyPositions(mdPath, elements);
        public void AddOrUpdatePosition(string mdPath, string name, float x, float y)
            => _positionStore.AddOrUpdatePositionEntry(mdPath, name, x, y);

        public void RenamePosition(string mdPath, string oldName, string newName)
            => _positionStore.RenamePositionEntry(mdPath, oldName, newName);
        public void RemovePosition(string mdPath, string name)
            => _positionStore.RemoveEntry(mdPath, name);

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

        // フォルダ探索（FolderItems と ScreenIndex を生成）
        public FolderLoadResult BuildFolderItems(string selectedFolderPath)
        {
            var folderItems = new ObservableCollection<FolderItem>();
            var screenIndex = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            SelectedFolderPath = selectedFolderPath ?? string.Empty;
            if (string.IsNullOrWhiteSpace(selectedFolderPath) || !Directory.Exists(selectedFolderPath))
            {
                ScreenIndex = screenIndex;
                return new FolderLoadResult(folderItems, screenIndex);
            }

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

            ScreenIndex = screenIndex;
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

        // ファイル読込（正規化→VDM→UI要素→positions反映→表示フラグ）
        public FileLoadResult LoadFile(string selectedFolderPath, string mdPath, FolderItem selectedItemOrNull)
        {
            if (!string.IsNullOrWhiteSpace(selectedFolderPath))
                SelectedFolderPath = selectedFolderPath;

            SelectedFilePath = mdPath ?? string.Empty;

            var markdown = ReadAndNormalizeMarkdown(mdPath);
            MarkdownText = markdown;
            var vdm = new MarkdownToVdmConverter().ConvertToVdm(markdown);

            bool isClassAdd, isScreenListAdd, isClassAll;
            var firstLine = GetFirstNonEmptyLine(markdown);

            if (firstLine.TrimStart().StartsWith("##", StringComparison.OrdinalIgnoreCase))
            {
                isClassAdd = false; 
                isScreenListAdd = false; 
                isClassAll = true;
            }
            else if (firstLine.StartsWith("# 画面一覧", StringComparison.OrdinalIgnoreCase))
            {
                isClassAdd = false; 
                isScreenListAdd = true; 
                isClassAll = false;
            }
            else
            {
                isClassAdd = true; 
                isScreenListAdd = false; 
                isClassAll = false;
            }

            var screenNamesForRenderer = _screenListService.GetScreenNames(selectedFolderPath);

            var elements = new MarkdownToUiConverter().Convert(markdown).ToList();
            _positionStore.ApplyPositions(mdPath, elements);
            _positionStore.EnsureExists(mdPath, elements);

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

         // ファイル更新（Markdown保存 + VDM++保存)
        public (string NormalizedMarkdown, string Vdm) UpdateMarkdownAndVdm(string mdPath, string markdown, IEnumerable<GuiElement> elements)
        {
            if (string.IsNullOrWhiteSpace(mdPath))
                return (string.Empty, string.Empty);

            var normalized = NormalizeMarkdownText(markdown ?? string.Empty);

            Directory.CreateDirectory(Path.GetDirectoryName(mdPath) ?? string.Empty);
            File.WriteAllText(mdPath, normalized, Encoding.UTF8);

            var vdm = new MarkdownToVdmConverter().ConvertToVdm(normalized);
            File.WriteAllText(Path.ChangeExtension(mdPath, ".vdmpp"), vdm, Encoding.UTF8);

            // CTM要素配置データの保存（無い場合は新規作成）
            _positionStore.SaveAll(mdPath, elements ?? Array.Empty<GuiElement>());

            if(string.Equals(mdPath, SelectedFilePath, StringComparison.OrdinalIgnoreCase))
                MarkdownText = normalized;

            return (normalized, vdm);
        }

        // 画面一覧など、VDM++生成が不要なケース向け
        public string UpdateMarkdownOnly(string mdPath, string markdown)
        {
            if (string.IsNullOrWhiteSpace(mdPath))
                return string.Empty;

            var normalized = NormalizeMarkdownText(markdown ?? string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(mdPath) ?? string.Empty);
            File.WriteAllText(mdPath, normalized, Encoding.UTF8);
            return normalized;
        }

                public string UpdateVdmOnly(string mdPath, string markdown)
        {
            if (string.IsNullOrWhiteSpace(mdPath))
                return string.Empty;

            var normalized = NormalizeMarkdownText(markdown ?? string.Empty);
            Directory.CreateDirectory(Path.GetDirectoryName(mdPath) ?? string.Empty);
            var vdm = new MarkdownToVdmConverter().ConvertToVdm(normalized);
            File.WriteAllText(Path.ChangeExtension(mdPath, ".vdmpp"), vdm, Encoding.UTF8);
            return vdm;
        }


        public string CreateNewMarkdownFile(string targetDir, string fileName, string initialContent)
        {
            if (string.IsNullOrWhiteSpace(targetDir))
                throw new ArgumentException("targetDir is null or empty", nameof(targetDir));
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName is null or empty", nameof(fileName));

            Directory.CreateDirectory(targetDir);

            var name = fileName.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
                ? fileName
                : fileName + ".md";

            var path = Path.Combine(targetDir, name);
            File.WriteAllText(path, initialContent ?? string.Empty, Encoding.UTF8);

            // 内部状態（SelectedFilePath）の更新
            SelectedFilePath = path;
            
            // 索引辞書の更新（最低限ファイル名で引けるようにする）
            var byName = Path.GetFileNameWithoutExtension(path);
                        if (!string.IsNullOrWhiteSpace(byName))
                ScreenIndex[byName] = path;
            return path;
        }

        public bool TryGetMarkdownPathByScreenName(string screenName, out string mdPath)
        {
            mdPath = string.Empty;
            if (string.IsNullOrWhiteSpace(screenName)) return false;
            return ScreenIndex.TryGetValue(screenName.Trim(), out mdPath);
        }

        /// <summary>
        /// 画面名に対応する Markdown 仕様ファイルパスを決定する
        /// 優先順位：
        /// 1) ScreenIndex（辞書）
        /// 2) SelectedFolderPath 配下の再帰探索（先頭5行の見出し一致）
        /// 見つかった場合は ScreenIndex を自己変更する
        /// </summary>
        public bool TryResolveScreenMarkdownPath(string screenName, out string mdPath)
        {
            mdPath = string.Empty;
            if (string.IsNullOrWhiteSpace(screenName)) return false;

            var key = screenName.Trim();

            // 1) 辞書
            if (ScreenIndex.TryGetValue(key, out var hit) && !string.IsNullOrWhiteSpace(hit) && File.Exists(hit))
            {
                mdPath = hit;
                return true;
            }

            // 2) 再帰探索
            if (string.IsNullOrWhiteSpace(SelectedFolderPath) || !Directory.Exists(SelectedFolderPath))
                return false;

            string[] mdFiles;
            try
            {
                mdFiles = Directory.GetFiles(SelectedFolderPath, "*.md", SearchOption.AllDirectories);
            }
            catch
            {
                return false;
            }

            // 3) 見出し一致（先頭5行）
            foreach (var f in mdFiles)
            {
                try
                {
                    foreach (var line in File.ReadLines(f).Take(5))
                    {
                        var t = (line ?? string.Empty).Trim();
                        if (t.Length == 0) continue;

                        if (t.StartsWith("# ") || t.StartsWith("## "))
                        {
                                // 先頭の # / ## を除去して純粋な見出し文字列を取得
                            var heading = t.TrimStart('#').Trim();
                            
                            if (string.Equals(heading, key, StringComparison.OrdinalIgnoreCase))
                            {
                                mdPath = f;
                                
                            
                                ScreenIndex[key] = f;
                                
                                var fileNameKey = Path.GetFileNameWithoutExtension(f);
                                if (!string.IsNullOrWhiteSpace(fileNameKey))
                                ScreenIndex[fileNameKey] = f;
                                
                                IndexFromFileHeading(ScreenIndex, f);
                                return true;
                            }
                        }
                    }
                }
                catch
                {
                    // 読めないファイルはスキップ
                }
            }

            return false;
        }

        /// <summary>
        /// 画面切り替え処理。
        /// 画面名から Markdown を解決し、ファイル読込処理を呼び出して FileLoadResult を返す。
        /// </summary>
        public bool TrySwitchScreen(string screenName, FolderItem selectedItemOrNull, out string mdPath, out FileLoadResult result)
        {
            mdPath = string.Empty;
            result = null;

            if (!TryResolveScreenMarkdownPath(screenName, out var path))
                return false;

            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            mdPath = path;
            result = LoadFile(SelectedFolderPath, path, selectedItemOrNull);
            return true;
        }

    }
}
