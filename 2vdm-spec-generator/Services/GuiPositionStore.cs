using _2vdm_spec_generator.ViewModel;
using Markdig.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace _2vdm_spec_generator.Services
{
    /// <summary>
    /// .positions.json の読み書きを担当する。
    /// ViewModel から JSON I/O を剥がし、位置情報永続化の責務を集約する。
    /// </summary>
    internal sealed class GuiPositionStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public void ApplyPositions(string markdownPath, IList<GuiElement> elements)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(markdownPath) || elements == null || elements.Count == 0) return;

                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
                if (!File.Exists(posPath)) return;

                var json = File.ReadAllText(posPath);
                var list = JsonSerializer.Deserialize<List<GuiElementPosition>>(json);
                if (list == null || list.Count == 0) return;

                foreach (var pos in list)
                {
                    var el = elements.FirstOrDefault(e => string.Equals(e.Name, pos.Name, StringComparison.Ordinal));
                    if (el != null)
                    {
                        el.X = pos.X;
                        el.Y = pos.Y;


                    }

                }
            }
            catch
            {
            }
        }
        public void AddOrUpdatePositionEntry(string markdownPath, string name, float x, float y)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(markdownPath) || string.IsNullOrWhiteSpace(name)) return;

                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
                var list = Read(posPath);

                var found = list.FirstOrDefault(p => string.Equals((p.Name ?? string.Empty).Trim(), name.Trim(), StringComparison.Ordinal));
                if (found == null)
                {
                    list.Add(new GuiElementPosition { Name = name.Trim(), X = x, Y = y });
                }
                else
                {
                    found.X = x;
                    found.Y = y;
                }

                Write(posPath, list);
            }
            catch
            {
                // 保存失敗は黙殺（UI操作の妨げにしない）
            }
        }

        public void RenamePositionEntry(string markdownPath, string oldName, string newName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(markdownPath)) return;
                if (string.IsNullOrWhiteSpace(oldName) || string.IsNullOrWhiteSpace(newName)) return;

                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
                if (!File.Exists(posPath)) return;

                var list = Read(posPath);
                var found = list.FirstOrDefault(p => string.Equals((p.Name ?? string.Empty).Trim(), oldName.Trim(), StringComparison.Ordinal));
                if (found == null) return;

                found.Name = newName.Trim();
                Write(posPath, list);
            }
            catch
            {
            }
        }

        private static List<GuiElementPosition> Read(string posPath)
        {
            if (!File.Exists(posPath)) return new List<GuiElementPosition>();

            var json = File.ReadAllText(posPath);
            return JsonSerializer.Deserialize<List<GuiElementPosition>>(json) ?? new List<GuiElementPosition>();
        }

        private static void Write(string posPath, List<GuiElementPosition> list)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(posPath) ?? string.Empty);
            File.WriteAllText(posPath, JsonSerializer.Serialize(list, JsonOptions), Encoding.UTF8);
        }

        // positions.json が無い場合のみ作成する（初回生成）
        public void EnsureExists(string markdownPath, IEnumerable<GuiElement> elements)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(markdownPath) || elements == null) return;

                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
                if (File.Exists(posPath)) return;

                var list = elements
                    .Where(e => e != null && !string.IsNullOrWhiteSpace(e.Name))
                    .Select(e => new GuiElementPosition { Name = e.Name, X = e.X, Y = e.Y })
                    .ToList();

                if (list.Count == 0) return;
                Write(posPath, list);
            }
            catch { }
        }

        // 要素一覧を positions.json に丸ごと保存する（要素が無ければ positions.json を削除）
        public void SaveAll(string markdownPath, IEnumerable<GuiElement> elements)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(markdownPath) || elements == null) return;
        
                var list = elements
                            .Where(e => e != null && !string.IsNullOrWhiteSpace(e.Name))
                            .Select(e => new GuiElementPosition { Name = e.Name, X = e.X, Y = e.Y })
                            .ToList();
        
                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
        
                if (list.Count == 0)
                {
                try {if (File.Exists(posPath)) File.Delete(posPath);} catch { }
                return;
                }
        
                Write(posPath, list);
            }
            catch { }
        }

        // 1件だけ削除する（空になったら positions.json を削除）
        public void RemoveEntry(string markdownPath, string name)
        {
            try
            {
            
                if (string.IsNullOrWhiteSpace(markdownPath) || string.IsNullOrWhiteSpace(name)) return;
        
                var posPath = Path.ChangeExtension(markdownPath, ".positions.json");
                if (!File.Exists(posPath)) return;
        
                var list = Read(posPath);
                var trimmed = name.Trim();
                var newList = list
                            .Where(p => !string.Equals((p.Name ?? string.Empty).Trim(), trimmed, StringComparison.Ordinal))
                            .ToList();
        
                if (newList.Count == 0)
                {
                     try { File.Delete(posPath); } catch { }
                }
                else
                {
                    Write(posPath, newList);
                }
            }
            catch { }

        }
    }
}
