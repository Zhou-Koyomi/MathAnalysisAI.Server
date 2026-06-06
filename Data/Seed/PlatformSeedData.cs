using MathAnalysisAI.Server.Models;

namespace MathAnalysisAI.Server.Data.Seed
{
    public static class PlatformSeedData
    {
        public static readonly DateTime SeedCreatedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public const int SubjectMathId = 100;
        public const int CourseMathAnalysisId = 200;

        public static readonly Subject[] Subjects =
        {
            new()
            {
                Id = SubjectMathId,
                Name = "数学",
                Description = "本科数学学科",
                CreatedAt = SeedCreatedAt
            }
        };

        public static readonly Course[] Courses =
        {
            new()
            {
                Id = CourseMathAnalysisId,
                SubjectId = SubjectMathId,
                Name = "数学分析",
                Code = "math_analysis",
                Description = "本科数学分析课程",
                CreatedAt = SeedCreatedAt
            }
        };

        public static readonly Chapter[] Chapters =
        {
            new() { Id = 301, CourseId = CourseMathAnalysisId, Name = "极限", Code = "ma.limit", OrderIndex = 1, Description = "函数与数列极限基础", CreatedAt = SeedCreatedAt },
            new() { Id = 302, CourseId = CourseMathAnalysisId, Name = "连续", Code = "ma.continuity", OrderIndex = 2, Description = "函数连续与间断", CreatedAt = SeedCreatedAt },
            new() { Id = 303, CourseId = CourseMathAnalysisId, Name = "导数与微分", Code = "ma.derivative", OrderIndex = 3, Description = "导数定义与求导", CreatedAt = SeedCreatedAt },
            new() { Id = 304, CourseId = CourseMathAnalysisId, Name = "微分中值定理", Code = "ma.mean_value_theorem", OrderIndex = 4, Description = "Rolle 与拉格朗日中值定理", CreatedAt = SeedCreatedAt },
            new() { Id = 305, CourseId = CourseMathAnalysisId, Name = "不定积分", Code = "ma.indefinite_integral", OrderIndex = 5, Description = "原函数与积分技巧", CreatedAt = SeedCreatedAt },
            new() { Id = 306, CourseId = CourseMathAnalysisId, Name = "定积分", Code = "ma.definite_integral", OrderIndex = 6, Description = "黎曼积分与计算", CreatedAt = SeedCreatedAt },
            new() { Id = 307, CourseId = CourseMathAnalysisId, Name = "反常积分", Code = "ma.improper_integral", OrderIndex = 7, Description = "无界区间与无界函数积分", CreatedAt = SeedCreatedAt },
            new() { Id = 308, CourseId = CourseMathAnalysisId, Name = "数项级数", Code = "ma.numeric_series", OrderIndex = 8, Description = "级数收敛性判别", CreatedAt = SeedCreatedAt },
            new() { Id = 309, CourseId = CourseMathAnalysisId, Name = "函数列与函数项级数", Code = "ma.function_series", OrderIndex = 9, Description = "一致收敛与函数项级数", CreatedAt = SeedCreatedAt },
            new() { Id = 310, CourseId = CourseMathAnalysisId, Name = "幂级数", Code = "ma.power_series", OrderIndex = 10, Description = "收敛半径与展开", CreatedAt = SeedCreatedAt },
            new() { Id = 311, CourseId = CourseMathAnalysisId, Name = "多元函数微分学", Code = "ma.multivariable_differential", OrderIndex = 11, Description = "偏导与全微分", CreatedAt = SeedCreatedAt }
        };

