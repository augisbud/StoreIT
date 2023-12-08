using Microsoft.AspNetCore.Mvc;
using StoreIT.Models;
using StoreIT.Services;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace StoreIT.Controllers
{
    [ApiController]
    [Route("api")]
    public class APIController : ControllerBase
    {
        private readonly FilesRepository _repository;
        private readonly ITelegramBotClient _telegramBotClient;
        private readonly string _chatID;

        public APIController(IConfiguration configuration, FilesRepository repository)
        {
            _repository = repository;
            _telegramBotClient = new TelegramBotClient(configuration.GetValue<string>("Telegram:Token") ?? throw new ArgumentNullException("No Token Provided."));
            _chatID = configuration.GetValue<string>("Telegram:ChatID") ?? throw new ArgumentNullException("No Chat ID provided.");
        }

        [HttpPost("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<FileEntry> List(string chatID)
        {
            return _repository.ListFiles(chatID);
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Upload(IFormFile[] files)
        {
            foreach (var file in files)
            {
                int parts = 0;
                using var inputStream = file.OpenReadStream();

                while(inputStream.Position < file.Length)
                    await ParseStream(inputStream, $"{file.FileName}.part{parts++}", 10 * 1024 * 1024);

                _repository.AddFile(new FileEntry(_chatID, file.FileName, parts, 10 * 1024 * 1024));
            }

            return Accepted();
        }

        private async Task ParseStream(Stream input, string part, int chunkSize)
        {
            var buffer = new byte[chunkSize];
            var chunkStream = new MemoryStream();
            var totalRead = 0;

            int bytesRead;
            while (totalRead < chunkSize && (bytesRead = await input.ReadAsync(buffer)) > 0)
            {
                await chunkStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                totalRead += bytesRead;
            }

            chunkStream.Position = 0;

            var message = await _telegramBotClient.SendDocumentAsync(
                chatId: _chatID,
                document: InputFile.FromStream(stream: chunkStream, fileName: part)
            );
        }
    }
}
