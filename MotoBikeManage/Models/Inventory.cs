//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MotoBikeManage.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class Inventory
    {
        public int model_id { get; set; }
        public string name { get; set; }
        public string brand { get; set; }
        public string model { get; set; }
        public string color { get; set; }
        public int manufacture_year { get; set; }
        public Nullable<int> stock_remaining { get; set; }
        public Nullable<int> total_exported { get; set; }
        public Nullable<int> total_maintenance { get; set; }
    }
}
