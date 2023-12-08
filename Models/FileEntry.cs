namespace StoreIT.Models
{
    public class FileEntry
    {
        public Guid Id { get; set; }
        public string ChatID { get; set; }
        public string FileName { get; set; }
        public int Parts { get; set; }
        public int ChunkSize { get; set; }

        public FileEntry(string chatID, string fileName, int parts, int chunkSize) {
            ChatID = chatID;
            FileName = fileName;
            Parts = parts;
            ChunkSize = chunkSize;
        }
    }
}