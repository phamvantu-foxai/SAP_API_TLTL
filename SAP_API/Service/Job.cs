using Quartz;
using System.IO;

namespace SAP_API.Service
{
    public class JobItem : IJob
    {
        private readonly ILogger<JobItem> _logger;
        private readonly ItemService _itemService;
        bool checkBP = true;
        bool checkIssue = true;
        public JobItem(ILogger<JobItem> logger, ItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[JobItem] Running at {DateTime.Now}");
            if (checkBP)
                checkBP = await _itemService.GetItemMasterData();
        }
    }
    public class JobPriceItem : IJob
    {
        private readonly ILogger<JobItem> _logger;
        private readonly ItemService _itemService;
        bool checkBP = true;
        bool checkIssue = true;
        public JobPriceItem(ILogger<JobItem> logger, ItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[JobItem] Running at {DateTime.Now}");
            if (checkBP)
                checkBP = await _itemService.GetItemPrice();
        }
    }
    public class JobItemSyn : IJob
    {
        private readonly ILogger<JobItemSyn> _logger;
        private readonly ItemService _itemService;
        bool checkBP = true;
        bool checkIssue = true;
        public JobItemSyn(ILogger<JobItemSyn> logger, ItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[JobItemSyn] Running at {DateTime.Now}");
            if (checkBP)
                checkBP = await _itemService.GetTransfer();
        }
    }
    public class JobGoodissue : IJob
    {
        private readonly ILogger<JobGoodissue> _logger;
        private readonly ItemService _itemService;
        bool checkBP = true;
        bool checkIssue = true;
        public JobGoodissue(ILogger<JobGoodissue> logger, ItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            _logger.LogInformation($"[JobGoodissue] Running at {DateTime.Now}");
            if (checkBP)
                checkBP = await _itemService.GetIssue();
        }
    }
    public class JobGoodReceipt : IJob
    {
        private readonly ILogger<JobGoodReceipt> _logger;
        private readonly ItemService _itemService;
        bool checkBP = true;
        bool checkIssue = true;
        public JobGoodReceipt(ILogger<JobGoodReceipt> logger, ItemService itemService)
        {
            _logger = logger;
            _itemService = itemService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Directory.CreateDirectory("Logs");
            string path = "Logs/error.log";
            File.AppendAllText(path, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - Bắt đầu JobGoodReceipt {Environment.NewLine}");
            _logger.LogInformation($"[JobGoodReceipt] Running at {DateTime.Now}");
            if (checkBP)
                checkBP = await _itemService.GetGoodReceipt();
        }
    }
}
