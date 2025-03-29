using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MotoBikeManage.ViewModels
{
    public class VehicleDetailViewModel
    {
        public int vehicle_id { get; set; }
        public int model_id { get; set; }
        public string name { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string color { get; set; }
        public int manufacture_year { get; set; }
        public string image { get; set; }
        public string frame_number { get; set; }
        public string engine_number { get; set; }
        public string status { get; set; }
        public DateTime? created_at { get; set; }
    }
}