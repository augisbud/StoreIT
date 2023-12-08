namespace StoreIT.Models
{
    public class FileEntry
    {
        public Guid Id { get; set; }
        public string ChatID { get; set; }
        public string FileName { get; set; }
        public int ChunkSize { get; set; }
        public List<string> ExternalFileIds { get; set; }

        public FileEntry(string chatID, string fileName, int chunkSize, List<string> externalFileIds) {
            ChatID = chatID;
            FileName = fileName;
            ChunkSize = chunkSize;
            ExternalFileIds = externalFileIds;
        }
    }
}