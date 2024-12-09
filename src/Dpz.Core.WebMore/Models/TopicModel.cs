using System;
using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models
{
    public class TopicModel
    {
        
        public string Id { get; set; }
        
        /// <summary>
        /// 标题
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 摘要 markdown文本
        /// </summary>
        public string Excerpt { get; set; }
        
        /// <summary>
        /// 详细内容 markdown文本
        /// </summary>
        public string Content { get; set; }
        
        /// <summary>
        /// 图片ID
        /// </summary>
        public string ImageId { get; set; }
        
        /// <summary>
        /// 内容所包含的图片
        /// </summary>
        public List<string> Images { get; set; }
        
        /// <summary>
        /// 回复量
        /// </summary>
        public int CommentCount { get; set; }
        
        /// <summary>
        /// 来源
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// 发布时间
        /// </summary>
        public DateTime PublishTime { get; set; }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
    }
}