using System;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using AutoWizard.Core.Models;

namespace AutoWizard.Core.Resources
{
    /// <summary>
    /// .aws 封裝格式管理器 (AutoWizard Script)
    /// 包含腳本 JSON + 圖片資源的 ZIP 封裝
    /// </summary>
    public class AwsPackage
    {
        /// <summary>
        /// 共用序列化選項，確保 Save/Load 使用一致的設定
        /// </summary>
        private static readonly JsonSerializerOptions SharedJsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public string ScriptName { get; set; } = "未命名腳本";
        public List<BaseAction> Actions { get; set; } = new();
        public List<VariableDefinition> Variables { get; set; } = new();
        public Dictionary<string, byte[]> ImageResources { get; set; } = new();

        /// <summary>
        /// 儲存為 .aws 檔案
        /// </summary>
        public void Save(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            Save(fs);
        }

        /// <summary>
        /// 儲存至 Stream (用於記憶體或附加資料)
        /// </summary>
        public void Save(Stream outStream)
        {
            using var archive = new ZipArchive(outStream, ZipArchiveMode.Create, leaveOpen: true);

            // 1. 儲存腳本 JSON
            var scriptEntry = archive.CreateEntry("script.json");
            using (var writer = new StreamWriter(scriptEntry.Open()))
            {
                var json = JsonSerializer.Serialize(Actions, SharedJsonOptions);
                writer.Write(json);
            }

            // 2. 儲存中繼資料
            var metadataEntry = archive.CreateEntry("metadata.json");
            using (var writer = new StreamWriter(metadataEntry.Open()))
            {
                var metadata = new
                {
                    Name = ScriptName,
                    CreatedAt = DateTime.Now,
                    Version = "1.0",
                    ActionCount = Actions.Count,
                    ImageCount = ImageResources.Count
                };
                var json = JsonSerializer.Serialize(metadata, SharedJsonOptions);
                writer.Write(json);
            }

            // 3. 儲存變數定義
            var variablesEntry = archive.CreateEntry("variables.json");
            using (var writer = new StreamWriter(variablesEntry.Open()))
            {
                var json = JsonSerializer.Serialize(Variables, SharedJsonOptions);
                writer.Write(json);
            }

            // 4. 儲存圖片資源
            foreach (var kvp in ImageResources)
            {
                var imageEntry = archive.CreateEntry($"images/{kvp.Key}");
                using var stream = imageEntry.Open();
                stream.Write(kvp.Value, 0, kvp.Value.Length);
            }
        }

        /// <summary>
        /// 从 .aws 档案载入
        /// </summary>
        public static AwsPackage Load(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return Load(fs);
        }

        /// <summary>
        /// 从 Stream 载入 (用於記憶體或附加資料)
        /// </summary>
        public static AwsPackage Load(Stream inStream)
        {
            var package = new AwsPackage();

            using var archive = new ZipArchive(inStream, ZipArchiveMode.Read, leaveOpen: true);

            // 1. 讀取腳本 JSON
            var scriptEntry = archive.GetEntry("script.json");
            if (scriptEntry != null)
            {
                using var reader = new StreamReader(scriptEntry.Open());
                var json = reader.ReadToEnd();
                try
                {
                    package.Actions = JsonSerializer.Deserialize<List<BaseAction>>(json, SharedJsonOptions) ?? new();
                }
                catch (Exception ex) when (ex is JsonException || ex is NotSupportedException)
                {
                    // 檢查是否為舊格式（缺少 $type 辨別碼）
                    if (!json.Contains("\"$type\""))
                    {
                        throw new InvalidOperationException(
                            "此腳本使用舊版格式儲存（缺少型別資訊），無法直接開啟。" +
                            "請使用新版程式重新建立並儲存腳本。");
                    }
                    throw; // 其他 JSON 錯誤直接拋出
                }
            }

            // 2. 讀取中繼資料
            var metadataEntry = archive.GetEntry("metadata.json");
            if (metadataEntry != null)
            {
                using var reader = new StreamReader(metadataEntry.Open());
                var json = reader.ReadToEnd();
                var metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                if (metadata != null && metadata.ContainsKey("Name"))
                {
                    package.ScriptName = metadata["Name"].ToString() ?? "未命名腳本";
                }
            }

            // 3. 讀取變數定義
            var variablesEntry = archive.GetEntry("variables.json");
            if (variablesEntry != null)
            {
                using var reader2 = new StreamReader(variablesEntry.Open());
                var json2 = reader2.ReadToEnd();
                package.Variables = JsonSerializer.Deserialize<List<VariableDefinition>>(json2, SharedJsonOptions) ?? new();
            }

            // 4. 讀取圖片資源
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("images/"))
                {
                    using var stream = entry.Open();
                    using var ms = new MemoryStream();
                    stream.CopyTo(ms);
                    var imageName = Path.GetFileName(entry.FullName);
                    package.ImageResources[imageName] = ms.ToArray();
                }
            }

            return package;
        }

        /// <summary>
        /// 添加圖片資源
        /// </summary>
        public void AddImageResource(string name, byte[] imageData)
        {
            ImageResources[name] = imageData;
        }

        /// <summary>
        /// 取得圖片資源
        /// </summary>
        public byte[]? GetImageResource(string name)
        {
            return ImageResources.TryGetValue(name, out var data) ? data : null;
        }
    }
}
