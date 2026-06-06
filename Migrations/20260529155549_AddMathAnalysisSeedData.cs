using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MathAnalysisAI.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddMathAnalysisSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Subjects",
                columns: new[] { "Id", "CreatedAt", "Description", "Name" },
                values: new object[] { 100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "本科数学学科", "数学" });

            migrationBuilder.InsertData(
                table: "Courses",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "Name", "SubjectId" },
                values: new object[] { 200, "math_analysis", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "本科数学分析课程", "数学分析", 100 });

            migrationBuilder.InsertData(
                table: "Chapters",
                columns: new[] { "Id", "Code", "CourseId", "CreatedAt", "Description", "Name", "OrderIndex" },
                values: new object[,]
                {
                    { 301, "ma.limit", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "函数与数列极限基础", "极限", 1 },
                    { 302, "ma.continuity", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "函数连续与间断", "连续", 2 },
                    { 303, "ma.derivative", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "导数定义与求导", "导数与微分", 3 },
                    { 304, "ma.mean_value_theorem", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rolle 与拉格朗日中值定理", "微分中值定理", 4 },
                    { 305, "ma.indefinite_integral", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "原函数与积分技巧", "不定积分", 5 },
                    { 306, "ma.definite_integral", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黎曼积分与计算", "定积分", 6 },
                    { 307, "ma.improper_integral", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "无界区间与无界函数积分", "反常积分", 7 },
                    { 308, "ma.numeric_series", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "级数收敛性判别", "数项级数", 8 },
                    { 309, "ma.function_series", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "一致收敛与函数项级数", "函数列与函数项级数", 9 },
                    { 310, "ma.power_series", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "收敛半径与展开", "幂级数", 10 },
                    { 311, "ma.multivariable_differential", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "偏导与全微分", "多元函数微分学", 11 }
                });

            migrationBuilder.InsertData(
                table: "KnowledgePoints",
                columns: new[] { "Id", "ChapterId", "Code", "CourseId", "CreatedAt", "Description", "Name" },
                values: new object[,]
                {
                    { 1001, 301, "ma.limit.sequence_limit", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "数列收敛定义与判定", "数列极限定义" },
                    { 1002, 301, "ma.limit.epsilon_delta", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "函数极限定义与几何直观", "函数极限定义" },
                    { 1003, 301, "ma.limit.infinitesimal_infinite", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "无穷小等价与比较", "无穷小与无穷大" },
                    { 1004, 301, "ma.limit.algebra_rules", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "极限运算法则", "极限四则运算" },
                    { 1005, 302, "ma.continuity.definition", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "点连续与区间连续", "连续定义" },
                    { 1006, 302, "ma.continuity.discontinuity_types", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "可去/跳跃/无穷间断", "间断点分类" },
                    { 1007, 302, "ma.continuity.properties", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "介值与最值性质", "连续函数性质" },
                    { 1008, 302, "ma.continuity.uniform_continuity", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "一致连续定义", "一致连续初步" },
                    { 1009, 303, "ma.derivative.definition", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "差商极限与导数", "导数定义" },
                    { 1010, 303, "ma.derivative.continuity_relation", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "可导蕴含连续", "可导与连续关系" },
                    { 1011, 303, "ma.derivative.rules", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "四则与复合函数求导", "求导法则" },
                    { 1012, 303, "ma.derivative.differential_linearization", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "微分定义与几何意义", "微分与线性近似" },
                    { 1013, 304, "ma.mean_value_theorem.rolle", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Rolle 定理条件与结论", "Rolle 定理" },
                    { 1014, 304, "ma.mean_value_theorem.lagrange", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "平均变化率与瞬时变化率", "拉格朗日中值定理" },
                    { 1015, 304, "ma.mean_value_theorem.cauchy", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "中值定理推广", "柯西中值定理" },
                    { 1016, 304, "ma.mean_value_theorem.lhopital", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "未定式极限计算", "洛必达法则" },
                    { 1017, 305, "ma.indefinite_integral.antiderivative", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "原函数与不定积分定义", "原函数概念" },
                    { 1018, 305, "ma.indefinite_integral.substitution", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "第一类与第二类换元", "换元积分法" },
                    { 1019, 305, "ma.indefinite_integral.integration_by_parts", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "分部积分公式应用", "分部积分法" },
                    { 1020, 305, "ma.indefinite_integral.rational_function", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "部分分式分解", "有理函数积分" },
                    { 1021, 306, "ma.definite_integral.definition", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "黎曼和与定积分", "定积分定义" },
                    { 1022, 306, "ma.definite_integral.mean_value_theorem", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "积分中值定理", "积分中值定理" },
                    { 1023, 306, "ma.definite_integral.newton_leibniz", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "微积分基本定理", "牛顿-莱布尼茨公式" },
                    { 1024, 306, "ma.definite_integral.applications", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "面积与体积计算", "定积分应用" },
                    { 1025, 307, "ma.improper_integral.infinite_interval", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "积分上限趋于无穷", "无穷区间反常积分" },
                    { 1026, 307, "ma.improper_integral.unbounded_function", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "积分区间端点无界", "无界函数反常积分" },
                    { 1027, 307, "ma.improper_integral.comparison_test", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "反常积分比较判别", "比较判别法" },
                    { 1028, 307, "ma.improper_integral.convergence_criteria", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "常见反常积分判定", "收敛与发散判定" },
                    { 1029, 308, "ma.numeric_series.convergence_definition", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "部分和与收敛", "级数收敛定义" },
                    { 1030, 308, "ma.numeric_series.positive_series_tests", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "比较、比值、根值判别", "正项级数判别" },
                    { 1031, 308, "ma.numeric_series.alternating_series_test", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Leibniz 判别法", "交错级数判别" },
                    { 1032, 308, "ma.numeric_series.absolute_conditional", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "绝对收敛关系", "绝对与条件收敛" },
                    { 1033, 309, "ma.function_series.pointwise_convergence", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "逐点收敛定义", "函数列逐点收敛" },
                    { 1034, 309, "ma.function_series.uniform_convergence", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "一致收敛与范数", "一致收敛定义" },
                    { 1035, 309, "ma.function_series.weierstrass_m_test", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "一致收敛判别", "Weierstrass 判别法" },
                    { 1036, 309, "ma.function_series.limit_exchange", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "极限与积分/导数交换", "一致收敛交换极限" },
                    { 1037, 310, "ma.power_series.radius_of_convergence", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "幂级数基本形式", "幂级数与收敛半径" },
                    { 1038, 310, "ma.power_series.interval_of_convergence", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "端点收敛性讨论", "收敛区间判定" },
                    { 1039, 310, "ma.power_series.termwise_operations", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "逐项求导与积分", "幂级数逐项运算" },
                    { 1040, 310, "ma.power_series.taylor_expansion", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "常见函数的幂级数展开", "Taylor 展开" },
                    { 1041, 311, "ma.multivariable_differential.partial_derivative", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "多元函数偏导", "偏导数定义" },
                    { 1042, 311, "ma.multivariable_differential.total_differential", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "全微分存在条件", "全微分" },
                    { 1043, 311, "ma.multivariable_differential.directional_gradient", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "梯度及几何意义", "方向导数与梯度" },
                    { 1044, 311, "ma.multivariable_differential.extrema_lagrange", 200, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Hessian 与拉格朗日乘子", "极值与条件极值" }
                });

            migrationBuilder.InsertData(
                table: "KnowledgeDependencies",
                columns: new[] { "Id", "CreatedAt", "DependencyType", "FromKnowledgePointId", "ToKnowledgePointId" },
                values: new object[,]
                {
                    { 2001, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1001, 1005 },
                    { 2002, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1002, 1005 },
                    { 2003, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1005, 1009 },
                    { 2004, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1009, 1014 },
                    { 2005, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "uses", 1014, 1018 },
                    { 2006, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1017, 1023 },
                    { 2007, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "uses", 1023, 1024 },
                    { 2008, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1029, 1037 },
                    { 2009, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "uses", 1034, 1039 },
                    { 2010, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "often_confused", 1030, 1027 },
                    { 2011, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "prerequisite", 1012, 1042 },
                    { 2012, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "uses", 1041, 1043 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2001);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2002);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2003);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2004);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2005);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2006);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2007);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2008);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2009);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2010);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2011);

            migrationBuilder.DeleteData(
                table: "KnowledgeDependencies",
                keyColumn: "Id",
                keyValue: 2012);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1003);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1004);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1006);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1007);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1008);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1010);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1011);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1013);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1015);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1016);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1019);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1020);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1021);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1022);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1025);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1026);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1028);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1031);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1032);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1033);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1035);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1036);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1038);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1040);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1044);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1001);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1002);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1005);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1009);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1012);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1014);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1017);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1018);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1023);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1024);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1027);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1029);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1030);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1034);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1037);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1039);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1041);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1042);

            migrationBuilder.DeleteData(
                table: "KnowledgePoints",
                keyColumn: "Id",
                keyValue: 1043);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 301);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 302);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 303);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 304);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 305);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 306);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 307);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 308);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 309);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 310);

            migrationBuilder.DeleteData(
                table: "Chapters",
                keyColumn: "Id",
                keyValue: 311);

            migrationBuilder.DeleteData(
                table: "Courses",
                keyColumn: "Id",
                keyValue: 200);

            migrationBuilder.DeleteData(
                table: "Subjects",
                keyColumn: "Id",
                keyValue: 100);
        }
    }
}
