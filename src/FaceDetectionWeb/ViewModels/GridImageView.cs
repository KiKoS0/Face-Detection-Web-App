using FaceDetectionWeb.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FaceDetectionWeb.ViewModels
{
    public class GridImageView
    {
        public List<List<AdvancedImageViewModel>> Images;
        private int _columns = 4;
        public int Columns {
            get
            {
                return _columns;
            }
            private set { }
        }
        public GridImageView(List<Image> images, int? columns)
        {
            if (columns != null && columns > 0)
                _columns = columns.Value;
            Images = new List<List<AdvancedImageViewModel>>();
            for (int i = 0; i < _columns; i++)
            {
                Images.Add(new List<AdvancedImageViewModel>());
            }
            if (!images.Any()) {
                return;
            }
            int index = 0;
            foreach(var img in images)
            {
                Images.ElementAt(index).Add(new AdvancedImageViewModel
                {
                    Title = img.Title,
                    AltText = img.AltText,
                    Caption = img.Caption,
                    Id = img.Id,
                    Path = img.ImageUrl.Trim()
                });
                index = (index + 1) % _columns;
            }
        }
    }
}
