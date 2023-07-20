using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CacheRedis.Model
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Desciption { get; set; }
        public bool IsOffline { get; set; }
        public string location { get; set; } = String.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsPublicSchool { get; set; }
        public int HostId { get; set; }
        public int? GradeId { get; set; }
        public bool Archived { get; set; } = true;
        public int SchoolId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
