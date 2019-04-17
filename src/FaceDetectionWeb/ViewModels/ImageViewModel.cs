using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FaceDetectionWeb.ViewModels
{
    public class ImageViewModel
    {
        [Required]
        public string Title { get; set; }

        public string AltText { get; set; }

        [DataType(DataType.Html)]
        public string Caption { get; set; }

        public bool FaceDetect { get; set; }

        [DataType(DataType.Upload)]
        [Display(Name ="Image File")]
        public IFormFile ImageUpload { get; set; }

    }
}
