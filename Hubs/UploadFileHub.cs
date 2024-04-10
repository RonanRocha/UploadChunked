using Microsoft.AspNetCore.SignalR;

namespace UploadChunked.Hubs
{
    public class UploadFileHub : Hub
    {
        private IWebHostEnvironment _environment;
        public Dictionary<string, List<ChunkViewModelSignalR>> Chunks { get; set; }

        public UploadFileHub(IWebHostEnvironment environment, Dictionary<string, List<ChunkViewModelSignalR>> request)
        {
            _environment = environment;
            Chunks = request;
        }

        public async Task UploadChunk(ChunkViewModelSignalR viewModel)
        {

            if (!Chunks.ContainsKey(viewModel.Name))
            {
                var list = new List<ChunkViewModelSignalR>
                {
                    viewModel
                };

                Chunks.Add(viewModel.Name, list);
            }
            else
            {
                Chunks[viewModel.Name].Add(viewModel);
            }


            var c = Chunks.FirstOrDefault(x => x.Key == viewModel.Name);
            var count = c.Value.Count;

            if (count == viewModel.TotalChunks)
            {
                var ordered = c.Value.OrderBy(x => x.Order);
           
                foreach (var chunk in ordered)
                {

                    var uploadPath = Path.Combine(_environment.WebRootPath, "Uploads");
                    var fileName = Path.GetFileNameWithoutExtension(chunk.Name);

                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    //var fileTempPath = Path.Combine(_environment.WebRootPath, Path.GetFileNameWithoutExtension("Temp"));
                    var fileTempPath = Path.Combine(_environment.WebRootPath, Path.Combine("Temp", fileName));

                    if (!Directory.Exists(fileTempPath))
                        Directory.CreateDirectory(fileTempPath);


                    var chunkFilePath = Path.Combine(fileTempPath, $"{fileName}-{chunk.Order}.part");

                    using FileStream fs = new FileStream(chunkFilePath, FileMode.Create, FileAccess.Write);
                    await fs.WriteAsync(Convert.FromBase64String(chunk.Blob));
                    fs.Close();

                    await Clients.Caller.SendAsync("ProgressUpload", chunk.ChunkId, "Processed");
                   

                    if (chunk.Order == chunk.TotalChunks)
                    {
                       
                        CombineChunks(uploadPath, $"{fileName}", chunk.TotalChunks, ".jpg");   
                        await Clients.Caller.SendAsync("UploadFinished", true);
                    }

                }


            }


        }

        private void CombineChunks(string uploadPath, string fileId, int totalChunks, string extension)
        {
            var finalFilePath = Path.Combine(uploadPath, fileId + extension);

            using var finalStream = System.IO.File.Create(finalFilePath);

            for (int i = 1; i <= totalChunks; i++)
            {


                var chunkFilePath = $"wwwroot/Temp/{fileId}/" + $"{fileId}-{i}.part";

                using var chunkStream = System.IO.File.OpenRead(chunkFilePath);

                chunkStream.CopyTo(finalStream);


            }
            //Chunks.Remove(fileId);
            Directory.Delete(Path.Combine("wwwroot/Temp/", fileId), true);


        }



    }





    public class ChunkViewModelSignalR
    {
        public string Name { get; set; }
        public string ChunkId { get; set; }
        public int TotalChunks { get; set; }
        public string Status { get; set; }
        public int Order { get; set; }
        public string Blob { get; set; }
    }
}
