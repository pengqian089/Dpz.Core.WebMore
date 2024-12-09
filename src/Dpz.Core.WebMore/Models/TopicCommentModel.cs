using System;
using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models
{
    public class TopicCommentModel
    {
        public string Id { get; set; }
        
        public string TopicId { get; set; }
        
        /// <summary>
        /// 回复内容 markdown文本
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 回复所包含的图片
        /// </summary>
        public List<string> Images { get; set; }
        
        /// <summary>
        /// 是否折叠
        /// </summary>
        public bool IsCollapsed { get; set; }
        
        /// <summary>
        /// 来源
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// 赞同数
        /// </summary>
        public int VoteUpCount { get; set; }
        
        /// <summary>
        /// 回复时间
        /// </summary>
        public DateTime CreatedTime { get; set; }
        
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime UpdatedTime { get; set; }
    }
}