using System.Text.Json;

namespace FinSys2.Services
{
    public class JsonDatabase<T>
    {
        private readonly string _filePath;

        public JsonDatabase(string fileName)
        {
            string directoryPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data"); //Path.Combine(env.ContentRootPath, "Data");

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            _filePath = Path.Combine(directoryPath, fileName);

            if (!File.Exists(_filePath))
            {
                File.WriteAllText(_filePath, "[]");
            }
        }

        public List<T> GetAll()
        {
            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
            catch { return new List<T>(); }
        }

        public void SaveAll(List<T> items)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(items, options);
            File.WriteAllText(_filePath, json);
        }
    }
}