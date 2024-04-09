using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UploadChunked.Models;
using System.IO;

namespace UploadChunked.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IWebHostEnvironment environment, ILogger<HomeController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

 

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }


    [HttpPost]
    public async  Task<IActionResult> Upload([FromForm] ChunkFileViewModel fileChunk)
    {
        try
        {
            var chunkNumber = fileChunk.ChunkNumber + 1;
            var totalChunks = fileChunk.TotalChunks;
            var fileId = fileChunk.FileId;


            var uploadPath = Path.Combine(_environment.WebRootPath, "Uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var fileTempPath = Path.Combine(_environment.WebRootPath, Path.GetFileNameWithoutExtension(fileId));

            if (!Directory.Exists(fileTempPath))
                Directory.CreateDirectory(fileTempPath);

            var chunkFilePath = Path.Combine(fileTempPath, $"{chunkNumber}.part");

            var file = HttpContext.Request.Form.Files[0];

            using FileStream fs = new FileStream(chunkFilePath, FileMode.Create, FileAccess.ReadWrite);

            await file.CopyToAsync(fs);

            fs.Close();


            if (chunkNumber == totalChunks)
            {
                // Combine all chunks to create the final file
                var extension = Path.GetExtension(file.FileName);

                CombineChunks(uploadPath, fileId, totalChunks, extension);
   
            }

            return Ok("Chunk uploaded successfully.");
        }
        catch (Exception ex)
        {
           
            
            return BadRequest(ex.Message);
        }
    }


    private void CombineChunks(string uploadPath, string fileId, int totalChunks, string extension)
    {
        var finalFilePath = Path.Combine(uploadPath, fileId + extension);
        using var finalStream = System.IO.File.Create(finalFilePath);
        
            for (int i = 1; i <= totalChunks; i++)
            {

            if (i == 223)
            {
                Console.WriteLine("asdas");
            }

                var chunkFilePath = Path.Combine("wwwroot/" + fileId, $"{i}.part");

                using var chunkStream = System.IO.File.OpenRead(chunkFilePath);
                
                chunkStream.CopyTo(finalStream);
                    
            }

            Directory.Delete(Path.Combine("wwwroot/", fileId), true);
        
    }

}
