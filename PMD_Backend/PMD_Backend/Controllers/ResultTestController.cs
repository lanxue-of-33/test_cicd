using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using PMD_Backend.Models;

namespace PMD_Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResultTestController : ControllerBase
    {
        private readonly ResultTestOptions _options;
        private readonly ILogger<ResultTestController> _logger;

        public ResultTestController(IOptions<ResultTestOptions> options, ILogger<ResultTestController> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// 获取全部结果测试项（从配置文件读取）。
        /// </summary>
        [HttpGet(Name = "GetResultTestItems")]
        public IActionResult GetAll()
        {
            if (!_options.Enabled)
            {
                return Ok(new { Enabled = false, Message = "结果测试功能已关闭（配置 Enabled=false）" });
            }

            return Ok(new
            {
                Enabled = _options.Enabled,
                Source = _options.Source,
                Count = _options.Items.Count,
                Items = _options.Items
            });
        }

        /// <summary>
        /// 按 Id 获取单个结果测试项（从配置文件读取）。
        /// </summary>
        [HttpGet("items/{id:int}", Name = "GetResultTestItemById")]
        public IActionResult GetById(int id)
        {
            var item = _options.Items.FirstOrDefault(x => x.Id == id);
            if (item is null)
            {
                _logger.LogWarning("未找到结果测试项 Id={Id}", id);
                return NotFound(new { Id = id, Message = "未找到对应的结果测试项" });
            }

            return Ok(item);
        }

        /// <summary>
        /// 获取结果测试汇总统计（根据配置数据实时计算）。
        /// </summary>
        [HttpGet("summary", Name = "GetResultTestSummary")]
        public IActionResult GetSummary()
        {
            var items = _options.Items;
            var average = items.Count == 0 ? 0 : Math.Round(items.Average(x => x.Score), 2);
            var pass = items.Count(x => x.Status == "Pass");
            var warn = items.Count(x => x.Status == "Warn");
            var fail = items.Count(x => x.Status == "Fail");

            return Ok(new
            {
                Source = _options.Source,
                Total = items.Count,
                Pass = pass,
                Warn = warn,
                Fail = fail,
                AverageScore = average
            });
        }

        /// <summary>
        /// 健康检查（配置开关与来源）。
        /// </summary>
        [HttpGet("health", Name = "GetResultTestHealth")]
        public IActionResult Health()
        {
            return Ok(new
            {
                Enabled = _options.Enabled,
                Source = _options.Source,
                Message = "ResultTest 接口运行正常"
            });
        }
    }
}
