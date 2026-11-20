using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace OrderedWork.Core
{
    /// <summary>
    /// One JSON file per record, with a temp file, backup and schema validation.
    /// The caller owns the directory, schema version, normalization and id policy.
    /// </summary>
    public sealed class AtomicJsonRecordStore<T> where T : class
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string directory;
        private readonly Func<T, int> schemaVersion;
        private readonly int minimumSchema;
        private readonly int maximumSchema;
        private readonly Action<T> normalize;
        private readonly Action<T> beforeSave;
        private readonly Action<T, string> afterLoad;
        private readonly Func<T, string> id;
        private readonly Func<string, string> normalizeId;
        private readonly Func<T, string> sortKey;
        private readonly bool descending;
        private readonly Action<string> warning;
        private readonly Action<string> error;

        public AtomicJsonRecordStore(
            string directory,
            Func<T, int> schemaVersion,
            int minimumSchema,
            int maximumSchema,
            Action<T> normalize,
            Action<T> beforeSave,
            Action<T, string> afterLoad,
            Func<T, string> id,
            Func<string, string> normalizeId,
            Func<T, string> sortKey,
            bool descending,
            Action<string> warning,
            Action<string> error)
        {
            this.directory = directory;
            this.schemaVersion = schemaVersion;
            this.minimumSchema = minimumSchema;
            this.maximumSchema = maximumSchema;
            this.normalize = normalize;
            this.beforeSave = beforeSave;
            this.afterLoad = afterLoad;
            this.id = id;
            this.normalizeId = normalizeId;
            this.sortKey = sortKey;
            this.descending = descending;
            this.warning = warning;
            this.error = error;
        }

        public string DirectoryPath
        {
            get { return directory; }
        }

        public string PathFor(string value)
        {
            return Path.Combine(directory, normalizeId(value) + ".json");
        }

        public List<T> LoadAll()
        {
            List<T> result = new List<T>();
            try
            {
                Directory.CreateDirectory(directory);
                foreach (string path in Directory.GetFiles(directory, "*.json"))
                {
                    T? value = TryLoad(path);
                    if (value != null)
                    {
                        result.Add(value);
                    }
                }
            }
            catch (Exception exception)
            {
                EmitWarning("Could not list records: " + exception.Message);
            }
            IOrderedEnumerable<T> ordered = descending
                ? result.OrderByDescending(sortKey)
                : result.OrderBy(sortKey);
            return ordered.ToList();
        }

        public T? TryLoadById(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }
            return TryLoad(PathFor(value));
        }

        public T? TryLoad(string path)
        {
            T? value = TryLoadFile(path);
            if (value != null)
            {
                return value;
            }

            string backup = path + ".bak";
            if (File.Exists(backup))
            {
                value = TryLoadFile(backup);
                if (value != null)
                {
                    try
                    {
                        File.Copy(backup, path, true);
                        EmitWarning("Recovered record from backup and repaired main file: " + path);
                    }
                    catch (Exception exception)
                    {
                        EmitWarning("Recovered record from backup but could not repair main file: " + exception.Message);
                    }
                    return value;
                }
            }
            return null;
        }

        private T? TryLoadFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }
                T? value = JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions);
                if (value == null
                    || schemaVersion(value) < minimumSchema
                    || schemaVersion(value) > maximumSchema)
                {
                    EmitWarning("Ignoring record with unsupported schema: " + path);
                    return null;
                }
                normalize?.Invoke(value);
                afterLoad?.Invoke(value, path);
                return value;
            }
            catch (Exception exception)
            {
                EmitWarning("Could not read record " + path + ": " + exception.Message);
                return null;
            }
        }

        public bool Save(T value)
        {
            if (value == null)
            {
                return false;
            }
            try
            {
                beforeSave?.Invoke(value);
                normalize?.Invoke(value);
                Directory.CreateDirectory(directory);
                string path = PathFor(id(value));
                if (File.Exists(path))
                {
                    if (TryLoadFile(path) != null)
                    {
                        File.Copy(path, path + ".bak", true);
                    }
                    else
                    {
                        EmitWarning("Keeping existing backup because main record is invalid: " + path);
                    }
                }
                string temp = path + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(value, JsonOptions));
                if (File.Exists(path))
                {
                    File.Replace(temp, path, null);
                }
                else
                {
                    File.Move(temp, path);
                }
                return true;
            }
            catch (Exception exception)
            {
                EmitError("Could not save record: " + exception);
                return false;
            }
        }

        public bool Delete(string value)
        {
            try
            {
                string path = PathFor(value);
                if (!File.Exists(path))
                {
                    return false;
                }
                if (File.Exists(path + ".bak"))
                {
                    File.Delete(path + ".bak");
                }
                File.Delete(path);
                return true;
            }
            catch (Exception exception)
            {
                EmitWarning("Could not delete record: " + exception.Message);
                return false;
            }
        }

        public static string SanitizeFileName(string value, string fallback, bool replaceInvalidWithUnderscore)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return fallback;
            }
            char[] invalid = Path.GetInvalidFileNameChars();
            string clean = new string(value
                .Where(c => replaceInvalidWithUnderscore
                    ? !invalid.Contains(c) || c == '_'
                    : !invalid.Contains(c) && c != '.' && c != '\\' && c != '/')
                .ToArray());
            if (replaceInvalidWithUnderscore)
            {
                clean = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray());
            }
            return string.IsNullOrEmpty(clean) ? fallback : clean;
        }

        private void EmitWarning(string message)
        {
            warning?.Invoke("[Ordered Work] " + message);
        }

        private void EmitError(string message)
        {
            error?.Invoke("[Ordered Work] " + message);
        }
    }
}