        public static readonly KnowledgePoint[] KnowledgePoints =
        {
            new() { Id = 1001, CourseId = CourseMathAnalysisId, ChapterId = 301, Name = "数列极限定义", Code = "ma.limit.sequence_limit", Description = "数列收敛定义与判定", CreatedAt = SeedCreatedAt },
            new() { Id = 1002, CourseId = CourseMathAnalysisId, ChapterId = 301, Name = "函数极限定义", Code = "ma.limit.epsilon_delta", Description = "函数极限定义与几何直观", CreatedAt = SeedCreatedAt },
            new() { Id = 1003, CourseId = CourseMathAnalysisId, ChapterId = 301, Name = "无穷小与无穷大", Code = "ma.limit.infinitesimal_infinite", Description = "无穷小等价与比较", CreatedAt = SeedCreatedAt },
            new() { Id = 1004, CourseId = CourseMathAnalysisId, ChapterId = 301, Name = "极限四则运算", Code = "ma.limit.algebra_rules", Description = "极限运算法则", CreatedAt = SeedCreatedAt },

            new() { Id = 1005, CourseId = CourseMathAnalysisId, ChapterId = 302, Name = "连续定义", Code = "ma.continuity.definition", Description = "点连续与区间连续", CreatedAt = SeedCreatedAt },
            new() { Id = 1006, CourseId = CourseMathAnalysisId, ChapterId = 302, Name = "间断点分类", Code = "ma.continuity.discontinuity_types", Description = "可去/跳跃/无穷间断", CreatedAt = SeedCreatedAt },
            new() { Id = 1007, CourseId = CourseMathAnalysisId, ChapterId = 302, Name = "连续函数性质", Code = "ma.continuity.properties", Description = "介值与最值性质", CreatedAt = SeedCreatedAt },
            new() { Id = 1008, CourseId = CourseMathAnalysisId, ChapterId = 302, Name = "一致连续初步", Code = "ma.continuity.uniform_continuity", Description = "一致连续定义", CreatedAt = SeedCreatedAt },

            new() { Id = 1009, CourseId = CourseMathAnalysisId, ChapterId = 303, Name = "导数定义", Code = "ma.derivative.definition", Description = "差商极限与导数", CreatedAt = SeedCreatedAt },
            new() { Id = 1010, CourseId = CourseMathAnalysisId, ChapterId = 303, Name = "可导与连续关系", Code = "ma.derivative.continuity_relation", Description = "可导蕴含连续", CreatedAt = SeedCreatedAt },
            new() { Id = 1011, CourseId = CourseMathAnalysisId, ChapterId = 303, Name = "求导法则", Code = "ma.derivative.rules", Description = "四则与复合函数求导", CreatedAt = SeedCreatedAt },
            new() { Id = 1012, CourseId = CourseMathAnalysisId, ChapterId = 303, Name = "微分与线性近似", Code = "ma.derivative.differential_linearization", Description = "微分定义与几何意义", CreatedAt = SeedCreatedAt },

            new() { Id = 1013, CourseId = CourseMathAnalysisId, ChapterId = 304, Name = "Rolle 定理", Code = "ma.mean_value_theorem.rolle", Description = "Rolle 定理条件与结论", CreatedAt = SeedCreatedAt },
            new() { Id = 1014, CourseId = CourseMathAnalysisId, ChapterId = 304, Name = "拉格朗日中值定理", Code = "ma.mean_value_theorem.lagrange", Description = "平均变化率与瞬时变化率", CreatedAt = SeedCreatedAt },
            new() { Id = 1015, CourseId = CourseMathAnalysisId, ChapterId = 304, Name = "柯西中值定理", Code = "ma.mean_value_theorem.cauchy", Description = "中值定理推广", CreatedAt = SeedCreatedAt },
            new() { Id = 1016, CourseId = CourseMathAnalysisId, ChapterId = 304, Name = "洛必达法则", Code = "ma.mean_value_theorem.lhopital", Description = "未定式极限计算", CreatedAt = SeedCreatedAt },

            new() { Id = 1017, CourseId = CourseMathAnalysisId, ChapterId = 305, Name = "原函数概念", Code = "ma.indefinite_integral.antiderivative", Description = "原函数与不定积分定义", CreatedAt = SeedCreatedAt },
            new() { Id = 1018, CourseId = CourseMathAnalysisId, ChapterId = 305, Name = "换元积分法", Code = "ma.indefinite_integral.substitution", Description = "第一类与第二类换元", CreatedAt = SeedCreatedAt },
            new() { Id = 1019, CourseId = CourseMathAnalysisId, ChapterId = 305, Name = "分部积分法", Code = "ma.indefinite_integral.integration_by_parts", Description = "分部积分公式应用", CreatedAt = SeedCreatedAt },
            new() { Id = 1020, CourseId = CourseMathAnalysisId, ChapterId = 305, Name = "有理函数积分", Code = "ma.indefinite_integral.rational_function", Description = "部分分式分解", CreatedAt = SeedCreatedAt },

            new() { Id = 1021, CourseId = CourseMathAnalysisId, ChapterId = 306, Name = "定积分定义", Code = "ma.definite_integral.definition", Description = "黎曼和与定积分", CreatedAt = SeedCreatedAt },
            new() { Id = 1022, CourseId = CourseMathAnalysisId, ChapterId = 306, Name = "积分中值定理", Code = "ma.definite_integral.mean_value_theorem", Description = "积分中值定理", CreatedAt = SeedCreatedAt },
            new() { Id = 1023, CourseId = CourseMathAnalysisId, ChapterId = 306, Name = "牛顿-莱布尼茨公式", Code = "ma.definite_integral.newton_leibniz", Description = "微积分基本定理", CreatedAt = SeedCreatedAt },
            new() { Id = 1024, CourseId = CourseMathAnalysisId, ChapterId = 306, Name = "定积分应用", Code = "ma.definite_integral.applications", Description = "面积与体积计算", CreatedAt = SeedCreatedAt },

            new() { Id = 1025, CourseId = CourseMathAnalysisId, ChapterId = 307, Name = "无穷区间反常积分", Code = "ma.improper_integral.infinite_interval", Description = "积分上限趋于无穷", CreatedAt = SeedCreatedAt },
            new() { Id = 1026, CourseId = CourseMathAnalysisId, ChapterId = 307, Name = "无界函数反常积分", Code = "ma.improper_integral.unbounded_function", Description = "积分区间端点无界", CreatedAt = SeedCreatedAt },
            new() { Id = 1027, CourseId = CourseMathAnalysisId, ChapterId = 307, Name = "比较判别法", Code = "ma.improper_integral.comparison_test", Description = "反常积分比较判别", CreatedAt = SeedCreatedAt },
            new() { Id = 1028, CourseId = CourseMathAnalysisId, ChapterId = 307, Name = "收敛与发散判定", Code = "ma.improper_integral.convergence_criteria", Description = "常见反常积分判定", CreatedAt = SeedCreatedAt },

            new() { Id = 1029, CourseId = CourseMathAnalysisId, ChapterId = 308, Name = "级数收敛定义", Code = "ma.numeric_series.convergence_definition", Description = "部分和与收敛", CreatedAt = SeedCreatedAt },
            new() { Id = 1030, CourseId = CourseMathAnalysisId, ChapterId = 308, Name = "正项级数判别", Code = "ma.numeric_series.positive_series_tests", Description = "比较、比值、根值判别", CreatedAt = SeedCreatedAt },
            new() { Id = 1031, CourseId = CourseMathAnalysisId, ChapterId = 308, Name = "交错级数判别", Code = "ma.numeric_series.alternating_series_test", Description = "Leibniz 判别法", CreatedAt = SeedCreatedAt },
            new() { Id = 1032, CourseId = CourseMathAnalysisId, ChapterId = 308, Name = "绝对与条件收敛", Code = "ma.numeric_series.absolute_conditional", Description = "绝对收敛关系", CreatedAt = SeedCreatedAt },

            new() { Id = 1033, CourseId = CourseMathAnalysisId, ChapterId = 309, Name = "函数列逐点收敛", Code = "ma.function_series.pointwise_convergence", Description = "逐点收敛定义", CreatedAt = SeedCreatedAt },
            new() { Id = 1034, CourseId = CourseMathAnalysisId, ChapterId = 309, Name = "一致收敛定义", Code = "ma.function_series.uniform_convergence", Description = "一致收敛与范数", CreatedAt = SeedCreatedAt },
            new() { Id = 1035, CourseId = CourseMathAnalysisId, ChapterId = 309, Name = "Weierstrass 判别法", Code = "ma.function_series.weierstrass_m_test", Description = "一致收敛判别", CreatedAt = SeedCreatedAt },
            new() { Id = 1036, CourseId = CourseMathAnalysisId, ChapterId = 309, Name = "一致收敛交换极限", Code = "ma.function_series.limit_exchange", Description = "极限与积分/导数交换", CreatedAt = SeedCreatedAt },

            new() { Id = 1037, CourseId = CourseMathAnalysisId, ChapterId = 310, Name = "幂级数与收敛半径", Code = "ma.power_series.radius_of_convergence", Description = "幂级数基本形式", CreatedAt = SeedCreatedAt },
            new() { Id = 1038, CourseId = CourseMathAnalysisId, ChapterId = 310, Name = "收敛区间判定", Code = "ma.power_series.interval_of_convergence", Description = "端点收敛性讨论", CreatedAt = SeedCreatedAt },
            new() { Id = 1039, CourseId = CourseMathAnalysisId, ChapterId = 310, Name = "幂级数逐项运算", Code = "ma.power_series.termwise_operations", Description = "逐项求导与积分", CreatedAt = SeedCreatedAt },
            new() { Id = 1040, CourseId = CourseMathAnalysisId, ChapterId = 310, Name = "Taylor 展开", Code = "ma.power_series.taylor_expansion", Description = "常见函数的幂级数展开", CreatedAt = SeedCreatedAt },

            new() { Id = 1041, CourseId = CourseMathAnalysisId, ChapterId = 311, Name = "偏导数定义", Code = "ma.multivariable_differential.partial_derivative", Description = "多元函数偏导", CreatedAt = SeedCreatedAt },
            new() { Id = 1042, CourseId = CourseMathAnalysisId, ChapterId = 311, Name = "全微分", Code = "ma.multivariable_differential.total_differential", Description = "全微分存在条件", CreatedAt = SeedCreatedAt },
            new() { Id = 1043, CourseId = CourseMathAnalysisId, ChapterId = 311, Name = "方向导数与梯度", Code = "ma.multivariable_differential.directional_gradient", Description = "梯度及几何意义", CreatedAt = SeedCreatedAt },
            new() { Id = 1044, CourseId = CourseMathAnalysisId, ChapterId = 311, Name = "极值与条件极值", Code = "ma.multivariable_differential.extrema_lagrange", Description = "Hessian 与拉格朗日乘子", CreatedAt = SeedCreatedAt }
        };

