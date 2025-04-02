using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MotoBikeManage.ViewModels
{
    public class InventoryViewModel
    {
        public int model_id { get; set; }
        public string name { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string color { get; set; }
        public int manufacture_year { get; set; }
        public int stock_remaining { get; set; }
        public int total_exported { get; set; }
        public int total_maintenance { get; set; }
        public int total_vehicles { get; set; }
    }
}