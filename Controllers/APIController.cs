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
        private readonly int _chunkSize;

        public APIController(IConfiguration configuration, FilesRepository repository)
        {
            _repository = repository;
            _telegramBotClient = new TelegramBotClient(configuration.GetValue<string>("Telegram:Token") ?? throw new ArgumentNullException("No Token Provided."));
            _chatID = configuration.GetValue<string>("Telegram:ChatID") ?? throw new ArgumentNullException("No Chat ID provided.");
            _chunkSize = configuration.GetValue<int>("SizeLimitations:Part");
        }

        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        public async Task<IActionResult> Upload(IFormFile[] files)
        {
            foreach (var file in files)
            {
                int parts = 0;
                var externalFileIds = new List<string>();
                using var inputStream = file.OpenReadStream();

                while(inputStream.Position < file.Length)
                    externalFileIds.Add(await ParseStream(inputStream, $"{file.FileName}.part{parts++}", _chunkSize));

                _repository.AddFile(new FileEntry(_chatID, file.FileName, _chunkSize, externalFileIds));
            }

            return Accepted();
        }

        [HttpPost("list")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IEnumerable<FileEntry> List(string chatID)
        {
            return _repository.ListFiles(chatID);
        }

        [HttpGet("retrieve")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Retrieve(Guid fileId)
        {
            var fileEntry = _repository.RetrieveFile(fileId);
            if (fileEntry == null)
            {
                return NotFound("File not found.");
            }

            var combinedStream = new MemoryStream();

            foreach (var externalFileId in fileEntry.ExternalFileIds)
            {
                var fileInfo = await _telegramBotClient.GetFileAsync(externalFileId);
                using var fileStream = new MemoryStream();

                await _telegramBotClient.DownloadFileAsync(fileInfo.FilePath ?? throw new ArgumentNullException("File Path from Telegram API was NULL"), fileStream);
                fileStream.Position = 0;
                await fileStream.CopyToAsync(combinedStream);
            }

            combinedStream.Position = 0;
            return File(combinedStream, "application/octet-stream", fileEntry.FileName);
        }


        private async Task<string> ParseStream(Stream input, string part, int chunkSize)
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

            if(message.Document == null)
                throw new ArgumentNullException("Message Document was NULL");

            return message.Document.FileId;
        }
    }
}
