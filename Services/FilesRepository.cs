using StoreIT.Models;

namespace StoreIT.Services
{
    public class FilesRepository
    {
        private readonly AppDbContext _context;

        public FilesRepository(AppDbContext context)
        {
            _context = context;
        }

        public void AddFile(FileEntry file)
        {
            _context.Files.Add(file);
            _context.SaveChanges();
        }

        public IEnumerable<FileEntry> ListFiles(string chatID) 
        {
            return _context.Files.Where(f => f.ChatID == chatID);
        }
    }
}