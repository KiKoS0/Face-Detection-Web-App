using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using FaceDetectionWeb.Entities;
using FaceDetectionWeb.Services;
using System.IO;
using FaceDetectionWeb.RabbitMQ;

namespace FaceDetectionWeb.Pages.Images
{
    public class DetailsModel : PageModel
    {

        public Image Image { get; set; }
        private readonly ImageService _imageService;
        public string ModifielVersionPath { get; set; }

        private readonly IHostingEnvironment _env;
        private readonly RabbitMQClient _mQClient;

        public DetailsModel(IHostingEnvironment env, ImageService imageService,RabbitMQClient mQClient)
        {
            _imageService = imageService;
            _env = env;
            _mQClient = mQClient;
        }

        public async Task<IActionResult> OnGet(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            Image = await _imageService.GetAsync(id);
            if(Image == null)
            {
                return NotFound();
            }
            string modImage = Path.Combine(Path.GetDirectoryName(Image.ImageUrl),
                      Path.GetFileNameWithoutExtension(Image.ImageUrl)
                      + "_Mod" +
                      Path.GetExtension(Image.ImageUrl));
            string checkImage = Path.Combine(_env.WebRootPath, modImage);
            if (System.IO.File.Exists(checkImage))
            {
                ModifielVersionPath = modImage;
            }
            return Page();
        }

        [BindProperty]
        public Image ImageToReschedule { get; set; }

        public async Task<IActionResult> OnPost()
        {
            string id = ImageToReschedule.Id;
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }
            ImageToReschedule = await _imageService.GetAsync(id);
            if (ImageToReschedule == null)
            {
                return NotFound();
            }
            _mQClient.SendImagePath(ImageToReschedule);
            return RedirectToPage("./Details", ImageToReschedule.Id);
        }
    }
}