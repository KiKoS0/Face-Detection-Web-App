using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceDetectionWeb.Entities;
using FaceDetectionWeb.RabbitMQ;
using FaceDetectionWeb.Services;
using FaceDetectionWeb.ViewModels;

namespace FaceDetectionWeb.Pages.Images
{
    public class IndexModel : PageModel
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly ImageService _imageService;
        private readonly RabbitMQClient _mQClient;

        public IndexModel(IHostingEnvironment hostingEnvironment,
            ImageService imageService,
            RabbitMQClient mQClient)
        {
            _hostingEnvironment = hostingEnvironment;
            _imageService = imageService;
            _mQClient = mQClient;
        }

        public GridImageView GridImageView { get; set; }

        public async Task OnGetAsync()
        {
            var imgs = await _imageService.GetAsync();
            GridImageView = new GridImageView(imgs,4);
        }

        [BindProperty]
        public ImageViewModel ImageViewModel { get; set; }


        public async Task<ActionResult> OnPostAsync()
        {
            var validImageTypes = new string[]
                                    {
                                            "image/gif",
                                            "image/jpeg",
                                            "image/pjpeg",
                                            "image/png"
                                    };
            if(ImageViewModel.ImageUpload==null || ImageViewModel.ImageUpload.Length == 0)
            {
                ModelState.AddModelError("ImageUpload", "This field is required");
            } else if(!validImageTypes.Contains(ImageViewModel.ImageUpload.ContentType))
            {
                ModelState.AddModelError("ImageUpload", "Please choose either a GIF, JPG or PNG image.");

            }
            if (ModelState.IsValid)
            {
                var image = new Image
                {
                    Title = ImageViewModel.Title,
                    Caption = ImageViewModel.Caption,
                    AltText = ImageViewModel.AltText,
                    FaceDetect = ImageViewModel.FaceDetect
                };
                if(ImageViewModel.ImageUpload!=null && ImageViewModel.ImageUpload.Length > 0)
                {
                    var uploadDir = "uploads";
                    var outputFile = GenerateFileName(Path.GetFileNameWithoutExtension(ImageViewModel.ImageUpload.FileName))
                                        +Path.GetExtension(ImageViewModel.ImageUpload.FileName);
                    var imagePath = Path.Combine(_hostingEnvironment.WebRootPath,
                                                 uploadDir,
                                                 outputFile);
                    var SaveToDiskTask = SaveImageToDisk(ImageViewModel.ImageUpload, imagePath);
                    var imageUrl = Path.Combine(uploadDir, outputFile);
                    image.ImageUrl = imageUrl;


                    await SaveToDiskTask;
                    image = await SaveImageToDb(image);
                    _mQClient.SendImagePath(image);

                }
            }
            return RedirectToPage("./Index");

        }

        private string GenerateFileName(string context)
        {
            return context + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Guid.NewGuid().ToString("N");
        }

        private async Task<Image> SaveImageToDb(Image image)
        {
            return await _imageService.CreateAsync(image);
        }


        private async Task SaveImageToDisk(IFormFile formFile,string filePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }
        }
    }
}