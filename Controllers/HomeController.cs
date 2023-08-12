using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Data;
using PhotoGallery.Models;
using PhotoGallery.ViewModels;

namespace PhotoGallery.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly PhotoGalleryDbContext _photoGalleryDbContext;

    public HomeController(ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment, PhotoGalleryDbContext photoGalleryDbContext)
    {
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
        _photoGalleryDbContext = photoGalleryDbContext;
    }

    public IActionResult Index()
    {
        var imageDirectory = Path.Combine(_webHostEnvironment.WebRootPath, "images");
        if (!Directory.Exists(imageDirectory))
        {
            Directory.CreateDirectory(imageDirectory);

            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var directoryInfo = new DirectoryInfo(imageDirectory);
                    var directorySecurity = directoryInfo.GetAccessControl();
                    directorySecurity.AddAccessRule(new FileSystemAccessRule("IIS_IUSRS", FileSystemRights.Modify, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                    directoryInfo.SetAccessControl(directorySecurity);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error setting directory permissions");
            }
        }

        var images = _photoGalleryDbContext.Photos.ToList();
        return View(images);
    }

    [Authorize]
    public IActionResult Upload()
    {
        return View();
    }

    [HttpPost]
    [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
    [Authorize]
    public IActionResult UploadImages()
    {
        var files = Request.Form.Files;
        if (files.Count > 0)
        {
            foreach (var file in files)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", fileName);

                _photoGalleryDbContext.Photos.Add(new Photo
                {
                    FileName = fileName,
                    UserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier))
                });
                _photoGalleryDbContext.SaveChanges();

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
            }
        }

        return RedirectToAction("Index");
    }

    [Authorize]
    public async Task<ActionResult> DeleteImage(int id)
    {
        var photo = await _photoGalleryDbContext.Photos
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)));
        if (photo != null)
        {
            _photoGalleryDbContext.Photos.Remove(photo);
            _photoGalleryDbContext.SaveChanges();

            var filePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", photo.FileName!);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }

        return RedirectToAction("Index");
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
}
