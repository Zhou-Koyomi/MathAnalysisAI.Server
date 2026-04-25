using System;
using System.ComponentModel.DataAnnotations;

namespace MathAnalysisAI.Models
{
    public class WrongQuestion
    {
        [Key]
        public int Id { get; set; }

        // 原始题目图片
        public string ImagePath { get; set; }

        // OCR原始结果
        public string RawLatex { get; set; }

        // 清洗后的latex
        public string CleanLatex { get; set; }

        // 学生自己的答案
        public string StudentAnswer { get; set; }

        // AI整体评价
        public string OverallEvaluation { get; set; }

        // AI错误定位
        public string ErrorAnalysis { get; set; }

        // AI修改建议
        public string ImprovementSuggestion { get; set; }

        // AI标准答案
        public string StandardSolution { get; set; }

        // 知识点标签
        public string KnowledgePoint { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}