        // NOTE: `often_confused` is modeled as a directed edge in storage,
        // but business logic should interpret it as an undirected relation.
        public static readonly KnowledgeDependency[] KnowledgeDependencies =
        {
            new() { Id = 2001, FromKnowledgePointId = 1001, ToKnowledgePointId = 1005, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2002, FromKnowledgePointId = 1002, ToKnowledgePointId = 1005, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2003, FromKnowledgePointId = 1005, ToKnowledgePointId = 1009, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2004, FromKnowledgePointId = 1009, ToKnowledgePointId = 1014, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2005, FromKnowledgePointId = 1014, ToKnowledgePointId = 1018, DependencyType = "uses", CreatedAt = SeedCreatedAt },
            new() { Id = 2006, FromKnowledgePointId = 1017, ToKnowledgePointId = 1023, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2007, FromKnowledgePointId = 1023, ToKnowledgePointId = 1024, DependencyType = "uses", CreatedAt = SeedCreatedAt },
            new() { Id = 2008, FromKnowledgePointId = 1029, ToKnowledgePointId = 1037, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2009, FromKnowledgePointId = 1034, ToKnowledgePointId = 1039, DependencyType = "uses", CreatedAt = SeedCreatedAt },
            new() { Id = 2010, FromKnowledgePointId = 1030, ToKnowledgePointId = 1027, DependencyType = "often_confused", CreatedAt = SeedCreatedAt },
            new() { Id = 2011, FromKnowledgePointId = 1012, ToKnowledgePointId = 1042, DependencyType = "prerequisite", CreatedAt = SeedCreatedAt },
            new() { Id = 2012, FromKnowledgePointId = 1041, ToKnowledgePointId = 1043, DependencyType = "uses", CreatedAt = SeedCreatedAt }
        };
    }
}
