namespace PMD_Backend.Models
{
    /// <summary>
    /// 结果测试配置项（对应 appsettings.json 中的 ResultTest:Items 数组）。
    /// </summary>
    public class ResultTestItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int Score { get; set; }
    }

    /// <summary>
    /// 结果测试配置根节点（对应 appsettings.json 中的 ResultTest 节点）。
    /// </summary>
    public class ResultTestOptions
    {
        public bool Enabled { get; set; } = true;

        public string Source { get; set; } = string.Empty;

        public List<ResultTestItem> Items { get; set; } = new();
    }
}
