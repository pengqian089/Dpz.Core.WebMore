using System;

namespace Dpz.Core.WebMore.Models
{
    public class TimelineModel
    {
        public string Id { get; set; }
        
        public string Title { get; set; }
        
        public string Content { get; set; }
        
        public DateTime Date { get; set; }
        
        public string More { get; set; }
        
        /// <summary>
        /// 作者
        /// </summary>
        public UserInfo Author { get; set; }
        
        public DateTime CreateTime { get; set; }
        
        public DateTime LastUpdateTime { get; set; }
    }
}